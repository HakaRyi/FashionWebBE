using Services.Request.ModelReq;
using Services.Response;

namespace Services.Implements.ModelImp
{
    public interface IModelService
    {
        Task<bool> CreateModelAsync(int accountId, CreateModelRequest request);
        Task DeleteModelAsync(int modelId);
        Task<IEnumerable<ModelResponse>> GetModelsByAccountIdAsync(int accountId);
    }
}
