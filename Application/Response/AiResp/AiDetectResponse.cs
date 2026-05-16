using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Application.Response.AiResp
{
    public class AIFashionDetectReponse
    {
        [JsonPropertyName("is_clothing")]
        public bool IsClothing { get; set; }

        [JsonPropertyName("label")]
        public string? Label { get; set; }

        [JsonPropertyName("confidence")]
        public double? Confidence { get; set; }
    }
}
