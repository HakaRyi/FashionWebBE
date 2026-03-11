using Microsoft.AspNetCore.Http;
using Repositories.Constants;
using Repositories.Dto.Response;
using Repositories.Entities;
using Repositories.Repos.ImageRepos;
using Repositories.Repos.PostRepos;
using Repositories.UnitOfWork;
using Services.RabbitMQ;
using Services.Request.PostReq;
using Services.Response.PostResp;
using Services.Utils;

namespace Services.Implements.PostImp
{
    public class PostService : IPostService
    {
        private readonly IPostRepository _postRepo;
        private readonly IImageRepository _imageRepo;
        private readonly ICloudStorageService _storageService;
        private readonly IRabbitMQProducer _producer;
        private readonly IUnitOfWork _unitOfWork;

        private const int MAX_IMAGES = 5;

        public PostService(
            IPostRepository postRepo,
            IImageRepository imageRepo,
            ICloudStorageService storageService,
            IRabbitMQProducer producer,
            IUnitOfWork unitOfWork)
        {
            _postRepo = postRepo;
            _imageRepo = imageRepo;
            _storageService = storageService;
            _producer = producer;
            _unitOfWork = unitOfWork;
        }

        public async Task<PostResponse> CreatePostAsync(
            int accountId,
            CreatePostRequest request)
        {
            ValidatePostContent(request.Content, request.Images);

            var now = DateTime.UtcNow;

            var imageUrls = await UploadImagesAsync(request.Images);

            var status = imageUrls.Any()
                ? PostStatus.Verifying
                : PostStatus.Published;

            var post = new Post
            {
                AccountId = accountId,
                Content = request.Content?.Trim(),
                EventId = request.EventId,
                CreatedAt = now,
                UpdatedAt = now,
                Status = status,
                LikeCount = 0,
                ShareCount = 0,
                Score = 0,
                IsExpertPost = false,
                Images = imageUrls.Select(url => new Image
                {
                    ImageUrl = url,
                    OwnerType = "Post",
                    CreatedAt = now
                }).ToList()
            };

            await _postRepo.AddAsync(post);
            await _unitOfWork.SaveChangesAsync();

            if (imageUrls.Any())
            {
                await SendModerationMessage(post.PostId, imageUrls);
            }

            return MapToResponse(post);
        }

        public async Task<PostResponse> UpdatePostAsync(
            int postId,
            int accountId,
            UpdatePostRequest request)
        {
            var post = await _postRepo.GetByIdAsync(postId)
                ?? throw new KeyNotFoundException("Post not found");

            if (post.AccountId != accountId)
                throw new UnauthorizedAccessException();

            UpdateBasicInfo(post, request);

            if (request.Images != null && request.Images.Any())
            {
                await ReplaceImages(post, request.Images);
            }

            post.UpdatedAt = DateTime.UtcNow;

            await _unitOfWork.SaveChangesAsync();

            return MapToResponse(post);
        }

        public async Task DeletePostAsync(int postId)
        {
            var post = await _postRepo.GetByIdAsync(postId)
                ?? throw new KeyNotFoundException("Post not found");

            await DeleteImages(post.Images);

            _postRepo.Delete(post);

            await _unitOfWork.SaveChangesAsync();
        }

        public async Task<string> AdminCheckTheStatusPost(
            CheckPostRequest request,
            int postId)
        {
            if (string.IsNullOrWhiteSpace(request.Status))
                return "Status is required.";

            if (!PostStatus.IsValid(request.Status))
                return "Invalid status.";

            var post = await _postRepo.GetByIdAsync(postId);

            if (post == null)
                return "Post not found.";

            if (post.Status != PostStatus.PendingAdmin &&
                post.Status != PostStatus.Verifying)
                return "Post is not waiting moderation.";

            if (post.Status == request.Status)
                return "Status already set.";

            post.Status = request.Status;
            post.UpdatedAt = DateTime.UtcNow;

            await _unitOfWork.SaveChangesAsync();

            return "Post moderation updated.";
        }

        public async Task<List<PostResponse>> GetAllPostAsync()
        {
            var posts = await _postRepo.GetAllPublishedAsync();

            return posts.Select(MapToResponse).ToList();
        }

        public async Task<List<PostResponse>> GetAllMyPostAsync(int userId)
        {
            var posts = await _postRepo.GetAllByUserAsync(userId);

            return posts.Select(MapToResponse).ToList();
        }

