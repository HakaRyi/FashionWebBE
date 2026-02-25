using Repositories.Entities;
using Services.Request.CommentReq;
using Services.Request.ReactionReq;

namespace Services.Implements.SocialImp
{
    public interface ISocialService
    {
        Task<int> CreateReaction(int accId, int postId);
        Task<Reaction> GetById(int reactionId);
        Task<int> UpdateReaction(int accId, int postId, UpdateReactionRequest request);
        Task<List<Reaction>> GetAllReactionByPostId(int postId);
        Task<int> GetReactionCountByPostId(int postId);
        Task<int> GetCommentCountByPostId(int postId);
        Task<bool> RemoveReaction(int reactId);
        Task<bool> CheckIsLikedByUser(int accId, int postId);
        Task<int> CreateComment(CommentRequest request, int accId, int postId);
        Task<int> UpdateComment(int commentId, int accId, CommentRequest request);
        Task<bool> DeleteComment(int commentId);
        Task<Comment> GetCommentById(int commentId);
        Task<List<Comment>> GetAllCommentByPostId(int postId);

    }
}
