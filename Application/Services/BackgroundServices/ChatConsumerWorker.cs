using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using Domain.Entities;
using Application.Interfaces;
using Application.RabbitMQ;
using Application.Response.MessageResp;
using Application.Utils.SignalR;
using System.Text;
using System.Text.Json;
using Domain.Interfaces;

namespace Application.Services.BackgroundServices
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
                        var groupRepo = scope.ServiceProvider.GetRequiredService<IGroupRepository>(); 
                        var uow = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
                        var context = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
                        var hubContext = scope.ServiceProvider.GetRequiredService<IHubContext<ChatHub>>();
                        var newMessage = new Message
                        {
                            AccountId = chatData.SenderId,
                            GroupId = chatData.GroupId,
                            Content = chatData.Content,
                            SentAt = DateTime.Now,
                            ReplyToMessageId = chatData.ReplyToId > 0 ? chatData.ReplyToId : (int?)null,
                            IsRecalled = false,
                            Photos = chatData.ImageUrls.Select(url => new Photo { PhotoUrl = url }).ToList()
                        };


                        await repo.AddMessage(newMessage);
                        await uow.CommitAsync();
                        var senderName = await repo.GetAccountById(chatData.SenderId);
                        var group = await groupRepo.GetGroupById(chatData.GroupId);
                        var messageResponse = new MessageResponse
                        {
                            MessageId = newMessage.MessageId,
                            Content = newMessage.Content,
                            SenderName = senderName.UserName,
                            SenderAvatar = senderName.Avatars
                              .OrderByDescending(img => img.CreatedAt)
                              .Select(img => img.ImageUrl)
                              .FirstOrDefault() ?? null,
                            SenderId = chatData.SenderId,
                            SentAt = newMessage.SentAt,
                            Photos = chatData.ImageUrls,
                            GroupId = chatData.GroupId,
                            GroupName = group?.Name ?? (group?.IsGroup == false ? "Private Chat" : "Nhóm không tên")
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
