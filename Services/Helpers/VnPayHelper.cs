using System.Security.Cryptography;
using System.Text;

namespace Services.Helpers
{
    public static class VnPayHelper
    {
        public static string HmacSha512(string key, string inputData)
        {
            var hash = new StringBuilder();
            byte[] keyBytes = Encoding.UTF8.GetBytes(key);
            byte[] inputBytes = Encoding.UTF8.GetBytes(inputData);

            using var hmac = new HMACSHA512(keyBytes);
            byte[] hashValue = hmac.ComputeHash(inputBytes);

            foreach (var b in hashValue)
            {
                hash.Append(b.ToString("x2"));
            }

            return hash.ToString();
        }

        public static string HmacSha256(string key, string inputData)
        {
            using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(key));
            var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(inputData));
            return BitConverter.ToString(hash).Replace("-", "").ToLower();
        }
    }
}