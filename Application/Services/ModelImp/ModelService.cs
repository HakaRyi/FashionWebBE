using Application.Services.BackgroundServices;
using Domain.Entities;
using Application.Request.ModelReq;
using Application.Response;
using Application.Response.AiResp;
using Application.Utils;
using System.Text.Json;
using Application.Interfaces;
using Domain.Interfaces;

namespace Application.Services.ModelImp
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
            var newModel = new Model
            {
                AccountId = accountId,
                Status = "Processing",
                CreatedAt = DateTime.UtcNow
            };

            await _modelRepo.CreateModelAsync(newModel);
            using var ms = new MemoryStream();
            await request.Image.CopyToAsync(ms);

            await _taskQueue.QueueBackgroundWorkItemAsync(new ModelProcessingJob
            {
                ModelId = newModel.Id,
                AccountId = accountId,
                ImageBytes = ms.ToArray(),
                FileName = request.Image.FileName,
                ContentType = request.Image.ContentType
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
