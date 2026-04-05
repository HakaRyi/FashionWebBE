using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Polly;
using Repositories.Repos.ModelRepos;
using Services.Response.AiResp;
using Services.Utils.SignalR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Services.Implements.BackgroundServices
{
    public class ModelProgessingWorker : BackgroundService
    {
        private readonly IBackgroundTaskQueue _taskQueue;
        private readonly IServiceProvider _serviceProvider;

        public ModelProgessingWorker(IBackgroundTaskQueue taskQueue, IServiceProvider serviceProvider)
        {
            _taskQueue = taskQueue;
            _serviceProvider = serviceProvider;
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
                    var hubContext = scope.ServiceProvider.GetRequiredService<IHubContext<NotificationHub>>();

                    string finalStatus = "Rejected";

                    var retryPolicy = Policy
                        .Handle<HttpRequestException>()
                        .Or<TaskCanceledException>()
                        .WaitAndRetryAsync(
                            3,
                            retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt))
                        );

                    try
                    {
                        await retryPolicy.ExecuteAsync(async () =>
                        {
                            using var client = httpClientFactory.CreateClient();

                            var imageBytes = await client.GetByteArrayAsync(workItem.ImageUrl, stoppingToken);

                            using var content = new MultipartFormDataContent();
                            var imageContent = new ByteArrayContent(imageBytes);
                            content.Add(imageContent, "file", "model.jpg");

                            var response = await client.PostAsync("https://sliding-rudderless-consuelo.ngrok-free.dev/validate", content, stoppingToken);

                            if (response.IsSuccessStatusCode)
                            {
                                var resultStr = await response.Content.ReadAsStringAsync(stoppingToken);
                                var result = JsonSerializer.Deserialize<AiValidationResponse>(resultStr);
                                finalStatus = (result != null && result.Valid) ? "Active" : "Rejected";
                            }
                            else
                            {
                                throw new HttpRequestException($"API Error {response.StatusCode}");
                            }
                        });
                    }
                    catch
                    {
                        finalStatus = "Rejected";
                    }

                    var model = await modelRepo.GetModelByIdAsync(workItem.ModelId);
                    if (model != null)
                    {
                        model.Status = finalStatus;
                        await modelRepo.UpdateModelAsync(model);
                    }

                    await hubContext.Clients.User(workItem.AccountId.ToString())
                        .SendAsync("ModelProcessed", new { modelId = workItem.ModelId, status = finalStatus }, stoppingToken);
                }
                catch
                {
                }
            }
        }
    }
}
