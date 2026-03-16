using Microsoft.AspNetCore.Http;
using Repositories.Constants;
using Repositories.Dto.Common;
using Repositories.Dto.Social.Post;
using Repositories.Entities;
using Repositories.Repos.ImageRepos;
using Repositories.Repos.PostRepos;
using Repositories.UnitOfWork;
using Services.RabbitMQ;
using Services.Utils;

namespace Services.Implements.PostImp
{
    public class PostService : IPostService
    {
        private readonly IPostRepository _postRepo;
        private readonly IImageRepository _imageRepo;
        private readonly ICloudStorageService _storage;
        private readonly IRabbitMQProducer _producer;
        private readonly IUnitOfWork _uow;

        private const int MAX_IMAGES = 5;

        public PostService(
            IPostRepository postRepo,
            IImageRepository imageRepo,
            ICloudStorageService storage,
            IRabbitMQProducer producer,
            IUnitOfWork uow)
        {
            _postRepo = postRepo;
            _imageRepo = imageRepo;
            _storage = storage;
            _producer = producer;
            _uow = uow;
        }

        public async Task<int> CreatePostAsync(
            int accountId,
            CreatePostDto dto,
            List<IFormFile>? files)
        {
            ValidatePost(dto, files);

            var now = DateTime.UtcNow;
            var imageUrls = await UploadImages(files);

            var post = new Post
            {
                AccountId = accountId,
                Tittle = dto.Title?.Trim(),
                Content = dto.Content?.Trim(),
                EventId = dto.EventId,
                CreatedAt = now,
                UpdatedAt = now,
                Status = imageUrls.Any()
                    ? PostStatus.Verifying
                    : PostStatus.Published,
                Visibility = PostVisibility.Visible,
                LikeCount = 0,
                CommentCount = 0,
                ShareCount = 0,
                Score = 0
            };

            await _postRepo.AddAsync(post);
            await _uow.SaveChangesAsync();

            if (imageUrls.Any())
            {
                var images = imageUrls.Select(x => new Image
                {
                    PostId = post.PostId,
                    ImageUrl = x,
                    OwnerType = "Post",
                    CreatedAt = now
                }).ToList();

                await _imageRepo.AddRangeAsync(images);
                await SendModeration(post.PostId, imageUrls);
            }

            await _uow.SaveChangesAsync();

            return post.PostId;
        }

        public async Task UpdatePostAsync(
            int postId,
            int accountId,
            UpdatePostDto dto)
        {
            var post = await _postRepo.GetByIdAsync(postId)
                ?? throw new Exception("Post not found");

            if (post.AccountId != accountId)
                throw new UnauthorizedAccessException();

            if (post.Status == PostStatus.Verifying)
                throw new Exception("Post is being verified and cannot be updated.");

            if (post.Status == PostStatus.Rejected)
                throw new Exception("Rejected post cannot be updated.");

            if (!string.IsNullOrWhiteSpace(dto.Title))
                post.Tittle = dto.Title.Trim();

            if (!string.IsNullOrWhiteSpace(dto.Content))
                post.Content = dto.Content.Trim();

            post.UpdatedAt = DateTime.UtcNow;

            _postRepo.Update(post);
            await _uow.SaveChangesAsync();
        }

        public async Task DeletePostAsync(int postId, int accountId)
        {
            var post = await _postRepo.GetByIdAsync(postId)
                ?? throw new Exception("Post not found");

            if (post.AccountId != accountId)
                throw new UnauthorizedAccessException();

            var images = await _imageRepo.GetPostImagesAsync(postId);

            foreach (var img in images)
            {
                await _storage.DeleteImageAsync(img.ImageUrl);
            }

            _imageRepo.DeleteRange(images);
            _postRepo.Delete(post);

            await _uow.SaveChangesAsync();
        }

        public Task<List<PostFeedDto>> GetFeedAsync(
            int userId,
            DateTime? cursor,
            int pageSize)
        {
            if (pageSize <= 0) pageSize = 10;
            if (pageSize > 50) pageSize = 50;

            return _postRepo.GetFeedWithSocialAsync(userId, cursor, pageSize);
        }

        public Task<PostDetailDto?> GetPostDetailAsync(
            int postId,
            int userId)
        {
            return _postRepo.GetPostDetailAsync(postId, userId);
        }

