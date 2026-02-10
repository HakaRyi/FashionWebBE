using Microsoft.Extensions.Configuration;
using RabbitMQ.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Services.RabbitMQ
{
    public class RabbitMQProducer : IRabbitMQProducer
    {
        private readonly IConfiguration _config;

        public RabbitMQProducer(IConfiguration config)
        {
            _config = config;
        }

        public void SendMessage<T>(T message)
        {
            var factory = new ConnectionFactory
            {
                HostName = _config["RabbitMQSettings:HostName"],
                UserName = _config["RabbitMQSettings:UserName"],
                Password = _config["RabbitMQSettings:Password"]
            };

            using var connection = factory.CreateConnection();
            using var channel = connection.CreateModel();

            channel.QueueDeclare(queue: "post_image_queue", durable: true, exclusive: false, autoDelete: false, arguments: null);

            var json = JsonSerializer.Serialize(message);
            var body = Encoding.UTF8.GetBytes(json);

            var properties = channel.CreateBasicProperties();
            properties.Persistent = true;

            channel.BasicPublish(exchange: "", routingKey: "post_image_queue", basicProperties: properties, body: body);
        }
    }
}
