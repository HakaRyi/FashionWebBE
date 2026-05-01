using Microsoft.Extensions.Configuration;
using RabbitMQ.Client;
using System.Text;
using System.Text.Json;

namespace Application.RabbitMQ
{
    public class RabbitMQProducer : IRabbitMQProducer
    {
        private readonly IConfiguration _config;

        public RabbitMQProducer(IConfiguration config)
        {
            _config = config;
        }

        public Task SendMessage<T>(T message)
        {
            string queueName = message is ChatMessageQueueDto
                ? "chat_messages"
                : "post_image_queue";

            var useSsl = bool.Parse(_config["RabbitMQSettings:UseSsl"] ?? "false");

            var factory = new ConnectionFactory
            {
                HostName = _config["RabbitMQSettings:HostName"] ?? "localhost",
                Port = int.Parse(_config["RabbitMQSettings:Port"] ?? "5672"),
                UserName = _config["RabbitMQSettings:UserName"] ?? "guest",
                Password = _config["RabbitMQSettings:Password"] ?? "guest",
                VirtualHost = _config["RabbitMQSettings:VirtualHost"] ?? "/"
            };

            if (useSsl)
            {
                factory.Ssl = new SslOption
                {
                    Enabled = true,
                    ServerName = factory.HostName,
                    Version = System.Security.Authentication.SslProtocols.Tls12
                };
            }

            using var connection = factory.CreateConnection();
            using var channel = connection.CreateModel();

            channel.QueueDeclare(
                queue: queueName,
                durable: true,
                exclusive: false,
                autoDelete: false,
                arguments: null);

            var json = JsonSerializer.Serialize(message);
            var body = Encoding.UTF8.GetBytes(json);

            var properties = channel.CreateBasicProperties();
            properties.Persistent = true;

            channel.BasicPublish(
                exchange: "",
                routingKey: queueName,
                basicProperties: properties,
                body: body);

            return Task.CompletedTask;
        }
    }
}