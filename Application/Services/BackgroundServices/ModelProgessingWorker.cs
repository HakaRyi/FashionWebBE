using Application.Response.AiResp;
using Application.Services.BackgroundServices;
using Application.Utils;
using Application.Utils.SignalR;
using Domain.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.Net.Http.Headers;
using System.Text.Json;

public class ModelProcessingWorker : BackgroundService
{
    private readonly IBackgroundTaskQueue _taskQueue;
    private readonly IServiceProvider _serviceProvider;
    private readonly ICloudStorageService _storageService;

    public ModelProcessingWorker(
        IBackgroundTaskQueue taskQueue,
        IServiceProvider serviceProvider,
        ICloudStorageService storageService)
    {
        _taskQueue = taskQueue;
        _serviceProvider = serviceProvider;
        _storageService = storageService;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            var workItem = await _taskQueue.DequeueAsync(stoppingToken);

            try
            {
                using var scope = _serviceProvider.CreateScope();
                var httpClientFactory = scope.ServiceProvider.GetRequiredService<IHttpClientFactory>();
                var modelRepo = scope.ServiceProvider.GetRequiredService<IModelRepository>();
                var storageService = scope.ServiceProvider.GetRequiredService<ICloudStorageService>();
                var hubContext = scope.ServiceProvider.GetRequiredService<IHubContext<NotificationHub>>();

                string finalStatus = "Rejected";

                var client = httpClientFactory.CreateClient();

                using var content = new MultipartFormDataContent();

                if (workItem.ImageBytes != null)
                {
                    var imageContent = new ByteArrayContent(workItem.ImageBytes);
                    imageContent.Headers.ContentType =
                        new MediaTypeHeaderValue(workItem.ContentType ?? "image/jpeg");

                    content.Add(imageContent, "file", workItem.FileName ?? "model.jpg");
                }
                else
                {
                    finalStatus = "Rejected";
                }

                var response = await client.PostAsync(
                    "https://sliding-rudderless-consuelo.ngrok-free.dev/validate",
                    content,
                    stoppingToken
                );

                if (!response.IsSuccessStatusCode)
                    throw new HttpRequestException($"API Error {response.StatusCode}");

                var contentType = response.Content.Headers.ContentType?.MediaType;

                if (contentType == "application/json")
                {
                    var resultStr = await response.Content.ReadAsStringAsync(stoppingToken);
                    var result = JsonSerializer.Deserialize<AiValidationResponse>(resultStr);

                    finalStatus = result != null && result.Valid ? "Active" : "Rejected";
                }
                else if (contentType == "image/jpeg")
                {
                    var processedBytes = await response.Content.ReadAsByteArrayAsync(stoppingToken);

                    var formFile = ConvertToFormFile(processedBytes, $"model_{workItem.ModelId}.jpg");

                    var newImageUrl = await storageService.UploadImageAsync(formFile);

                    workItem.ImageUrl = newImageUrl;

                    finalStatus = "Active";
                }
                else
                {
                    finalStatus = "Rejected";
                }

                var model = await modelRepo.GetModelByIdAsync(workItem.ModelId);
                if (model != null)
                {
                    model.Status = finalStatus;
                    model.ImageUrl = workItem.ImageUrl;
                    await modelRepo.UpdateModelAsync(model);
                }

                await hubContext.Clients.User(workItem.AccountId.ToString())
                    .SendAsync("ModelProcessed", new
                    {
                        modelId = workItem.ModelId,
                        status = finalStatus,
                        imageUrl = workItem.ImageUrl
                    }, stoppingToken);
            }
            catch
            {
            }
        }
    }

    private IFormFile ConvertToFormFile(byte[] bytes, string fileName)
    {
        var stream = new MemoryStream(bytes);

        return new FormFile(stream, 0, bytes.Length, "file", fileName)
        {
            Headers = new HeaderDictionary(),
            ContentType = "image/jpeg"
        };
    }
}