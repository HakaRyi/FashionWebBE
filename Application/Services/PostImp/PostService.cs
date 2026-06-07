using Application.Interfaces;
using Application.RabbitMQ;
using Application.Request.PostReq;
using Application.Response.PostResp;
using Application.Utils;
using Domain.Constants;
using Domain.Contracts.Common;
using Domain.Contracts.Social.Post;
using Domain.Dto.Social.Post;
using Domain.Entities;
using Domain.Interfaces;
using Mapster;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Application.Services.PostImp
{
    public class PostService : IPostService
    {
        private readonly IPostRepository _postRepo;
        private readonly IImageRepository _imageRepo;
        private readonly ICloudStorageService _storage;
        private readonly IRabbitMQProducer _producer;
        private readonly IUnitOfWork _uow;
        private readonly IWalletRepository _walletRepo;
        private readonly IEventRepository _eventRepo;
        private readonly ICurrentUserService _currentUserService;
        private readonly UserManager<Account> _userManager;
        private readonly ICacheService _cacheService;
        private readonly IHashtagRepository _hashtagRepo;

        private const int MAX_IMAGES = 5;

        public PostService(
            IPostRepository postRepo,
            IImageRepository imageRepo,
            ICloudStorageService storage,
            IRabbitMQProducer producer,
            IUnitOfWork uow,
            IWalletRepository walletRepository,
            IEventRepository eventRepository,
            ICurrentUserService currentUserService,
            UserManager<Account> userManager,
            ICacheService cacheService,
            IHashtagRepository hashtagRepo)
        {
            _postRepo = postRepo;
            _imageRepo = imageRepo;
            _storage = storage;
            _producer = producer;
            _uow = uow;
            _walletRepo = walletRepository;
            _eventRepo = eventRepository;
            _currentUserService = currentUserService;
            _userManager = userManager;
            _cacheService = cacheService;
            _hashtagRepo = hashtagRepo;
        }

        public async Task<PostResponse> CreatePostAsync(
            int accountId,
            CreatePostDto dto)
        {
            ValidateCreatePost(dto?.Content, dto?.Images);

            var now = DateTime.UtcNow;
            var imageUrls = await UploadImages(dto!.Images!.ToList());

            var account = await _userManager.Users
                .Include(x => x.ExpertProfile)
                .FirstOrDefaultAsync(x => x.Id == accountId)
                ?? throw new KeyNotFoundException("Account not found");

            var isExpertPost =
                account.ExpertProfile?.Verified == true;

            var post = new Post
            {
                AccountId = accountId,
                Title = dto.Title?.Trim(),
                Content = dto.Content?.Trim(),
                EventId = dto.EventId,
                CreatedAt = now,
                UpdatedAt = now,
                Status = PostStatus.Published,
                Visibility = PostVisibility.Visible,
                LikeCount = 0,
                CommentCount = 0,
                ShareCount = 0,
                IsExpertPost = isExpertPost
            };

            await _postRepo.AddAsync(post);
            await _uow.SaveChangesAsync();

            await SyncHashtagsAsync(
                post,
                dto.Hashtags);

            var images = imageUrls.Select(url => new Image
            {
                PostId = post.PostId,
                ImageUrl = url,
                OwnerType = "Post",
                CreatedAt = now
            }).ToList();

            await _imageRepo.AddRangeAsync(images);

            post.Images = images;

            _postRepo.Update(post);
            await _uow.SaveChangesAsync();

            await SendModeration(post.PostId, imageUrls);

            post.Account = account;

            account.CountPost += 1;
            await _userManager.UpdateAsync(account);

            await _cacheService.RemoveDataAsync(
                $"my_profile_{accountId}");

            var createdPost =
                await _postRepo.GetByIdAsync(post.PostId)
                ?? throw new KeyNotFoundException("Post not found.");

            return MapToResponse(createdPost);
        }

        public async Task<PostResponse> UpdatePostAsync(
            int postId,
            int accountId,
            UpdatePostDto dto)
        {
            if (dto == null)
                throw new ArgumentNullException(nameof(dto));

            var post = await _postRepo.GetByIdAsync(postId)
                ?? throw new KeyNotFoundException("Post not found.");

            if (post.AccountId != accountId)
                throw new UnauthorizedAccessException(
                    "You are not the owner of this post.");

            if (post.Status == PostStatus.Deleted)
                throw new InvalidOperationException(
                    "Deleted post cannot be updated.");

            if (post.Status == PostStatus.Banned)
                throw new InvalidOperationException(
                    "Banned post cannot be updated.");

            bool hasTextChange = false;
            bool hasImageChange =
                dto.Images != null && dto.Images.Any();

            bool hasHashtagChange =
                dto.Hashtags != null;

            var newTitle = dto.Title?.Trim();
            var newContent = dto.Content?.Trim();

            if (dto.Title != null &&
                post.Title != newTitle)
            {
                post.Title = newTitle;
                hasTextChange = true;
            }

            if (dto.Content != null &&
                post.Content != newContent)
            {
                post.Content = newContent;
                hasTextChange = true;
            }

            if (!hasTextChange &&
                !hasImageChange &&
                !hasHashtagChange)
            {
                throw new InvalidOperationException(
                    "No changes detected.");
            }

            if (hasImageChange)
            {
                if (dto.Images!.Count > MAX_IMAGES)
                    throw new InvalidOperationException(
                        "Maximum 5 images allowed.");

                var oldImages =
                    post.Images?.ToList()
                    ?? new List<Image>();

                var uploadTasks = dto.Images
                    .Select(x => _storage.UploadImageAsync(x));

                var newImageUrls =
                    (await Task.WhenAll(uploadTasks))
                    .ToList();

                var newImages = newImageUrls
                    .Select(url => new Image
                    {
                        PostId = post.PostId,
                        ImageUrl = url,
                        OwnerType = "Post",
                        CreatedAt = DateTime.UtcNow
                    })
                    .ToList();

                if (oldImages.Any())
                {
                    _imageRepo.DeleteRange(oldImages);
                }

                await _imageRepo.AddRangeAsync(newImages);

                post.Images = newImages;

                foreach (var oldImage in oldImages)
                {
                    await _storage.DeleteImageAsync(
                        oldImage.ImageUrl);
                }
            }

            await SyncHashtagsAsync(
                post,
                dto.Hashtags);

            post.Status = PostStatus.Published;
            post.Visibility = PostVisibility.Visible;
            post.UpdatedAt = DateTime.UtcNow;

            _postRepo.Update(post);

            await _uow.SaveChangesAsync();

            var updatedPost =
                await _postRepo.GetByIdAsync(postId)
                ?? throw new KeyNotFoundException(
                    "Post not found after update.");

            return MapToResponse(updatedPost);
        }

        public async Task DeletePostAsync(int postId, int accountId)
        {
            var post = await _postRepo.GetByIdAsync(postId)
                ?? throw new KeyNotFoundException("Post not found.");

            if (post.AccountId != accountId)
                throw new UnauthorizedAccessException("You are not the owner of this post.");

            if (post.Status == PostStatus.Deleted)
                return;

            if (post.Status == PostStatus.Banned)
                throw new InvalidOperationException("Banned post cannot be deleted by user.");

            post.Status = PostStatus.Deleted;
            post.Visibility = PostVisibility.Hidden;
            post.UpdatedAt = DateTime.UtcNow;

            _postRepo.Update(post);

            var account = await _userManager.FindByIdAsync(accountId.ToString())
                ?? throw new KeyNotFoundException("Account not found.");

            account.CountPost = Math.Max(account.CountPost - 1, 0);

            await _userManager.UpdateAsync(account);
            await _uow.SaveChangesAsync();

            await _cacheService.RemoveDataAsync($"my_profile_{accountId}");
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
                post.Status == PostStatus.PendingAdmin &&
                 (newStatus == PostStatus.Published || newStatus == PostStatus.Rejected)
                ||
                post.Status == PostStatus.Rejected &&
                 newStatus == PostStatus.Published;

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
            var currentUserId = _currentUserService.GetUserId();

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

                Hashtags = post.PostHashtags?
                    .Select(ph => ph.Hashtag.Name)
                    .ToList() ?? [],

                IsExpertPost = post.IsExpertPost ?? false,

                IsLikedByExpert = post.Reactions.Any(r =>
                    r.Account != null &&
                    r.Account.ExpertProfile != null &&
                    r.Account.ExpertProfile.Verified == true
                ),

                Status = post.Status,

                IsLiked = currentUserId.HasValue &&
                          post.Reactions.Any(r => r.AccountId == currentUserId.Value),

                IsSaved = currentUserId.HasValue &&
                          post.Saves.Any(s => s.AccountId == currentUserId.Value),

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

        public async Task<List<PostResponse>> GetPostsByEventIdAsync(int eventId)
        {
            var posts = await _postRepo.GetPostsByEventIdAsync(eventId);

            var response = posts.Adapt<List<PostResponse>>();

            foreach (var res in response)
            {
                var originalPost = posts.FirstOrDefault(p => p.PostId == res.PostId);
                if (originalPost?.Scoreboard != null)
                {
                    res.Score = originalPost.Scoreboard.FinalScore;
                    res.Reason = originalPost.Scoreboard.ExpertReason;
                }
            }

            return response;
        }

        public async Task<List<PostResponse>> GetPostsForExpertReviewAsync(int eventId)
        {
            int currentExpertId = _currentUserService.GetRequiredUserId();

            var posts = (await _postRepo.GetPostsForReview(eventId, currentExpertId)).ToList();

            var responses = posts.Adapt<List<PostResponse>>();

            for (int i = 0; i < posts.Count; i++)
            {
                var rating = posts[i].ExpertRatings?
                    .FirstOrDefault(r => r.ExpertId == currentExpertId);

                if (rating != null)
                {
                    responses[i].Score = rating.Score;
                    responses[i].Reason = rating.Reason;

                    if (rating.CriterionRatings != null)
                    {
                        responses[i].CriterionRatings = rating.CriterionRatings
                            .Select(cr => new CriterionRatingResponse
                            {
                                EventCriterionId = cr.EventCriterionId,
                                Score = cr.Score
                            }).ToList();
                    }
                }
            }

            return responses;
        }

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

        public async Task<PostResponse> CreatePostAsync(
            int accountId,
            CreatePostRequest request)
        {
            ValidatePostContent(
                request.Content,
                request.Images);

            var now = DateTime.UtcNow;

            List<string> imageUrls = new();

            if (request.Images != null &&
                request.Images.Any())
            {
                var uploadTasks =
                    request.Images.Select(
                        x => _storage.UploadImageAsync(x));

                imageUrls =
                    (await Task.WhenAll(uploadTasks))
                    .ToList();
            }

            var initialStatus =
                imageUrls.Any()
                    ? PostStatus.Verifying
                    : PostStatus.Published;

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

            await SyncHashtagsAsync(
                post,
                request.Hashtags);

            await _uow.SaveChangesAsync();

            if (imageUrls.Any())
            {
                await _producer.SendMessage(
                    new PostImageMessage
                    {
                        PostId = post.PostId,
                        ImageUrls = imageUrls
                    });
            }

            var createdPost =
                await _postRepo.GetByIdAsync(post.PostId)
                ?? throw new KeyNotFoundException(
                    "Post not found.");

            return MapToResponse(createdPost);
        }

        public async Task<PostResponse> UpdatePostAsync(
            int postId,
            int accountId,
            UpdatePostRequest request)
        {
            var post = await _postRepo.GetByIdAsync(postId)
                ?? throw new KeyNotFoundException(
                    "Post not found.");

            if (post.AccountId != accountId)
            {
                throw new UnauthorizedAccessException(
                    "You are not the owner of this post.");
            }

            var account = await _userManager.Users
                .Include(x => x.ExpertProfile)
                .FirstOrDefaultAsync(x => x.Id == accountId)
                ?? throw new KeyNotFoundException(
                    "Account not found");

            post.Content = request.Content?.Trim();
            post.IsExpertPost =
                account.ExpertProfile?.Verified == true;
            post.UpdatedAt = DateTime.UtcNow;

            if (request.Images != null &&
                request.Images.Any())
            {
                var oldImages = post.Images.ToList();

                _imageRepo.DeleteRange(oldImages);

                var uploadTasks = request.Images
                    .Select(x => _storage.UploadImageAsync(x));

                var newImageUrls =
                    (await Task.WhenAll(uploadTasks))
                    .ToList();

                var newImageEntities =
                    newImageUrls.Select(url => new Image
                    {
                        ImageUrl = url,
                        PostId = post.PostId,
                        OwnerType = "Post",
                        CreatedAt = DateTime.UtcNow
                    }).ToList();

                await _imageRepo.AddRangeAsync(
                    newImageEntities);

                post.Images = newImageEntities;
                post.Status = PostStatus.Verifying;

                await _producer.SendMessage(
                    new PostImageMessage
                    {
                        PostId = post.PostId,
                        ImageUrls = newImageUrls
                    });
            }

            await SyncHashtagsAsync(
                post,
                request.Hashtags);

            _postRepo.Update(post);

            await _uow.SaveChangesAsync();

            var updatedPost =
                await _postRepo.GetByIdAsync(postId)
                ?? throw new KeyNotFoundException(
                    "Post not found.");

            return MapToResponse(updatedPost);
        }

        public async Task<int> SharePostAsync(int postId)
        {
            var post = await _postRepo.GetByIdAsync(postId)
                ?? throw new KeyNotFoundException("Post not found.");

            if (post.Status != PostStatus.Published)
                throw new Exception("Only published posts can be shared.");

            if (post.Visibility != PostVisibility.Visible)
                throw new Exception("Only visible posts can be shared.");

            post.ShareCount = (post.ShareCount ?? 0) + 1;
            post.UpdatedAt = DateTime.UtcNow;

            _postRepo.Update(post);
            await _uow.SaveChangesAsync();

            return post.ShareCount ?? 0;
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
            var post = await _postRepo.GetByIdAsync(postId)
                ?? throw new KeyNotFoundException("Post not found.");

            if (post.Status == PostStatus.Deleted)
                return;

            post.Status = PostStatus.Deleted;
            post.Visibility = PostVisibility.Hidden;
            post.UpdatedAt = DateTime.UtcNow;

            _postRepo.Update(post);
            await _uow.SaveChangesAsync();
        }

        public async Task SetPostBannedStatus(int postId)
        {
            var post = await _postRepo.GetByIdAsync(postId)
                ?? throw new KeyNotFoundException("Post not found.");

            if (post.Status == PostStatus.Banned)
                return;

            post.Status = PostStatus.Banned;
            post.Visibility = PostVisibility.Hidden;
            post.UpdatedAt = DateTime.UtcNow;

            _postRepo.Update(post);
            await _uow.SaveChangesAsync();
        }

        public async Task<GlobalSearchResultDto> SearchEverythingAsync(string keyword, int? viewerId)
        {
            if (string.IsNullOrWhiteSpace(keyword)) return new GlobalSearchResultDto();

            var (postEntities, userEntities) = await _postRepo.SearchRawDataAsync(keyword, 5);

            List<int> likedPostIds = new();
            if (viewerId.HasValue && postEntities.Any())
            {
                var postIds = postEntities.Select(p => p.PostId).ToList();
                likedPostIds = await _postRepo.GetLikedPostIdsAsync(viewerId.Value, postIds);
            }

            var userDtos = userEntities.Select(u => new UserSearchDto
            {
                AccountId = u.Id,
                UserName = u.UserName!,
                AvatarUrl = u.Avatars.OrderByDescending(a => a.CreatedAt).Select(a => a.ImageUrl).FirstOrDefault(),
                IsExpert = u.ExpertProfile?.Verified ?? false,
                ExpertiseField = u.ExpertProfile?.ExpertiseField,
                FollowerCount = u.CountFollower
            }).ToList();

            var postDtos = postEntities.Select(p => new PostFeedDto
            {
                PostId = p.PostId,
                AccountId = p.AccountId,
                UserName = p.Account.UserName!,
                AvatarUrl = p.Account.Avatars.OrderByDescending(a => a.CreatedAt).Select(a => a.ImageUrl).FirstOrDefault(),
                Title = p.Title,
                Content = p.Content,
                Images = p.Images.Select(i => i.ImageUrl).ToList(),
                LikeCount = p.LikeCount ?? 0,
                CommentCount = p.CommentCount ?? 0,
                CreatedAt = p.CreatedAt ?? DateTime.UtcNow,

                Hashtags = p.PostHashtags
                    .Select(ph => ph.Hashtag.Name)
                    .ToList(),

                IsLiked = likedPostIds.Contains(p.PostId),
                IsExpertPost = p.IsExpertPost ?? false
            }).ToList();

            return new GlobalSearchResultDto
            {
                Users = userDtos,
                Posts = postDtos
            };
        }

        public async Task<List<PostFeedDto>> GetPostsByTagAsync(string tagName, int viewerId, DateTime? cursor, int pageSize)
        {
            return await _postRepo.GetPostsByHashtagAsync(tagName, viewerId, cursor, pageSize);
        }

        private async Task SyncHashtagsAsync(
            Post post,
            IEnumerable<string>? hashtags)
        {
            if (hashtags == null)
                return;

            var normalizedTags = hashtags
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Select(x => x.Trim().ToLower())
                .Distinct()
                .ToList();

            var existingTags =
                await _hashtagRepo.GetByNamesAsync(normalizedTags);

            var existingNames = existingTags
                .Select(x => x.Name.ToLower())
                .ToHashSet();

            var newTags = normalizedTags
                .Where(x => !existingNames.Contains(x))
                .Select(x => new Hashtag
                {
                    Name = x,
                    UsageCount = 0
                })
                .ToList();

            if (newTags.Any())
            {
                await _hashtagRepo.AddRangeAsync(newTags);
                await _uow.SaveChangesAsync();

                existingTags.AddRange(newTags);
            }

            var currentLinks = post.PostHashtags?
                .Select(x => x.HashtagId)
                .ToHashSet()
                ?? [];

            var desiredLinks = existingTags
                .Select(x => x.HashtagId)
                .ToHashSet();

            post.PostHashtags ??= [];

            var removeItems = post.PostHashtags
                .Where(ph => !desiredLinks.Contains(ph.HashtagId))
                .ToList();

            foreach (var item in removeItems)
            {
                post.PostHashtags.Remove(item);
            }

            foreach (var tag in existingTags)
            {
                if (!currentLinks.Contains(tag.HashtagId))
                {
                    post.PostHashtags.Add(new PostHashtag
                    {
                        PostId = post.PostId,
                        HashtagId = tag.HashtagId
                    });

                    tag.UsageCount++;
                    _hashtagRepo.Update(tag);
                }
            }
        }
    }
}