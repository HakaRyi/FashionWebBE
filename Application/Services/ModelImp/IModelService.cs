using Application.Request.ModelReq;
using Application.Response;

namespace Application.Services.ModelImp
{
    public interface IModelService
    {
        Task<bool> CreateModelAsync(int accountId, CreateModelRequest request);
        Task DeleteModelAsync(int modelId);
        Task<IEnumerable<ModelResponse>> GetModelsByAccountIdAsync(int accountId);
    }
}
