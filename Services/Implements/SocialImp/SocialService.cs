using Repositories.Constants;
using Repositories.Dto.Social.Comment;
using Repositories.Dto.Social.Post;
using Repositories.Entities;
using Repositories.Repos.CommentReactionRepos;
using Repositories.Repos.CommentRepos;
using Repositories.Repos.PostRepos;
using Repositories.Repos.ReactionRepos;
using Repositories.UnitOfWork;

namespace Services.Implements.SocialImp
{
    public class SocialService : ISocialService
    {
        private const int MaxContentLength = 2000;

        private readonly ICommentRepository _commentRepo;
        private readonly ICommentReactionRepository _commentReactionRepo;
        private readonly IReactionRepository _reactionRepo;
        private readonly IPostRepository _postRepo;
        private readonly IUnitOfWork _uow;

        public SocialService(
            ICommentRepository commentRepo,
            ICommentReactionRepository commentReactionRepo,
            IReactionRepository reactionRepo,
            IPostRepository postRepo,
            IUnitOfWork uow)
        {
            _commentRepo = commentRepo;
            _commentReactionRepo = commentReactionRepo;
            _reactionRepo = reactionRepo;
            _postRepo = postRepo;
            _uow = uow;
        }

        public async Task<PagedCommentsResponseDto> GetCommentsAsync(int userId, int postId, int skip, int take)
        {
            NormalizePaging(ref skip, ref take);

            var post = await _postRepo.GetByIdAsync(postId)
                ?? throw new Exception("Post not found");

            EnsurePostCanInteract(post, userId);

            var roots = await _commentRepo.GetRootCommentsByPostIdAsync(postId, skip, take);
            var totalCount = await _commentRepo.CountRootCommentsByPostIdAsync(postId);

            var rootIds = roots.Select(c => c.CommentId).ToList();

            var reactedIds = rootIds.Count == 0
                ? new List<int>()
                : await _commentReactionRepo.GetUserReactedCommentIdsAsync(userId, rootIds);

            var replyCounts = await _commentRepo.GetReplyCountsByParentIdsAsync(rootIds);

            var items = roots.Select(root =>
            {
                var replyCount = replyCounts.TryGetValue(root.CommentId, out var count)
                    ? count
                    : 0;

                var isLiked = reactedIds.Contains(root.CommentId);

                return MapComment(root, isLiked, replyCount);
            }).ToList();

            return new PagedCommentsResponseDto
            {
                Items = items,
                TotalCount = totalCount,
                Skip = skip,
                Take = take,
                HasMore = skip + items.Count < totalCount
            };
        }

        public async Task<CommentRepliesResponseDto> GetRepliesAsync(int userId, int parentCommentId, int skip, int take)
        {
            NormalizePaging(ref skip, ref take);

            var parent = await _commentRepo.GetByIdAsync(parentCommentId)
                ?? throw new Exception("Parent comment not found");

            EnsurePostCanInteract(parent.Post, userId);

            if (parent.ParentCommentId != null)
                throw new Exception("Only root comments can have replies");

            var totalCount = await _commentRepo.CountRepliesAsync(parentCommentId);
            var replies = await _commentRepo.GetRepliesAsync(parentCommentId, skip, take);

            var replyIds = replies.Select(r => r.CommentId).ToList();

            var reactedIds = replyIds.Count == 0
                ? new List<int>()
                : await _commentReactionRepo.GetUserReactedCommentIdsAsync(userId, replyIds);

            var items = replies.Select(reply =>
            {
                var isLiked = reactedIds.Contains(reply.CommentId);
                return MapReply(reply, isLiked);
            }).ToList();

            return new CommentRepliesResponseDto
            {
                ParentCommentId = parentCommentId,
                ReplyCount = totalCount,
                HasReplies = totalCount > 0,
                Items = items,
                Skip = skip,
                Take = take,
                HasMore = skip + items.Count < totalCount
            };
        }

