using Repositories.Entities;
using Repositories.Repos.ModelRepos;
using Repositories.UnitOfWork;
using Services.Request.ModelReq;
using Services.Response;
using Services.Utils;

namespace Services.Implements.ModelImp
{
    public class ModelService : IModelService
    {
        private readonly IModelRepository _modelRepo;
        private readonly ICloudStorageService _storageService;
        private readonly IUnitOfWork _unitOfWork;

        public ModelService(IModelRepository modelRepo, ICloudStorageService storageService, IUnitOfWork unitOfWork)
        {
            _modelRepo = modelRepo;
            _storageService = storageService;
            _unitOfWork = unitOfWork;
        }

        public async Task<bool> CreateModelAsync(int accountId, CreateModelRequest request)
        {
            var imageUrl = await _storageService.UploadImageAsync(request.Image);

            var newModel = new Model
            {
                AccountId = accountId,
                ImageUrl = imageUrl,
                Status = "Active",
                CreatedAt = DateTime.UtcNow
            };

            await _modelRepo.CreateModelAsync(newModel);
            return true;
        }

        public Task DeleteModelAsync(int modelId)
        {
            throw new NotImplementedException();
        }

        public async Task<IEnumerable<ModelResponse>> GetModelsByAccountIdAsync(int accountId)
        {
            var models = await _modelRepo.GetModelsByAccountIdAsync(accountId);

            return models.Select(m => new ModelResponse
            {
                Id = m.Id,
                ImageUrl = m.ImageUrl,
                Status = m.Status,
                CreatedAt = m.CreatedAt
            });
        }
    }
}
