using Microsoft.Extensions.DependencyInjection;
using Repositories.Entities;
using Repositories.Repos.PostRepos;
using Services.RabbitMQ;
using Services.Request.PostReq;
using Services.Response.PostResp;
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
        private readonly IRabbitMQProducer _producer;

        public PostService(
            IPostRepository postRepo,
            ICloudStorageService storageService,
            IAIDetectionService aiService,
            IRabbitMQProducer producer)
        {
            _postRepo = postRepo;
            _storageService = storageService;
            _aiService = aiService;
            _producer = producer;
        }

        public async Task<string> AdminCheckTheStatusPost(CheckPostRequest request, int id)
        {
            var post = await _postRepo.GetPostByIdAsync(id);
            if (post == null)
            {
                return "Post not found.";
            }
            post.Status = request.Status;
            await _postRepo.UpdatePostAsync(post);
            return "successfully";

        }

        public async Task<Post> CreatePostAsync(int accountId, CreatePostRequest request)
        {
            var newPost = new Post
            {
                AccountId = accountId,
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

            if (request.Images != null && request.Images.Any())
            {
                var imageUrls = new List<string>();

                foreach (var file in request.Images)
                {
                    var url = await _storageService.UploadImageAsync(file);
                    imageUrls.Add(url);
                }

                var message = new PostImageMessage
                {
                    PostId = newPost.PostId,
                    ImageUrls = imageUrls
                };

                _producer.SendMessage(message);
            }

            return newPost;
        }
        public async Task<List<PostResponse>> GetAllPostAsync()
        {
            var posts = await _postRepo.GetAllPostAsync();
            return posts.Select(post => MapToPostResponse(post)).ToList();
        }

        public async Task<List<PostResponse>> GetAllMyPostAsync(int userId)
        {
            var posts = await _postRepo.GetAllMyPostAsync(userId);
            return posts.Select(post => MapToPostResponse(post)).ToList();
        }

        public async Task<PostResponse> GetPostByIdAsync(int postId)
        {
            var post = await _postRepo.GetPostByIdAsync(postId);
            if (post == null) return null;
            return MapToPostResponse(post);
        }

        private PostResponse MapToPostResponse(Post post)
        {
            return new PostResponse
            {
                PostId = post.PostId,
                UserName = post.Account?.UserName,
                AvatarUrl = post.Account?.Avatar,
                EventId = post.EventId,
                EventName = post.Event?.Title,
                Title = post.Tittle,
                Content = post.Content,
                ImageUrls = post.Images?.Select(img => img.ImageUrl).ToList() ?? new List<string>(),
                IsExpertPost = post.IsExpertPost,
                Status = post.Status,
                Score = post.Score,
                LikeCount = post.LikeCount,
                ShareCount = post.ShareCount,
                CreatedAt = post.CreatedAt,
                UpdatedAt = post.UpdatedAt
            };
        }

        public async Task UpdatePostAsync(int postId, UpdatePostRequest request)
        {
            var post = await _postRepo.GetPostByIdAsync(postId);
            if (post == null) return;
            post.Tittle = request.Tittle ?? post.Tittle;
            post.Content = request.Content ?? post.Content;
            post.UpdatedAt = DateTime.UtcNow;
            post.Status = (request.Images != null && request.Images.Any()) ? "Verifying" : post.Status;

            await _postRepo.UpdatePostAsync(post);

            if (request.Images != null && request.Images.Any())
            {
                var imageUrls = new List<string>();
                foreach (var file in request.Images)
                {
                    var url = await _storageService.UploadImageAsync(file);
                    imageUrls.Add(url);
                }

                var message = new PostImageMessage
                {
                    PostId = post.PostId,
                    ImageUrls = imageUrls
                };

                _producer.SendMessage(message);
            }
        }

        public async Task DeletePostAsync(int postId)
        {
            var post = await _postRepo.GetPostByIdAsync(postId);
            if (post == null) return;
            await _postRepo.DeletePostAsync(postId);
        }
        //private async Task ProcessPostAIInBackground(int postId, List<string> imageUrls)
        //{
        //    using (var scope = _scopeFactory.CreateScope())
        //    {
        //        var scopedRepo = scope.ServiceProvider.GetRequiredService<IPostRepository>();
        //        var aiService = scope.ServiceProvider.GetRequiredService<IAIDetectionService>();

        //        var post = await scopedRepo.GetPostByIdAsync(postId);
        //        if (post == null) return;

        //        bool hasFashionItem = false;

        //        foreach (var url in imageUrls)
        //        {
        //            if (!hasFashionItem)
        //            {
        //                bool isFashion = await aiService.DetectFashionItemsAsync(url);
        //                if (isFashion) hasFashionItem = true;
        //            }
        //        }

        //        if (hasFashionItem)
        //        {
        //            post.Status = "Active";
        //        }
        //        else
        //        {
        //            post.Status = "PendingAdmin";
        //        }
        //        await scopedRepo.UpdatePostAsync(post);
        //    }
        //}
    }
}
