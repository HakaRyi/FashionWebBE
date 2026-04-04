namespace Application.RabbitMQ
{
    public interface IRabbitMQProducer
    {
        Task SendMessage<T>(T message);
    }
}
