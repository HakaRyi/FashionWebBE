using Repositories.Entities;
using Repositories.Repos.SocialRepos;
using Services.Request.CommentReq;
using Services.Request.ReactionReq;

namespace Services.Implements.SocialImp
{
    public class SocialService : ISocialService
    {
        private readonly ISocialRepository _socialRepository;
        public SocialService(ISocialRepository socialRepository)
        {
            _socialRepository = socialRepository;
        }

        public Task<bool> CheckIsLikedByUser(int accId, int postId)
        {
            return _socialRepository.CheckIsLikedByUser(accId, postId);
        }

        public async Task<int> CreateComment(CommentRequest request, int accId, int postId)
        {
            var comment = new Comment
            {
                AccountId = accId,
                PostId = postId,
                Content = request.Content,
                CreatedAt = DateTime.UtcNow
            };
            return await _socialRepository.Comment(comment);
        }

        public async Task<int> CreateReaction(int accId, int postId)
        {
            var reaction = new Reaction
            {
                AccountId = accId,
                PostId = postId,
                ReactionType = "Like",
                CreatedAt = DateTime.UtcNow
            };
            return await _socialRepository.CreateReact(reaction);
        }

        public async Task<bool> DeleteComment(int commentId)
        {
            var comment = await _socialRepository.GetCommentById(commentId);
            return await _socialRepository.Delete(comment);
        }

        public async Task<List<Comment>> GetAllCommentByPostId(int postId)
        {
            return await _socialRepository.GetAllCommentByPostId(postId);
        }

        public async Task<List<Reaction>> GetAllReactionByPostId(int postId)
        {
            return await _socialRepository.GetAllReactionByPostId(postId);
        }

        public async Task<Reaction> GetById(int reactionId)
        {
            return await _socialRepository.GetById(reactionId);
        }

        public async Task<Comment> GetCommentById(int commentId)
        {
            return await _socialRepository.GetCommentById(commentId);
        }

        public async Task<int> GetCommentCountByPostId(int postId)
        {
            return (await _socialRepository.GetAllCommentByPostId(postId)).Count;
        }

        public async Task<int> GetReactionCountByPostId(int postId)
        {
            return (await _socialRepository.GetAllReactionByPostId(postId)).Count;
        }

        public async Task<bool> RemoveReaction(int reactId)
        {
            var react = await _socialRepository.GetById(reactId);
            if (react == null)
            {
                throw new Exception("Reaction not found.");
            }
            return await _socialRepository.RemoveReaction(reactId);
        }

        public async Task<int> UpdateComment(int commentId, int accId, CommentRequest request)
        {
            var comment = await _socialRepository.GetCommentById(commentId);
            if (comment == null || comment.AccountId != accId)
            {
                throw new Exception("Comment not found or does not belong to the user.");
            }
            comment.Content = request.Content;
            return await _socialRepository.UpdateComment(comment);
        }

        public async Task<int> UpdateReaction(int accId, int postId, UpdateReactionRequest request)
        {
            var reaction = await _socialRepository.GetReactByAccIdAndPostId(accId, postId);
            if (reaction == null || reaction.AccountId != accId || reaction.PostId != postId)
            {
                throw new Exception("Reaction not found or does not belong to the user/post.");
            }
            reaction.ReactionType = request.ReactionType;
            return await _socialRepository.UpdateReact(reaction);
        }
    }
}
