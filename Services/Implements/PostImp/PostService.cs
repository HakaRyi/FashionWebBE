using Microsoft.AspNetCore.Http;
using Repositories.Constants;
using Repositories.Data;
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
        private readonly FashionDbContext _context;

        public PostService(
            IPostRepository postRepo,
            IImageRepository imageRepo,
            ICloudStorageService storageService,
            IRabbitMQProducer producer,
            IUnitOfWork unitOfWork,
            FashionDbContext context)
        {
            _postRepo = postRepo;
            _imageRepo = imageRepo;
            _storageService = storageService;
            _producer = producer;
            _unitOfWork = unitOfWork;
            _context = context;
        }

        // ==============================
        // CREATE POST
        // ==============================
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
                var uploadTasks = request.Images.Select(f => _storageService.UploadImageAsync(f));
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
            await _unitOfWork.SaveChangesAsync();

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

        // ==============================
        // UPDATE POST
        // ==============================
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

                var uploadTasks = request.Images.Select(img => _storageService.UploadImageAsync(img));
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

                await _unitOfWork.SaveChangesAsync();

                await _producer.SendMessage(new PostImageMessage
                {
                    PostId = post.PostId,
                    ImageUrls = newImageUrls
                });
            }
            else
            {
                await _unitOfWork.SaveChangesAsync();
            }

            var updatedPost = await _postRepo.GetByIdAsync(postId);
            return MapToResponse(updatedPost);
        }

        // ==============================
        // DELETE
        // ==============================
        public async Task DeletePostAsync(int postId)
        {
            var post = await _postRepo.GetByIdAsync(postId)
                ?? throw new Exception("Post not found");

            _postRepo.Delete(post);
            await _unitOfWork.SaveChangesAsync();
        }

        // ==============================
        // ADMIN MODERATION
        // ==============================
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

            if (post.Status != PostStatus.PendingAdmin &&
                post.Status != PostStatus.Verifying)
                return "This post is not waiting for moderation.";

            if (post.Status == newStatus)
                return "Status is already set.";

            post.Status = newStatus;
            post.UpdatedAt = DateTime.UtcNow;

            await _unitOfWork.SaveChangesAsync();

            return "Post moderation updated successfully.";
        }

        // ==============================
        // GET METHODS
        // ==============================
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

        // ==============================
        // PRIVATE HELPERS
        // ==============================

        private async Task HandleImageUploadAndModeration(Post post, IEnumerable<IFormFile> files)
        {
            // Upload song song
            var uploadTasks = files.Select(f => _storageService.UploadImageAsync(f));
            var imageUrls = (await Task.WhenAll(uploadTasks)).ToList();

            var imageEntities = imageUrls.Select(url => new Image
            {
                ImageUrl = url,
                PostId = post.PostId,
                OwnerType = "Post",
                CreatedAt = DateTime.UtcNow
            }).ToList();

            await _imageRepo.AddRangeAsync(imageEntities);

            post.Status = PostStatus.Verifying;
            post.UpdatedAt = DateTime.UtcNow;

            await _unitOfWork.SaveChangesAsync();

            _producer.SendMessage(new PostImageMessage
            {
                PostId = post.PostId,
                ImageUrls = imageUrls
            });
        }

        private void UpdateBasicInfo(Post post, UpdatePostRequest request)
        {
            if (!string.IsNullOrWhiteSpace(request.Tittle))
                post.Tittle = request.Tittle.Trim();

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

        public async Task<List<PostResponse>> GetFeedAsync(
            int userId,
            DateTime? cursor,
            int pageSize)
        {
            var posts = await _postRepo
                .GetFeedWithSocialAsync(userId, cursor, pageSize);

            return posts;
        }

        private PostResponse MapToResponse(Post post)
        {
            return new PostResponse
            {
                PostId = post.PostId,

                // ===== USER INFO =====
                UserName = post.Account?.UserName,
                AvatarUrl = post.Account?.Avatars
                                .OrderByDescending(a => a.CreatedAt)
                                .Select(a => a.ImageUrl)
                                .FirstOrDefault(),

                // ===== POST INFO =====
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
                CreatedAt = post.CreatedAt,
                UpdatedAt = post.UpdatedAt,
                AvatarUrl = post.Account?.Avatars?
                        .OrderByDescending(a => a.CreatedAt)
                        .FirstOrDefault()?.ImageUrl,
                UserName = post.Account?.UserName
            };
        }
    }
}