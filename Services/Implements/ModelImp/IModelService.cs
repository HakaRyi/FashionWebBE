using Services.Request.ModelReq;
using Services.Response;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Services.Implements.ModelImp
{
    public interface IModelService
    {
        Task<bool> CreateModelAsync(int accountId, CreateModelRequest request);
        Task DeleteModelAsync(int modelId);
        Task<IEnumerable<ModelResponse>> GetModelsByAccountIdAsync(int accountId);
    }
}
