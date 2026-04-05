namespace Application.Services.PaymentService
{
    public interface ITopUpPaymentProcessor
    {
        Task<bool> ProcessAsync(string orderCode, bool isSuccess);
    }
}