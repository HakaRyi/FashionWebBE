using System.Text.Json.Serialization;

namespace Repositories.Repos.AI
{
    public class AIFashionDetectReponse
    {
        [JsonPropertyName("is_fashion")]
        public bool IsFashion { get; set; }

        [JsonPropertyName("confidence")]
        public double Confidence { get; set; }
    }
}
