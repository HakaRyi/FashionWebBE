namespace Services.Implements.PaymentService
{
    public interface ITopUpPaymentProcessor
    {
        Task<bool> ProcessAsync(string orderCode, bool isSuccess);
    }
}