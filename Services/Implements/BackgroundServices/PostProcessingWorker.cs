using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using Repositories.Constants;
using Repositories.Repos.PostRepos;
using Repositories.UnitOfWork;
using Services.RabbitMQ;
using Services.Utils;
using System.Text;
using System.Text.Json;

namespace Services.Implements.BackgroundServices
{
    public class PostProcessingWorker : BackgroundService
    {
        private readonly IConfiguration _config;
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<PostProcessingWorker> _logger;

        private IConnection _connection;
        private IModel _channel;

        public PostProcessingWorker(
            IConfiguration config,
            IServiceScopeFactory scopeFactory,
            ILogger<PostProcessingWorker> logger)
        {
            _config = config;
            _scopeFactory = scopeFactory;
            _logger = logger;

            InitializeRabbitMQ();
        }

        private void InitializeRabbitMQ()
        {
            var factory = new ConnectionFactory
            {
                HostName = _config["RabbitMQSettings:HostName"],
                UserName = _config["RabbitMQSettings:UserName"],
                Password = _config["RabbitMQSettings:Password"],
                DispatchConsumersAsync = true,
                AutomaticRecoveryEnabled = true
            };

            _connection = factory.CreateConnection();
            _channel = _connection.CreateModel();

            _channel.QueueDeclare(
                queue: "post_image_queue",
                durable: true,
                exclusive: false,
                autoDelete: false,
                arguments: null);

            _channel.BasicQos(0, 1, false);
        }

        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var consumer = new AsyncEventingBasicConsumer(_channel);

            consumer.Received += async (model, ea) =>
            {
                try
                {
                    var body = ea.Body.ToArray();
                    var json = Encoding.UTF8.GetString(body);

                    var message = JsonSerializer.Deserialize<PostImageMessage>(json);

                    if (message != null)
                    {
                        await ProcessPostAsync(message);
                    }

                    _channel.BasicAck(ea.DeliveryTag, false);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error processing message");
                    _channel.BasicNack(ea.DeliveryTag, false, true);
                }
            };

            _channel.BasicConsume(
                queue: "post_image_queue",
                autoAck: false,
                consumer: consumer);

            return Task.CompletedTask;
        }

        private async Task ProcessPostAsync(PostImageMessage message)
        {
            using var scope = _scopeFactory.CreateScope();

            var postRepo = scope.ServiceProvider.GetRequiredService<IPostRepository>();
            var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
            var aiService = scope.ServiceProvider.GetRequiredService<IAIDetectionService>();

            var post = await postRepo.GetByIdAsync(message.PostId);

            if (post == null)
                return;

            bool hasFashionItem = false;

            foreach (var url in message.ImageUrls)
            {
                if (await aiService.DetectFashionItemsAsync(url))
                {
                    hasFashionItem = true;
                    break;
                }
            }

            post.Status = hasFashionItem
                ? PostStatus.Published
                : PostStatus.PendingAdmin;

            post.UpdatedAt = DateTime.UtcNow;

            postRepo.Update(post);
            await unitOfWork.SaveChangesAsync();

            _logger.LogInformation(
                "Processed Post {PostId} -> {Status}",
                post.PostId,
                post.Status);
        }

        public override void Dispose()
        {
            _channel?.Close();
            _connection?.Close();
            base.Dispose();
        }
    }
}