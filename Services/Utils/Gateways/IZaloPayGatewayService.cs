namespace Services.Utils.Gateways
{
    public interface IZaloPayGatewayService
    {
        Task<object> CreateOrderAsync(string appTransId, decimal amount, int accountId);
    }
}