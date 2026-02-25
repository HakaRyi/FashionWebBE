using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using Repositories.Repos.PostRepos;
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
        private IConnection _connection;
        private IModel _channel;

        public PostProcessingWorker(IConfiguration config, IServiceScopeFactory scopeFactory)
        {
            _config = config;
            _scopeFactory = scopeFactory;
            InitializeRabbitMQ();
        }

        //private void InitializeRabbitMQ()
        //{
        //    var factory = new ConnectionFactory
        //    {
        //        HostName = _config["RabbitMQSettings:HostName"],
        //        UserName = _config["RabbitMQSettings:UserName"],
        //        Password = _config["RabbitMQSettings:Password"],
        //        DispatchConsumersAsync = true
        //    };
        //    _connection = factory.CreateConnection();
        //    _channel = _connection.CreateModel();
        //    _channel.QueueDeclare(queue: "post_image_queue", durable: true, exclusive: false, autoDelete: false, arguments: null);
        //    _channel.BasicQos(prefetchSize: 0, prefetchCount: 1, global: false);
        //}

        private void InitializeRabbitMQ()
        {
            var host = _config["RabbitMQSettings:HostName"] ?? "localhost";
            var user = _config["RabbitMQSettings:UserName"] ?? "guest";
            var pass = _config["RabbitMQSettings:Password"] ?? "guest";

            try
            {
                var factory = new ConnectionFactory
                {
                    HostName = host,
                    UserName = user,
                    Password = pass,
                    DispatchConsumersAsync = true
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

                Console.WriteLine("RabbitMQ connected.");
            }
            catch (Exception ex)
            {
                Console.WriteLine("RabbitMQ disabled: " + ex.Message);
                _connection = null;
                _channel = null;
            }
        }

        //protected override Task ExecuteAsync(CancellationToken stoppingToken)
        //{
        //    var consumer = new AsyncEventingBasicConsumer(_channel);

        //    consumer.Received += async (model, ea) =>
        //    {
        //        var body = ea.Body.ToArray();
        //        var messageJson = Encoding.UTF8.GetString(body);
        //        var message = JsonSerializer.Deserialize<PostImageMessage>(messageJson);

        //        if (message != null)
        //        {
        //            await ProcessPostAsync(message);
        //        }

        //        _channel.BasicAck(deliveryTag: ea.DeliveryTag, multiple: false);
        //    };

        //    _channel.BasicConsume(queue: "post_image_queue", autoAck: false, consumer: consumer);
        //    return Task.CompletedTask;
        //}

        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            if (_channel == null)
            {
                Console.WriteLine("RabbitMQ not initialized. Worker is idle.");
                return Task.CompletedTask;
            }

            var consumer = new AsyncEventingBasicConsumer(_channel);

            consumer.Received += async (model, ea) =>
            {
                var body = ea.Body.ToArray();
                var messageJson = Encoding.UTF8.GetString(body);
                var message = JsonSerializer.Deserialize<PostImageMessage>(messageJson);

                if (message != null)
                {
                    await ProcessPostAsync(message);
                }

                _channel.BasicAck(ea.DeliveryTag, false);
            };

            _channel.BasicConsume("post_image_queue", false, consumer);

            return Task.CompletedTask;
        }

        private async Task ProcessPostAsync(PostImageMessage message)
        {
            using (var scope = _scopeFactory.CreateScope())
            {
                var postRepo = scope.ServiceProvider.GetRequiredService<IPostRepository>();
                var aiService = scope.ServiceProvider.GetRequiredService<IAIDetectionService>();

                try
                {
                    var post = await postRepo.GetPostByIdAsync(message.PostId);
                    if (post == null) return;

                    bool hasFashionItem = false;

                    foreach (var url in message.ImageUrls)
                    {
                        if (!hasFashionItem)
                        {
                            if (await aiService.DetectFashionItemsAsync(url)) hasFashionItem = true;
                        }
                    }

                    post.Status = hasFashionItem ? "Active" : "PendingAdmin";
                    post.UpdatedAt = DateTime.UtcNow;

                    await postRepo.UpdatePostAsync(post);
                    Console.WriteLine($"Processed Post {post.PostId}: {post.Status}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error processing Post {message.PostId}: {ex.Message}");
                }
            }
        }

        //public override void Dispose()
        //{
        //    _channel.Close();
        //    _connection.Close();
        //    base.Dispose();
        //}

        public override void Dispose()
        {
            _channel?.Close();
            _connection?.Close();
            base.Dispose();
        }
    }
}
