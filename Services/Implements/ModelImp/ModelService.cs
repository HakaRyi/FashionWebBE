using Repositories.Entities;
using Repositories.Repos.ModelRepos;
using Repositories.UnitOfWork;
using Services.Implements.BackgroundServices;
using Services.Request.ModelReq;
using Services.Response;
using Services.Response.AiResp;
using Services.Utils;
using System.Text.Json;

namespace Services.Implements.ModelImp
{
    public class ModelService : IModelService
    {
        private readonly IModelRepository _modelRepo;
        private readonly ICloudStorageService _storageService;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IBackgroundTaskQueue _taskQueue;

        public ModelService(
            IModelRepository modelRepo,
            ICloudStorageService storageService,
            IUnitOfWork unitOfWork,
            IHttpClientFactory httpClientFactory,
            IBackgroundTaskQueue taskQueue)
        {
            _modelRepo = modelRepo;
            _storageService = storageService;
            _unitOfWork = unitOfWork;
            _httpClientFactory = httpClientFactory;
            _taskQueue = taskQueue;
        }

        public async Task<bool> CreateModelAsync(int accountId, CreateModelRequest request)
        {
            var imageUrl = await _storageService.UploadImageAsync(request.Image);

            var newModel = new Model
            {
                AccountId = accountId,
                ImageUrl = imageUrl,
                Status = "Processing",
                CreatedAt = DateTime.UtcNow
            };

            await _modelRepo.CreateModelAsync(newModel);

            await _taskQueue.QueueBackgroundWorkItemAsync(new ModelProcessingJob
            {
                ModelId = newModel.Id,
                AccountId = accountId,
                ImageUrl = imageUrl
            });

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
