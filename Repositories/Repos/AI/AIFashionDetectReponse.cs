using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

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
