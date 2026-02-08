using Microsoft.Extensions.DependencyInjection;
using Repositories.Entities;
using Repositories.Repos.PostRepos;
using Services.Request.PostReq;
using Services.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Services.Implements.PostImp
{
    public class PostService : IPostService
    {
        private readonly IPostRepository _postRepo;
        private readonly ICloudStorageService _storageService;
        private readonly IAIDetectionService _aiService;
        private readonly IServiceScopeFactory _scopeFactory;

        public PostService(
            IPostRepository postRepo,
            ICloudStorageService storageService,
            IAIDetectionService aiService,
            IServiceScopeFactory scopeFactory)
        {
            _postRepo = postRepo;
            _storageService = storageService;
            _aiService = aiService;
            _scopeFactory = scopeFactory;
        }

        public async Task<Post> CreatePostAsync(CreatePostRequest request)
        {
            var newPost = new Post
            {
                AccountId = request.AccountId,
                Tittle = request.Title,
                Content = request.Content,
                EventId = request.EventId,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                LikeCount = 0,
                ShareCount = 0,
                Score = 0,
                IsExpertPost = false,
                Status = (request.Images != null && request.Images.Any()) ? "Verifying" : "Active"
            };
            await _postRepo.AddPostAsync(newPost);

            if (request.Images != null && request.Images.Count > 0)
            {
                var imageUrls = new List<string>();
                foreach (var file in request.Images)
                {
                    var url = await _storageService.UploadImageAsync(file);
                    imageUrls.Add(url);
                }
                _ = Task.Run(() => ProcessPostAIInBackground(newPost.PostId, imageUrls));
            }

            return newPost;
        }
        private async Task ProcessPostAIInBackground(int postId, List<string> imageUrls)
        {
            using (var scope = _scopeFactory.CreateScope())
            {
                var scopedRepo = scope.ServiceProvider.GetRequiredService<IPostRepository>();
                var aiService = scope.ServiceProvider.GetRequiredService<IAIDetectionService>();

                var post = await scopedRepo.GetPostByIdAsync(postId);
                if (post == null) return;

                bool hasFashionItem = false;

                foreach (var url in imageUrls)
                {
                    if (!hasFashionItem)
                    {
                        bool isFashion = await aiService.DetectFashionItemsAsync(url);
                        if (isFashion) hasFashionItem = true;
                    }
                }

                if (hasFashionItem)
                {
                    post.Status = "Active";
                }
                else
                {
                    post.Status = "PendingAdmin";
                }
                await scopedRepo.UpdatePostAsync(post);
            }
        }
    }
}
