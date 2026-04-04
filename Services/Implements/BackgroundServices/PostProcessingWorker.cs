using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using Repositories.Constants;
using Repositories.Entities;
using Repositories.Repos.PostRepos;
using Repositories.UnitOfWork;
using Services.RabbitMQ;
using Services.Utils.AIDectection;

namespace Services.Implements.BackgroundServices
{
    public class PostProcessingWorker : BackgroundService
    {
        private readonly IConfiguration _config;
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<PostProcessingWorker> _logger;

        private IConnection? _connection;
        private IModel? _channel;

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
            if (_channel == null)
                throw new InvalidOperationException("RabbitMQ channel is not initialized.");

            var consumer = new AsyncEventingBasicConsumer(_channel);

            consumer.Received += async (_, ea) =>
            {
                try
                {
                    var body = ea.Body.ToArray();
                    var json = Encoding.UTF8.GetString(body);

                    var message = JsonSerializer.Deserialize<PostImageMessage>(json);

                    if (message == null)
                    {
                        _logger.LogWarning("Received null/invalid PostImageMessage payload.");
                        _channel.BasicAck(ea.DeliveryTag, false);
                        return;
                    }

                    await ProcessPostAsync(message, stoppingToken);

                    _channel.BasicAck(ea.DeliveryTag, false);
                }
                catch (Exception ex)
                {
                    _logger.LogError(
                        ex,
                        "Error processing post_image_queue message. Message will be retried, post remains Verifying.");

                    _channel?.BasicNack(ea.DeliveryTag, false, true);
                }
            };

            _channel.BasicConsume(
                queue: "post_image_queue",
                autoAck: false,
                consumer: consumer);

            return Task.CompletedTask;
        }

        private async Task ProcessPostAsync(
            PostImageMessage message,
            CancellationToken cancellationToken)
        {
            using var scope = _scopeFactory.CreateScope();

            var postRepo = scope.ServiceProvider.GetRequiredService<IPostRepository>();
            var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
            var aiService = scope.ServiceProvider.GetRequiredService<IAIDetectionService>();
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<Account>>();
            var post = await postRepo.GetByIdAsync(message.PostId);

            if (post == null)
            {
                _logger.LogWarning("Post {PostId} not found during processing.", message.PostId);
                return;
            }

            if (post.Status != PostStatus.Verifying)
            {
                _logger.LogInformation(
                    "Skipping Post {PostId} because current status is {Status}, not Verifying.",
                    post.PostId,
                    post.Status);
                return;
            }

            // Đây là lỗi bất thường của hệ thống/queue, không reject oan.
            // Throw để message retry, post vẫn giữ Verifying.
            if (message.ImageUrls == null || message.ImageUrls.Count == 0)
            {
                throw new InvalidOperationException(
                    $"PostImageMessage for Post {message.PostId} contains no images.");
            }

            bool hasFashionItem = false;

            foreach (var url in message.ImageUrls)
            {
                cancellationToken.ThrowIfCancellationRequested();

                // Nếu AI lỗi kỹ thuật thì service phải throw ra ngoài
                // để worker nack + retry
                var detected = await aiService.DetectFashionItemsAsync(url);

                if (detected)
                {
                    hasFashionItem = true;
                    break;
                }
            }

            // Với AI bool hiện tại:
            // true  -> Published
            // false -> Rejected
            post.Status = hasFashionItem
                ? PostStatus.Published
                : PostStatus.Rejected;

            post.UpdatedAt = DateTime.UtcNow;

            postRepo.Update(post);
            await unitOfWork.SaveChangesAsync();

            var account = await userManager.FindByIdAsync(post.AccountId.ToString());
            account.CountPost += 1;
            await userManager.UpdateAsync(account);

            _logger.LogInformation(
                "Processed Post {PostId} -> {Status}",
                post.PostId,
                post.Status);
        }

        public override void Dispose()
        {
            try
            {
                _channel?.Close();
                _connection?.Close();
            }
            catch
            {
            }

            base.Dispose();
        }
    }
}