namespace Application.Utils
{
    public static class TransactionCodeGenerator
    {
        public static string Generate()
        {
            return $"TRX-{DateTime.UtcNow:yyyyMMddHHmmssfff}-{Guid.NewGuid().ToString("N")[..6].ToUpper()}";
        }
    }
}