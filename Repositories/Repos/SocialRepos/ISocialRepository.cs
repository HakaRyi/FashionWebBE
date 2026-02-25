using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Repositories.Entities;

namespace Repositories.Repos.SocialRepos
{
    public interface ISocialRepository
    {
        Task<int> CreateReact(Reaction reaction);
        Task<bool> CheckIsLikedByUser(int userId, int postId);
        Task<bool> CheckIsSharedByUser(int userId ,int postId);
        Task<List<Reaction>> GetAllReactionByPostId(int postId);
        Task<Reaction> GetById(int reactId);
        Task<Reaction> GetReactByAccIdAndPostId(int accId,int postId);
        Task<int> UpdateReact(Reaction reaction);
         Task<int> Comment(Comment comment);
        Task<int> UpdateComment(Comment comment);
        Task<bool> Delete(Comment comment);
        Task<Comment> GetCommentById(int id);
        Task<List<Comment>> GetAllCommentByPostId(int postId);
        Task<bool> RemoveReaction(int reactId);
    }
}