        public async Task<CommentDto> CreateCommentAsync(int userId, int postId, CreateCommentRequestDto dto)
        {
            var content = NormalizeContent(dto?.Content, "Comment");

            var post = await _postRepo.GetByIdAsync(postId)
                ?? throw new Exception("Post not found");

            EnsurePostCanInteract(post, userId);

            var comment = new Comment
            {
                PostId = postId,
                AccountId = userId,
                Content = content,
                CreatedAt = DateTime.UtcNow,
                LikeCount = 0,
                ParentCommentId = null
            };

            await _commentRepo.AddAsync(comment);

            post.CommentCount = (post.CommentCount ?? 0) + 1;

            await _uow.SaveChangesAsync();

            var created = await _commentRepo.GetByIdAsync(comment.CommentId)
                ?? throw new Exception("Created comment not found");

            return MapComment(created, false, 0);
        }

        public async Task<CreateReplyResultDto> ReplyCommentAsync(int userId, int parentCommentId, CreateReplyDto dto)
        {
            var content = NormalizeContent(dto?.Content, "Reply");

            var parent = await _commentRepo.GetByIdAsync(parentCommentId)
                ?? throw new Exception("Parent comment not found");

            EnsurePostCanInteract(parent.Post, userId);

            if (parent.ParentCommentId != null)
                throw new Exception("Only one-level replies are supported");

            var reply = new Comment
            {

                PostId = parent.PostId,
                AccountId = userId,
                ParentCommentId = parent.CommentId,
                Content = content,
                CreatedAt = DateTime.UtcNow,
                LikeCount = 0

            };

            await _commentRepo.AddAsync(reply);

            var post = await _postRepo.GetByIdAsync(parent.PostId);
            if (post != null)
            {
                post.CommentCount = (post.CommentCount ?? 0) + 1;
            }

            await _uow.SaveChangesAsync();

            var created = await _commentRepo.GetByIdAsync(reply.CommentId)
                ?? throw new Exception("Created reply not found");

            var replyCount = await _commentRepo.CountRepliesAsync(parentCommentId);

            return new CreateReplyResultDto
            {
                Reply = MapReply(created, false),
                ParentCommentId = parentCommentId,
                ReplyCount = replyCount,
                HasReplies = replyCount > 0
            };
        }

        public async Task UpdateCommentAsync(int commentId, int userId, CommentRequest request)
        {
            var newContent = NormalizeContent(request?.Content, "Comment");

            var comment = await _commentRepo.GetByIdAsync(commentId)
                ?? throw new Exception("Comment not found");

            EnsurePostCanInteract(comment.Post, userId);

            if (comment.AccountId != userId)
                throw new UnauthorizedAccessException("You cannot edit this comment");

            if (comment.Content == newContent)
                return;

            comment.Content = newContent;
            comment.UpdatedAt = DateTime.UtcNow;

            _commentRepo.Update(comment);
            await _uow.SaveChangesAsync();
        }

        public async Task<PostReactionResultDto> TogglePostReactionAsync(int userId, int postId)
        {
            var post = await _postRepo.GetByIdAsync(postId)
                ?? throw new Exception("Post not found");

            EnsurePostCanInteract(post, userId);

            var reaction = await _reactionRepo.GetAsync(userId, postId);

            bool isLiked;

            if (reaction == null)
            {
                await _reactionRepo.AddAsync(new Reaction
                {
                    AccountId = userId,
                    PostId = postId,
                    CreatedAt = DateTime.UtcNow
                });

                post.LikeCount = (post.LikeCount ?? 0) + 1;
                isLiked = true;
            }

            //reaction.ReactionType = request.ReactionType;
            //return await _socialRepository.UpdateReact(reaction);

            else
            {
                _reactionRepo.Remove(reaction);
                post.LikeCount = Math.Max(0, (post.LikeCount ?? 0) - 1);
                isLiked = false;
            }

            await _uow.SaveChangesAsync();

            return new PostReactionResultDto
            {
                IsLiked = isLiked,
                LikeCount = post.LikeCount ?? 0
            };
        }

