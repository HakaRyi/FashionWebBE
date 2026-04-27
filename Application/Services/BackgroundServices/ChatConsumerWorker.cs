using Application.Interfaces;
using Application.RabbitMQ;
using Application.Response.MessageResp;
using Application.Utils.SignalR;
using Domain.Entities;
using Domain.Interfaces;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;
using System.Text.Json;

namespace Application.Services.BackgroundServices
{
    public class ChatConsumerWorker : BackgroundService
    {
        private const string QueueName = "chat_messages";

        private readonly IServiceProvider _serviceProvider;
        private readonly IConfiguration _config;
        private readonly ILogger<ChatConsumerWorker> _logger;

        private IConnection? _connection;
        private IModel? _channel;

        public ChatConsumerWorker(
            IServiceProvider serviceProvider,
            IConfiguration config,
            ILogger<ChatConsumerWorker> logger)
        {
            _serviceProvider = serviceProvider;
            _config = config;
            _logger = logger;
        }

        private void InitRabbitMQ()
        {
            var factory = new ConnectionFactory
            {
                HostName = _config["RabbitMQSettings:HostName"],
                Port = int.Parse(_config["RabbitMQSettings:Port"]!),
                UserName = _config["RabbitMQSettings:UserName"],
                Password = _config["RabbitMQSettings:Password"],
                VirtualHost = _config["RabbitMQSettings:VirtualHost"],

                DispatchConsumersAsync = true,

                Ssl = new SslOption
                {
                    Enabled = true,
                    ServerName = _config["RabbitMQSettings:HostName"],
                    Version = System.Security.Authentication.SslProtocols.Tls12
                }
            };

            _logger.LogInformation(
              "Connecting RabbitMQ {0}:{1}",
              factory.HostName,
              factory.Port);

            _connection = factory.CreateConnection();
            _channel = _connection.CreateModel();

            _channel.QueueDeclare(
               queue: QueueName,
               durable: true,
               exclusive: false,
               autoDelete: false
            );
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    InitRabbitMQ();

                    if (_channel == null)
                    {
                        throw new InvalidOperationException("RabbitMQ channel is not initialized.");
                    }

                    var consumer = new AsyncEventingBasicConsumer(_channel);

                    consumer.Received += async (_, ea) =>
                    {
                        try
                        {
                            await HandleMessageAsync(ea);

                            _channel.BasicAck(
                                deliveryTag: ea.DeliveryTag,
                                multiple: false);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Failed to process chat message from RabbitMQ.");

                            _channel.BasicNack(
                                deliveryTag: ea.DeliveryTag,
                                multiple: false,
                                requeue: true);
                        }
                    };

                    _channel.BasicConsume(
                        queue: QueueName,
                        autoAck: false,
                        consumer: consumer);

                    _logger.LogInformation("ChatConsumerWorker connected to RabbitMQ.");

                    await Task.Delay(Timeout.Infinite, stoppingToken);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "RabbitMQ connection failed. Retrying in 10 seconds.");

                    SafeCloseRabbitMQ();

                    await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);
                }
            }
        }

        private async Task HandleMessageAsync(BasicDeliverEventArgs ea)
        {
            var body = ea.Body.ToArray();
            var messageJson = Encoding.UTF8.GetString(body);

            var chatData = JsonSerializer.Deserialize<ChatMessageQueueDto>(
                messageJson,
                new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

            if (chatData == null)
            {
                return;
            }

            using var scope = _serviceProvider.CreateScope();

            var repo = scope.ServiceProvider.GetRequiredService<IChatRepository>();
            var groupRepo = scope.ServiceProvider.GetRequiredService<IGroupRepository>();
            var uow = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
            var hubContext = scope.ServiceProvider.GetRequiredService<IHubContext<ChatHub>>();

            var imageUrls = chatData.ImageUrls ?? new List<string>();

            var newMessage = new Message
            {
                AccountId = chatData.SenderId,
                GroupId = chatData.GroupId,
                Content = chatData.Content,
                SentAt = DateTime.UtcNow,
                ReplyToMessageId = chatData.ReplyToId > 0 ? chatData.ReplyToId : null,
                IsRecalled = false,
                Photos = imageUrls
                    .Select(url => new Photo { PhotoUrl = url })
                    .ToList()
            };

            await repo.AddMessage(newMessage);
            await uow.CommitAsync();

            var sender = await repo.GetAccountById(chatData.SenderId);
            var group = await groupRepo.GetGroupById(chatData.GroupId);

            var messageResponse = new MessageResponse
            {
                MessageId = newMessage.MessageId,
                Content = newMessage.Content,
                SenderName = sender.UserName,
                SenderAvatar = sender.Avatars
                    .OrderByDescending(img => img.CreatedAt)
                    .Select(img => img.ImageUrl)
                    .FirstOrDefault(),
                SenderId = chatData.SenderId,
                SentAt = newMessage.SentAt,
                Photos = imageUrls,
                GroupId = chatData.GroupId,
                GroupName = group?.Name ?? (group?.IsGroup == false ? "Private Chat" : "Unnamed Group")
            };

            await hubContext.Clients
                .Group(chatData.GroupId.ToString())
                .SendAsync("ReceiveMessage", messageResponse);
        }

        private void SafeCloseRabbitMQ()
        {
            try
            {
                _channel?.Close();
                _channel?.Dispose();
            }
            catch
            {
            }

            try
            {
                _connection?.Close();
                _connection?.Dispose();
            }
            catch
            {
            }

            _channel = null;
            _connection = null;
        }

        public override void Dispose()
        {
            SafeCloseRabbitMQ();
            base.Dispose();
        }
    }
}