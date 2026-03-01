namespace Services.RabbitMQ
{
    public interface IRabbitMQProducer
    {
        void SendMessage<T>(T message);
    }
}
