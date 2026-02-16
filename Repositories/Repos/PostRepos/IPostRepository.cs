using Repositories.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Repositories.Repos.PostRepos
{
    public interface IPostRepository
    {
        Task AddPostAsync(Post post);
        Task<Post?> GetPostByIdAsync(int postId);
        Task UpdatePostAsync(Post post);
        Task<List<Post>> GetAllPostAsync(); 

        Task DeletePostAsync(int postId);
    }
}
