using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Services.Response.AiResp
{
    public class VectorApiResponse
    {
        public float[] Embedding { get; set; } = Array.Empty<float>();
    }
}