        public Task<PagedResultDto<MyPostDto>> GetMyPostsAsync(
            int ownerId,
            int page,
            int pageSize)
        {
            if (page <= 0) page = 1;
            if (pageSize <= 0) pageSize = 10;
            if (pageSize > 50) pageSize = 50;

            return _postRepo.GetMyPostsPagedAsync(ownerId, page, pageSize);
        }

        public Task<PagedResultDto<PostFeedDto>> GetUserPostsAsync(
            int ownerId,
            int? viewerId,
            int page,
            int pageSize)
        {
            if (page <= 0) page = 1;
            if (pageSize <= 0) pageSize = 10;
            if (pageSize > 50) pageSize = 50;

            return _postRepo.GetUserPublicPostsPagedAsync(
                ownerId,
                viewerId,
                page,
                pageSize);
        }

        public Task<List<PostFeedDto>> GetTrendingPostsAsync(
            int userId,
            int limit)
        {
            if (limit <= 0) limit = 10;
            if (limit > 50) limit = 50;

            return _postRepo.GetTrendingPostsAsync(limit, userId);
        }

        public async Task<PostVisibilityResponseDto> HidePostAsync(
            int postId,
            int accountId)
        {
            var post = await _postRepo.GetByIdAsync(postId)
                ?? throw new Exception("Post not found");

            if (post.AccountId != accountId)
                throw new UnauthorizedAccessException();

            if (post.Status == PostStatus.Rejected)
                throw new Exception("Rejected post cannot be hidden.");

            if (post.Visibility == PostVisibility.Hidden)
            {
                return new PostVisibilityResponseDto
                {
                    PostId = post.PostId,
                    Status = post.Status,
                    Visibility = post.Visibility,
                    IsPubliclyVisible = post.Status == PostStatus.Published
                                     && post.Visibility == PostVisibility.Visible,
                    Message = "Post is already hidden."
                };
            }

            post.Visibility = PostVisibility.Hidden;
            post.UpdatedAt = DateTime.UtcNow;

            _postRepo.Update(post);
            await _uow.SaveChangesAsync();

            return new PostVisibilityResponseDto
            {
                PostId = post.PostId,
                Status = post.Status,
                Visibility = post.Visibility,
                IsPubliclyVisible = false,
                Message = "Post hidden successfully."
            };
        }

        public async Task<PostVisibilityResponseDto> UnhidePostAsync(
            int postId,
            int accountId)
        {
            var post = await _postRepo.GetByIdAsync(postId)
                ?? throw new Exception("Post not found");

            if (post.AccountId != accountId)
                throw new UnauthorizedAccessException();

            if (post.Status == PostStatus.Rejected)
                throw new Exception("Rejected post cannot be unhidden.");

            if (post.Visibility == PostVisibility.Visible)
            {
                return new PostVisibilityResponseDto
                {
                    PostId = post.PostId,
                    Status = post.Status,
                    Visibility = post.Visibility,
                    IsPubliclyVisible = post.Status == PostStatus.Published
                                     && post.Visibility == PostVisibility.Visible,
                    Message = "Post is already visible."
                };
            }

            post.Visibility = PostVisibility.Visible;
            post.UpdatedAt = DateTime.UtcNow;

            _postRepo.Update(post);
            await _uow.SaveChangesAsync();

            return new PostVisibilityResponseDto
            {
                PostId = post.PostId,
                Status = post.Status,
                Visibility = post.Visibility,
                IsPubliclyVisible = post.Status == PostStatus.Published
                                 && post.Visibility == PostVisibility.Visible,
                Message = "Post visible successfully."
            };
        }

        private async Task<List<string>> UploadImages(
            List<IFormFile>? files)
        {
            if (files == null || files.Count == 0)
                return new List<string>();

            if (files.Count > MAX_IMAGES)
                throw new Exception("Maximum 5 images allowed");

            var tasks = files.Select(f => _storage.UploadImageAsync(f));

            return (await Task.WhenAll(tasks)).ToList();
        }

        private Task SendModeration(
            int postId,
            List<string> images)
        {
            return _producer.SendMessage(new PostImageMessage
            {
                PostId = postId,
                ImageUrls = images
            });
        }

        private void ValidatePost(
            CreatePostDto dto,
            List<IFormFile>? images)
        {
            if (string.IsNullOrWhiteSpace(dto.Content)
                && (images == null || images.Count == 0))
            {
                throw new Exception("Post must contain content or images.");
            }
        }
    }
}