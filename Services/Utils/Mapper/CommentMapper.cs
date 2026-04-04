using Domain.Dto.Social.Comment;
using Domain.Entities;

namespace Application.Utils.Mapper
{
    public static class CommentMapper
    {
        public static CommentDto ToDto(
            this Comment comment,
            bool isLiked,
            int likeCount)
        {
            return new CommentDto
            {
                CommentId = comment.CommentId,
                PostId = comment.PostId,
                AccountId = comment.AccountId,

                UserName = comment.Account.UserName,

                AvatarUrl = comment.Account.Avatars
                    .OrderByDescending(a => a.CreatedAt)
                    .Select(a => a.ImageUrl)
                    .FirstOrDefault(),

                Content = comment.Content,

                LikeCount = likeCount,
                IsLiked = isLiked,

                CreatedAt = comment.CreatedAt,
                ParentCommentId = comment.ParentCommentId
            };
        }


        public static CommentReplyDto ToReplyDto(
            this Comment comment,
            bool isLiked,
            int likeCount)
        {
            return new CommentReplyDto
            {
                CommentId = comment.CommentId,
                AccountId = comment.AccountId,

                UserName = comment.Account.UserName,

                AvatarUrl = comment.Account.Avatars
                    .OrderByDescending(a => a.CreatedAt)
                    .Select(a => a.ImageUrl)
                    .FirstOrDefault(),

                Content = comment.Content,

                LikeCount = likeCount,
                IsLiked = isLiked,

                CreatedAt = comment.CreatedAt,
                ParentCommentId = comment.ParentCommentId ?? 0
            };
        }
    }
}