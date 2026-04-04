namespace Application.Helpers
{
    public static class PaymentCodeGenerator
    {
        public static string GenerateTopUpOrderCode()
        {
            return $"TOP-{Guid.NewGuid().ToString("N")[..10].ToUpper()}";
        }

        public static string GenerateVnPayOrderCode()
        {
            return DateTime.UtcNow.ToString("yyMMdd") + "_" + Guid.NewGuid().ToString("N")[..6];
        }

        public static string GenerateTransactionCode(string prefix)
        {
            return $"{prefix}-{DateTime.UtcNow:yyyyMMddHHmmss}-{Guid.NewGuid().ToString("N")[..6].ToUpper()}";
        }
    }
}