using Domain.Contracts.Social.Post;
using Domain.Entities;

namespace Application.Utils.Mapper
{
    public static class PostMapper
    {
        public static PostFeedDto ToFeedDto(
            this Post post,
            int currentUserId,
            bool isLiked)
        {
            return new PostFeedDto
            {
                PostId = post.PostId,
                AccountId = post.AccountId,

                UserName = post.Account.UserName,

                AvatarUrl = post.Account.Avatars
                    .OrderByDescending(a => a.CreatedAt)
                    .Select(a => a.ImageUrl)
                    .FirstOrDefault(),

                Title = post.Title,
                Content = post.Content,

                Images = post.Images
                    .OrderBy(i => i.CreatedAt)
                    .Select(i => i.ImageUrl)
                    .ToList(),

                LikeCount = post.LikeCount ?? 0,
                CommentCount = post.CommentCount ?? 0,
                ShareCount = post.ShareCount ?? 0,

                IsLiked = isLiked,

                CreatedAt = (DateTime)post.CreatedAt
            };
        }


        public static PostDetailDto ToDetailDto(
            this Post post,
            bool isLiked)
        {
            return new PostDetailDto
            {
                PostId = post.PostId,
                AccountId = post.AccountId,

                UserName = post.Account.UserName,

                AvatarUrl = post.Account.Avatars
                    .OrderByDescending(a => a.CreatedAt)
                    .Select(a => a.ImageUrl)
                    .FirstOrDefault(),

                Title = post.Title,
                Content = post.Content,

                Images = post.Images
                    .OrderBy(i => i.CreatedAt)
                    .Select(i => i.ImageUrl)
                    .ToList(),

                LikeCount = post.LikeCount ?? 0,
                CommentCount = post.CommentCount ?? 0,
                ShareCount = post.ShareCount ?? 0,

                IsLiked = isLiked,

                CreatedAt = (DateTime)post.CreatedAt
            };
        }
    }
}