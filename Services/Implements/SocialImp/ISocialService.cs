using Repositories.Dto.Social.Comment;
using Repositories.Dto.Social.Post;

namespace Services.Implements.SocialImp
{
    public interface ISocialService
    {
        Task<PagedCommentsResponseDto> GetCommentsAsync(int userId, int postId, int skip, int take);

        Task<CommentRepliesResponseDto> GetRepliesAsync(int userId, int parentCommentId, int skip, int take);

        Task<CommentDto> CreateCommentAsync(int userId, int postId, CreateCommentRequestDto dto);

        Task<CreateReplyResultDto> ReplyCommentAsync(int userId, int parentCommentId, CreateReplyDto dto);

        Task UpdateCommentAsync(int commentId, int userId, CommentRequest request);

        Task DeleteCommentAsync(int commentId, int userId);

        Task<PostReactionResultDto> TogglePostReactionAsync(int userId, int postId);

        Task<CommentReactionResultDto> ToggleCommentReactionAsync(int userId, int commentId);
    }
}