        public async Task<CommentReactionResultDto> ToggleCommentReactionAsync(int userId, int commentId)
        {
            var comment = await _commentRepo.GetByIdAsync(commentId)
                ?? throw new Exception("Comment not found");

            EnsurePostCanInteract(comment.Post, userId);

            var reaction = await _commentReactionRepo.GetAsync(userId, commentId);

            bool isLiked;

            if (reaction == null)
            {
                await _commentReactionRepo.AddAsync(new CommentReaction
                {
                    AccountId = userId,
                    CommentId = commentId,
                    CreatedAt = DateTime.UtcNow
                });

                comment.LikeCount++;
                isLiked = true;
            }
            else
            {
                _commentReactionRepo.Remove(reaction);
                comment.LikeCount = Math.Max(0, comment.LikeCount - 1);
                isLiked = false;
            }

            await _uow.SaveChangesAsync();

            return new CommentReactionResultDto
            {
                CommentId = commentId,
                IsLiked = isLiked,
                LikeCount = comment.LikeCount
            };
        }

        public async Task DeleteCommentAsync(int commentId, int userId)
        {
            var comment = await _commentRepo.GetByIdAsync(commentId)
                ?? throw new Exception("Comment not found");

            EnsurePostCanInteract(comment.Post, userId);

            if (comment.AccountId != userId)
                throw new UnauthorizedAccessException("You cannot delete this comment");

            var toDelete = await _commentRepo.GetCommentWithDirectRepliesAsync(commentId);

            foreach (var item in toDelete)
            {
                await _commentReactionRepo.DeleteByCommentIdAsync(item.CommentId);
            }

            _commentRepo.DeleteRange(toDelete);

            var post = await _postRepo.GetByIdAsync(comment.PostId);
            if (post != null)
            {
                post.CommentCount = Math.Max(0, (post.CommentCount ?? 0) - toDelete.Count);
            }

            await _uow.SaveChangesAsync();
        }

        private static void NormalizePaging(ref int skip, ref int take)
        {
            if (skip < 0)
                skip = 0;

            if (take <= 0)
                take = 20;

            if (take > 50)
                take = 50;
        }

        private static string NormalizeContent(string? content, string fieldName)
        {
            if (string.IsNullOrWhiteSpace(content))
                throw new Exception($"{fieldName} cannot be empty");

            var normalized = content.Trim();

            if (normalized.Length > MaxContentLength)
                throw new Exception($"{fieldName} is too long");

            return normalized;
        }

        private static void EnsurePostCanInteract(Post? post, int userId)
        {
            if (post == null)
                throw new Exception("Post not found");

            var canAccess =
                post.AccountId == userId ||
                (post.Status == PostStatus.Published &&
                 post.Visibility == PostVisibility.Visible);

            if (!canAccess)
                throw new UnauthorizedAccessException("You cannot access this post.");
        }

        private CommentDto MapComment(Comment comment, bool isLiked, int replyCount)
        {
            return new CommentDto
            {
                CommentId = comment.CommentId,
                PostId = comment.PostId,
                AccountId = comment.AccountId,
                UserName = comment.Account?.UserName ?? "Unknown",
                AvatarUrl = comment.Account?.Avatars?
                    .OrderByDescending(a => a.CreatedAt)
                    .Select(a => a.ImageUrl)
                    .FirstOrDefault(),
                Content = comment.Content,
                LikeCount = comment.LikeCount,
                IsLiked = isLiked,
                CreatedAt = comment.CreatedAt,
                ParentCommentId = comment.ParentCommentId,
                ReplyCount = replyCount,
                HasReplies = replyCount > 0,
                Replies = new List<CommentReplyDto>()
            };
        }

        private CommentReplyDto MapReply(Comment reply, bool isLiked)
        {
            return new CommentReplyDto
            {
                CommentId = reply.CommentId,
                AccountId = reply.AccountId,
                UserName = reply.Account?.UserName ?? "Unknown",
                AvatarUrl = reply.Account?.Avatars?
                    .OrderByDescending(a => a.CreatedAt)
                    .Select(a => a.ImageUrl)
                    .FirstOrDefault(),
                Content = reply.Content,
                LikeCount = reply.LikeCount,
                IsLiked = isLiked,
                CreatedAt = reply.CreatedAt,
                ParentCommentId = reply.ParentCommentId ?? 0
            };

        }
    }
}