using Microsoft.AspNetCore.Http;
using Repositories.Constants;
using Repositories.Dto.Common;
using Repositories.Dto.Social.Post;
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

        public async Task<PostResponse> CreatePostAsync(int accountId, CreatePostDto dto)
        {
            ValidateCreatePost(dto?.Content, dto?.Images);

            var now = DateTime.UtcNow;
            var imageUrls = await UploadImages(dto!.Images!.ToList());

            var post = new Post
            {
                AccountId = accountId,
                Title = dto.Title?.Trim(),
                Content = dto.Content?.Trim(),
                EventId = dto.EventId,
                CreatedAt = now,
                UpdatedAt = now,
                Status = PostStatus.Verifying,
                Visibility = PostVisibility.Visible,
                LikeCount = 0,
                CommentCount = 0,
                ShareCount = 0,
                Score = 0,
                IsExpertPost = false
            };

            await _postRepo.AddAsync(post);
            await _uow.SaveChangesAsync();

            var images = imageUrls.Select(url => new Image
            {
                PostId = post.PostId,
                ImageUrl = url,
                OwnerType = "Post",
                CreatedAt = now
            }).ToList();

            await _imageRepo.AddRangeAsync(images);
            await _uow.SaveChangesAsync();

            await SendModeration(post.PostId, imageUrls);

            post.Images = images;

            return MapToResponse(post);
        }

        public async Task UpdatePostAsync(int postId, int accountId, UpdatePostDto dto)
        {
            var post = await _postRepo.GetByIdAsync(postId)
                ?? throw new Exception("Post not found");

            if (post.AccountId != accountId)
                throw new UnauthorizedAccessException();

            if (post.Status == PostStatus.Verifying)
                throw new Exception("Post is being verified and cannot be updated.");

            if (post.Status == PostStatus.PendingAdmin)
                throw new Exception("Post is pending admin review and cannot be updated.");

            var changed = false;

            if (dto.Title != null && post.Title != dto.Title.Trim())
            {
                post.Title = dto.Title.Trim();
                changed = true;
            }

            if (dto.Content != null && post.Content != dto.Content.Trim())
            {
                post.Content = dto.Content.Trim();
                changed = true;
            }

            if (!changed)
                throw new Exception("No changes detected.");

            post.UpdatedAt = DateTime.UtcNow;
            post.Status = PostStatus.Verifying;

            _postRepo.Update(post);
            await _uow.SaveChangesAsync();

            var images = await _imageRepo.GetPostImagesAsync(postId);
            if (images == null || !images.Any())
                throw new Exception("Post must contain at least one image for moderation.");

            await SendModeration(post.PostId, images.Select(i => i.ImageUrl).ToList());
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

        public async Task<string> AdminCheckTheStatusPost(CheckPostRequest request, int id)
        {
            if (string.IsNullOrWhiteSpace(request.Status))
                return "Status is required.";

            var newStatus = request.Status.Trim();

            if (!PostStatus.IsValid(newStatus))
                return "Invalid status.";

            var post = await _postRepo.GetByIdAsync(id);
            if (post == null)
                return "Post not found.";

            var isValidTransition =
                (post.Status == PostStatus.PendingAdmin &&
                 (newStatus == PostStatus.Published || newStatus == PostStatus.Rejected))
                ||
                (post.Status == PostStatus.Rejected &&
                 newStatus == PostStatus.Published);

            if (!isValidTransition)
                return "Invalid status transition.";

            if (post.Status == newStatus)
                return "Status is already set.";

            post.Status = newStatus;
            post.UpdatedAt = DateTime.UtcNow;

            _postRepo.Update(post);
            await _uow.SaveChangesAsync();

            return "Post moderation updated successfully.";
        }

        public async Task<List<PostResponse>> GetAllPostAsync()
        {
            var posts = await _postRepo.GetAllPostAsync();
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

        private PostResponse MapToResponse(Post post)
        {
            return new PostResponse
            {
                PostId = post.PostId,
                AccountId = post.AccountId,

                UserName = post.Account?.UserName,
                AvatarUrl = post.Account?.Avatars?
                    .OrderByDescending(a => a.CreatedAt)
                    .FirstOrDefault()?.ImageUrl,

                EventId = post.EventId,
                EventName = post.Event?.Title,

                Title = post.Title,
                Content = post.Content,

                ImageUrls = post.Images?
                    .OrderBy(i => i.CreatedAt)
                    .Select(i => i.ImageUrl)
                    .ToList() ?? new List<string>(),

                IsExpertPost = post.IsExpertPost,
                Status = post.Status,
                Score = post.Score,

                LikeCount = post.LikeCount,
                CommentCount = post.CommentCount,
                ShareCount = post.ShareCount,

                CreatedAt = post.CreatedAt,
                UpdatedAt = post.UpdatedAt
            };
        }

        public Task<List<PostFeedDto>> GetFeedAsync(int userId, DateTime? cursor, int pageSize)
        {
            if (pageSize <= 0) pageSize = 10;
            if (pageSize > 50) pageSize = 50;

            return _postRepo.GetFeedWithSocialAsync(userId, cursor, pageSize);
        }

        public Task<PostDetailDto?> GetPostDetailAsync(int postId, int userId)
        {
            return _postRepo.GetPostDetailAsync(postId, userId);
        }

        public Task<PagedResultDto<MyPostDto>> GetMyPostsAsync(int ownerId, int page, int pageSize)
        {
            if (page <= 0) page = 1;
            if (pageSize <= 0) pageSize = 10;
            if (pageSize > 50) pageSize = 50;

            return _postRepo.GetMyPostsPagedAsync(ownerId, page, pageSize);
        }

        public Task<PagedResultDto<PostFeedDto>> GetUserPostsAsync(int ownerId, int? viewerId, int page, int pageSize)
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

        public Task<List<PostFeedDto>> GetTrendingPostsAsync(int userId, int limit)
        {
            if (limit <= 0) limit = 10;
            if (limit > 50) limit = 50;

            return _postRepo.GetTrendingPostsAsync(limit, userId);
        }

        public async Task<PostVisibilityResponseDto> HidePostAsync(int postId, int accountId)
        {
            var post = await _postRepo.GetByIdAsync(postId)
                ?? throw new Exception("Post not found");

            if (post.AccountId != accountId)
                throw new UnauthorizedAccessException();

            if (post.Status != PostStatus.Published)
                throw new Exception("Only published posts can be hidden.");

            if (post.Visibility == PostVisibility.Hidden)
            {
                return new PostVisibilityResponseDto
                {
                    PostId = post.PostId,
                    Status = post.Status,
                    Visibility = post.Visibility,
                    IsPubliclyVisible = false,
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

        public async Task<PostVisibilityResponseDto> UnhidePostAsync(int postId, int accountId)
        {
            var post = await _postRepo.GetByIdAsync(postId)
                ?? throw new Exception("Post not found");

            if (post.AccountId != accountId)
                throw new UnauthorizedAccessException();

            if (post.Status != PostStatus.Published)
                throw new Exception("Only published posts can be made visible.");

            if (post.Visibility == PostVisibility.Visible)
            {
                return new PostVisibilityResponseDto
                {
                    PostId = post.PostId,
                    Status = post.Status,
                    Visibility = post.Visibility,
                    IsPubliclyVisible = true,
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
                IsPubliclyVisible = true,
                Message = "Post visible successfully."
            };
        }

        public Task<PagedResultDto<AdminReviewPostDto>> GetPendingAdminPostsAsync(int page, int pageSize)
        {
            if (page <= 0) page = 1;
            if (pageSize <= 0) pageSize = 10;
            if (pageSize > 50) pageSize = 50;

            return _postRepo.GetPendingAdminPostsPagedAsync(page, pageSize);
        }

        public Task<PagedResultDto<AdminReviewPostDto>> GetRejectedPostsAsync(int page, int pageSize)
        {
            if (page <= 0) page = 1;
            if (pageSize <= 0) pageSize = 10;
            if (pageSize > 50) pageSize = 50;

            return _postRepo.GetRejectedPostsPagedAsync(page, pageSize);
        }

        private async Task<List<string>> UploadImages(List<IFormFile> files)
        {
            if (files == null || files.Count == 0)
                throw new Exception("Post must contain at least one image.");

            if (files.Count > MAX_IMAGES)
                throw new Exception("Maximum 5 images allowed.");

            var tasks = files.Select(f => _storage.UploadImageAsync(f));
            return (await Task.WhenAll(tasks)).ToList();
        }

        private Task SendModeration(int postId, List<string> images)
        {
            return _producer.SendMessage(new PostImageMessage
            {
                PostId = postId,
                ImageUrls = images
            });
        }

        private void ValidateCreatePost(string? content, IEnumerable<IFormFile>? images)
        {
            if (images == null || !images.Any())
                throw new Exception("Post must contain at least one image.");

            if (string.IsNullOrWhiteSpace(content) && !images.Any())
                throw new Exception("Post must contain content or images.");
        }

        private void UpdateBasicInfo(Post post, UpdatePostRequest request)
        {
            if (!string.IsNullOrWhiteSpace(request.Tittle))
                post.Title = request.Tittle.Trim();

            if (!string.IsNullOrWhiteSpace(request.Content))
                post.Content = request.Content.Trim();

            if (request.IsExpertPost.HasValue)
                post.IsExpertPost = request.IsExpertPost.Value;

            post.UpdatedAt = DateTime.UtcNow;
        }

        private void ValidatePostContent(string? content, IEnumerable<IFormFile>? images)
        {
            if (string.IsNullOrWhiteSpace(content)
                && (images == null || !images.Any()))

            {
                throw new Exception("Post must contain content or images.");
            }
        }


        // ==============================
        // ADMIN MODERATION
        // ==============================
        public async Task<List<PostResponse>> GetAllPendingAdminAsync()
        {
            List<Post> posts = await _postRepo.GetAllPendingAdminPostAsync();
            return posts.Select(MapToResponse).ToList();
        }

        public async Task<int> UpdatePostStatus(int postId, string status)
        {
            try
            {
                Post post = await _postRepo.GetByIdAsync(postId);
                if (post != null)
                {
                    post.Status = status;
                    post.UpdatedAt = DateTime.UtcNow;
                    var result = await _uow.SaveChangesAsync();
                    return result;
                }
                else
                {
                    throw new KeyNotFoundException("Post not found");
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Error updating post status: {ex.Message}");
            }
        }

        public async Task<PostResponse> CreatePostAsync(int accountId, CreatePostRequest request)
        {
            ValidatePostContent(request.Content, request.Images);

            //if (request.EventId.HasValue)
            //{
            //    var eventExists = await _eventRepo.ExistsAsync(request.EventId.Value);
            //    if (!eventExists) throw new KeyNotFoundException("Event không tồn tại.");
            //}

            var now = DateTime.UtcNow;

            List<string> imageUrls = new List<string>();
            if (request.Images != null && request.Images.Any())
            {
                var uploadTasks = request.Images.Select(f => _storage.UploadImageAsync(f));
                imageUrls = (await Task.WhenAll(uploadTasks)).ToList();
            }

            var initialStatus = imageUrls.Any() ? PostStatus.Verifying : PostStatus.Published;

            var post = new Post
            {
                AccountId = accountId,
                Content = request.Content?.Trim(),
                EventId = request.EventId,
                CreatedAt = now,
                UpdatedAt = now,
                Status = initialStatus,
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
            await _uow.SaveChangesAsync();

            if (imageUrls.Any())
            {
                await _producer.SendMessage(new PostImageMessage
                {
                    PostId = post.PostId,
                    ImageUrls = imageUrls
                });
            }
            return MapToResponse(post);
        }

        public async Task<PostResponse> UpdatePostAsync(int postId, int accountId, UpdatePostRequest request)
        {
            var post = await _postRepo.GetByIdAsync(postId);
            if (post == null) throw new KeyNotFoundException("Bài viết không tồn tại");

            if (post.AccountId != accountId) throw new UnauthorizedAccessException("Không chính chủ");

            post.Content = request.Content?.Trim();
            post.IsExpertPost = request.IsPublish;
            post.UpdatedAt = DateTime.UtcNow;

            if (request.Images != null && request.Images.Any())
            {
                var oldImages = post.Images.ToList();
                _imageRepo.DeleteRange(oldImages);

                var uploadTasks = request.Images.Select(img => _storage.UploadImageAsync(img));
                var newImageUrls = (await Task.WhenAll(uploadTasks)).ToList();

                var newImageEntities = newImageUrls.Select(url => new Image
                {
                    ImageUrl = url,
                    PostId = post.PostId,
                    OwnerType = "Post",
                    CreatedAt = DateTime.UtcNow
                }).ToList();

                await _imageRepo.AddRangeAsync(newImageEntities);

                post.Status = PostStatus.Verifying;

                await _uow.SaveChangesAsync();

                await _producer.SendMessage(new PostImageMessage
                {
                    PostId = post.PostId,
                    ImageUrls = newImageUrls
                });
            }
            else
            {
                await _uow.SaveChangesAsync();
            }

            var updatedPost = await _postRepo.GetByIdAsync(postId);
            return MapToResponse(updatedPost);
        }


        public async Task DeletePostAsync(int postId)
        {
            var post = await _postRepo.GetByIdAsync(postId)
                ?? throw new Exception("Post not found");

            _postRepo.Delete(post);
            await _uow.SaveChangesAsync();
        }

        public async Task SetPostDeleteStatus(int postId)
        {
            var post = await _postRepo.GetByIdAsync(postId);
            post.Status = PostStatus.Deleted;
            post.UpdatedAt = DateTime.UtcNow;
            _postRepo.Update(post);
            await _uow.SaveChangesAsync();
        }

        public async Task SetPostBannedStatus(int postId)
        {
            var post = await _postRepo.GetByIdAsync(postId);
            post.Status = PostStatus.Banned;
            post.UpdatedAt = DateTime.UtcNow;
            _postRepo.Update(post);
            await _uow.SaveChangesAsync();
        }

    }
}