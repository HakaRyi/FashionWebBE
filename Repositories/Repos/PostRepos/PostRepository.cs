using Repositories.Data;
using Repositories.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Repositories.Repos.PostRepos
{
    public class PostRepository : IPostRepository
    {
        protected readonly FashionDbContext _context;
        public PostRepository(FashionDbContext context)
        {
            _context = context;
        }
        public async Task AddPostAsync(Post post)
        {
            await _context.Posts.AddAsync(post);
            await _context.SaveChangesAsync();
        }

        public async Task<Post?> GetPostByIdAsync(int postId)
        {
            return await _context.Posts.FindAsync(postId);
        }

        public async Task UpdatePostAsync(Post post)
        {
            post.UpdatedAt = DateTime.UtcNow;

            _context.Posts.Update(post);
            await _context.SaveChangesAsync();
        }
    }
}