        public async Task<PostResponse?> GetPostByIdAsync(int postId)
        {
            var post = await _postRepo.GetByIdAsync(postId);

            return post == null ? null : MapToResponse(post);
        }

        public async Task<List<PostResponse>> GetFeedAsync(
            int userId,
            DateTime? cursor,
            int pageSize)
        {
            return await _postRepo
                .GetFeedWithSocialAsync(userId, cursor, pageSize);
        }

        private async Task<List<string>> UploadImagesAsync(
            List<IFormFile>? images)
        {
            if (images == null || !images.Any())
                return new List<string>();

            if (images.Count > MAX_IMAGES)
                throw new Exception("Maximum 5 images allowed.");

            var tasks = images
                .Select(img => _storageService.UploadImageAsync(img));

            return (await Task.WhenAll(tasks)).ToList();
        }

        private async Task ReplaceImages(Post post, List<IFormFile>? files)
        {
            if (files == null || files.Count == 0)
                return;

            await DeleteImages(post.Images);

            var urls = await UploadImagesAsync(files);

            var images = urls.Select(url => new Image
            {
                ImageUrl = url,
                PostId = post.PostId,
                OwnerType = "Post",
                CreatedAt = DateTime.UtcNow
            }).ToList();

            await _imageRepo.AddRangeAsync(images);

            post.Status = PostStatus.Verifying;

            await SendModerationMessage(post.PostId, urls);
        }

        private async Task DeleteImages(IEnumerable<Image>? images)
        {
            if (images == null)
                return;

            var imageList = images.ToList();

            if (imageList.Count == 0)
                return;

            foreach (var img in imageList)
            {
                await _storageService.DeleteImageAsync(img.ImageUrl);
            }

            _imageRepo.DeleteRange(imageList);
        }

        private async Task SendModerationMessage(
            int postId,
            List<string> imageUrls)
        {
            await _producer.SendMessage(new PostImageMessage
            {
                PostId = postId,
                ImageUrls = imageUrls
            });
        }

        private void UpdateBasicInfo(
            Post post,
            UpdatePostRequest request)
        {
            if (!string.IsNullOrWhiteSpace(request.Tittle))
                post.Tittle = request.Tittle.Trim();

            if (!string.IsNullOrWhiteSpace(request.Content))
                post.Content = request.Content.Trim();

            if (request.IsExpertPost.HasValue)
                post.IsExpertPost = request.IsExpertPost.Value;
        }

        private void ValidatePostContent(
            string? content,
            IEnumerable<IFormFile>? images)
        {
            if (string.IsNullOrWhiteSpace(content)
                && (images == null || !images.Any()))
            {
                throw new Exception(
                    "Post must contain content or images.");
            }
        }

        public async Task<List<PostResponse>> GetPostsByUserAsync(int userId, int pageSize)
        {
            if (pageSize <= 0)
                pageSize = 10;

            if (pageSize > 50)
                pageSize = 50;

            return await _postRepo.GetPostsByUserAsync(userId, pageSize);
        }

        public async Task<List<PostResponse>> GetTrendingPostsAsync(int limit)
        {
            if (limit <= 0)
                limit = 10;

            if (limit > 50)
                limit = 50;

            return await _postRepo.GetTrendingPostsAsync(limit);
        }

        private PostResponse MapToResponse(Post post)
        {
            return new PostResponse
            {
                PostId = post.PostId,
                UserName = post.Account?.UserName,
                AvatarUrl = post.Account?.Avatars
                    ?.OrderByDescending(a => a.CreatedAt)
                    .FirstOrDefault()?.ImageUrl,

                EventId = post.EventId,
                EventName = post.Event?.Title,

                Title = post.Tittle,
                Content = post.Content,

                ImageUrls = post.Images?
                    .OrderBy(i => i.CreatedAt)
                    .Select(i => i.ImageUrl)
                    .ToList(),

                IsExpertPost = post.IsExpertPost,
                Status = post.Status,

                Score = post.Score,
                LikeCount = post.LikeCount,
                ShareCount = post.ShareCount,

                CommentCount = post.Comments?.Count ?? 0,

                CreatedAt = post.CreatedAt,
                UpdatedAt = post.UpdatedAt
            };
        }
    }
}