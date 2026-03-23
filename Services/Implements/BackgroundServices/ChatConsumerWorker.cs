using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using Repositories.Data;
using Repositories.Entities;
using Repositories.Repos.ChatRepos;
using Repositories.UnitOfWork;
using Services.RabbitMQ;
using Services.Response.MessageResp;
using Services.Utils.SignalR;
using System.Text;
using System.Text.Json;

namespace Services.Implements.BackgroundServices
{
    public class ChatConsumerWorker : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly IConfiguration _config;
        private IConnection _connection;
        private IModel _channel;

        public ChatConsumerWorker(IServiceProvider serviceProvider, IConfiguration config)
        {
            _serviceProvider = serviceProvider;
            _config = config;
            InitRabbitMQ();
        }
        private void InitRabbitMQ()
        {
            var factory = new ConnectionFactory
            {
                HostName = _config["RabbitMQSettings:HostName"],
                UserName = _config["RabbitMQSettings:UserName"],
                Password = _config["RabbitMQSettings:Password"]
            };
            _connection = factory.CreateConnection();
            _channel = _connection.CreateModel();
            _channel.QueueDeclare(queue: "chat_messages", durable: true, exclusive: false, autoDelete: false);
        }
        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var consumer = new EventingBasicConsumer(_channel);
            consumer.Received += async (model, ea) =>
            {
                var body = ea.Body.ToArray();
                var messageJson = Encoding.UTF8.GetString(body);
                var chatData = JsonSerializer.Deserialize<ChatMessageQueueDto>(messageJson);

                if (chatData != null)
                {
                    using (var scope = _serviceProvider.CreateScope())
                    {
                        var repo = scope.ServiceProvider.GetRequiredService<IChatRepository>();
                        var uow = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
                        var context = scope.ServiceProvider.GetRequiredService<FashionDbContext>();
                        var hubContext = scope.ServiceProvider.GetRequiredService<IHubContext<ChatHub>>();
                        var newMessage = new Message
                        {
                            AccountId = chatData.SenderId,
                            GroupId = chatData.GroupId,
                            Content = chatData.Content,
                            SentAt = DateTime.UtcNow,
                            ReplyToMessageId = chatData.ReplyToId,
                            IsRecalled = false,
                            Photos = chatData.ImageUrls.Select(url => new Photo { PhotoUrl = url }).ToList()
                        };


                        await repo.AddMessage(newMessage);
                        await uow.CommitAsync();
                        var senderName = await repo.GetAccountById(chatData.SenderId);
                        var messageResponse = new MessageResponse
                        {
                            MessageId = newMessage.MessageId,
                            Content = newMessage.Content,
                            SenderName = senderName.UserName,
                            SentAt = newMessage.SentAt,
                            Photos = chatData.ImageUrls
                        };
                        await hubContext.Clients.Group(chatData.GroupId.ToString())
                                    .SendAsync("ReceiveMessage", messageResponse);
                    }
                }
                _channel.BasicAck(ea.DeliveryTag, false);
            };

            _channel.BasicConsume(queue: "chat_messages", autoAck: false, consumer: consumer);
            return Task.CompletedTask;
        }

        public override void Dispose()
        {
            _channel.Close();
            _connection.Close();
            base.Dispose();
        }

    }
}
