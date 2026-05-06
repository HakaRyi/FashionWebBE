using System.Text.Json.Serialization;

namespace Infrastructure.Repositories
{
    public class AIFashionDetectReponse
    {
        [JsonPropertyName("is_fashion")]
        public bool IsFashion { get; set; }

        [JsonPropertyName("confidence")]
        public double Confidence { get; set; }
    }
}
