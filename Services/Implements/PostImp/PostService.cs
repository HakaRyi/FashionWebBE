using Repositories.Constants;
using Repositories.Entities;
using Repositories.Repos.PostRepos;
using Services.RabbitMQ;
using Services.Request.PostReq;
using Services.Response.PostResp;
using Services.Utils;

namespace Services.Implements.PostImp
{
    public class PostService : IPostService
    {
        private readonly IPostRepository _postRepo;
        private readonly ICloudStorageService _storageService;
        private readonly IRabbitMQProducer _producer;

        public PostService(
            IPostRepository postRepo,
            ICloudStorageService storageService,
            IRabbitMQProducer producer)
        {
            _postRepo = postRepo;
            _storageService = storageService;
            _producer = producer;
        }

        private PostResponse MapToResponse(Post post)
        {
            return new PostResponse
            {
                PostId = post.PostId,
                //UserName = post.Account?.Username,
                //AvatarUrl = post.Account?.Avatar,
                EventId = post.EventId,
                EventName = post.Event?.Title,
                Title = post.Tittle,
                Content = post.Content,
                ImageUrls = post.Images?.Select(i => i.ImageUrl).ToList(),
                IsExpertPost = post.IsExpertPost,
                Status = post.Status,
                Score = post.Score,
                LikeCount = post.LikeCount,
                ShareCount = post.ShareCount,
                CreatedAt = post.CreatedAt,
                UpdatedAt = post.UpdatedAt
            };
        }

        public async Task<Post> CreatePostAsync(int accountId, CreatePostRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Content)
                && (request.Images == null || !request.Images.Any()))
            {
                throw new Exception("Post must contain content or images.");
            }

            string status;

            if (!request.IsPublic)
            {
                status = PostStatus.Draft;
            }
            else
            {
                status = request.Images != null && request.Images.Any()
                            ? PostStatus.Verifying
                            : PostStatus.Published;
            }

            var newPost = new Post
            {
                AccountId = accountId,
                Content = request.Content?.Trim(),
                EventId = request.EventId,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                LikeCount = 0,
                ShareCount = 0,
                Score = 0,
                IsExpertPost = false,
                Status = status
            };

            await _postRepo.AddPostAsync(newPost);

            if (status == PostStatus.Verifying && request.Images != null)
            {
                var imageUrls = new List<string>();

                foreach (var file in request.Images)
                {
                    var url = await _storageService.UploadImageAsync(file);
                    imageUrls.Add(url);
                }

                if (imageUrls.Any())
                {
                    var message = new PostImageMessage
                    {
                        PostId = newPost.PostId,
                        ImageUrls = imageUrls
                    };

                    _producer.SendMessage(message);
                }
            }

            return newPost;
        }

        public async Task<List<PostResponse>> GetAllPostAsync()
        {
            var posts = await _postRepo.GetAllPostAsync();
            return posts.Select(MapToResponse).ToList();
        }

        public async Task<List<PostResponse>> GetAllMyPostAsync(int userId)
        {
            var posts = await _postRepo.GetAllMyPostAsync(userId);
            return posts.Select(MapToResponse).ToList();
        }

        public async Task<PostResponse?> GetPostByIdAsync(int postId)
        {
            var post = await _postRepo.GetPostByIdAsync(postId);
            if (post == null) return null;

            return MapToResponse(post);
        }

        public async Task<string> AdminCheckTheStatusPost(CheckPostRequest request, int id)
        {
            if (string.IsNullOrWhiteSpace(request.Status))
                return "Status is required.";

            var newStatus = request.Status.Trim();

            if (!PostStatus.IsValid(newStatus))
                return "Invalid status.";

            var post = await _postRepo.GetPostByIdAsync(id);
            if (post == null)
                return "Post not found.";

            // chỉ cho phép admin xử lý bài đang chờ duyệt
            if (post.Status != PostStatus.PendingAdmin &&
                post.Status != PostStatus.Verifying)
            {
                return "This post is not waiting for moderation.";
            }

            // admin chỉ được phép Published hoặc Rejected
            if (newStatus != PostStatus.Published &&
                newStatus != PostStatus.Rejected &&
                newStatus != PostStatus.PendingAdmin)
            {
                return "Admin can only set Published, Rejected or PendingAdmin.";
            }

            // status không thay đổi thì không cần update
            if (post.Status == newStatus)
                return "Status is already set.";

            post.Status = newStatus;
            post.UpdatedAt = DateTime.UtcNow;

            await _postRepo.UpdatePostAsync(post);

            return "Post moderation updated successfully.";
        }

        public async Task UpdatePostAsync(int postId, int accountId, UpdatePostRequest request)
        {
            var post = await _postRepo.GetPostByIdAsync(postId);
            if (post == null)
                throw new Exception("Post not found");

            if (post.AccountId != accountId)
                throw new Exception("You are not the owner of this post");

            if (!string.IsNullOrWhiteSpace(request.Tittle))
                post.Tittle = request.Tittle.Trim();

            if (!string.IsNullOrWhiteSpace(request.Content))
                post.Content = request.Content.Trim();

            if (request.IsExpertPost.HasValue)
                post.IsExpertPost = request.IsExpertPost.Value;

            post.UpdatedAt = DateTime.UtcNow;

            bool hasNewImages = request.Images != null && request.Images.Any();

            // chỉ cho verify lại nếu bài đang public
            if (hasNewImages && post.Status == PostStatus.Published)
            {
                post.Status = PostStatus.Verifying;
            }

            await _postRepo.UpdatePostAsync(post);

            if (hasNewImages)
            {
                var imageUrls = new List<string>();

                foreach (var file in request.Images)
                {
                    var url = await _storageService.UploadImageAsync(file);
                    imageUrls.Add(url);
                }

                if (imageUrls.Any())
                {
                    var message = new PostImageMessage
                    {
                        PostId = post.PostId,
                        ImageUrls = imageUrls
                    };

                    _producer.SendMessage(message);
                }
            }
        }

        public async Task DeletePostAsync(int postId)
        {
            var post = await _postRepo.GetPostByIdAsync(postId);
            if (post == null)
                throw new Exception("Post not found");

            await _postRepo.DeletePostAsync(post);
        }
    }
}