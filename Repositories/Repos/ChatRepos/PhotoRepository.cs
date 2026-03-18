using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Repositories.Data;
using Repositories.Entities;

namespace Repositories.Repos.ChatRepos
{
    public class PhotoRepository : IPhotoRepository
    {
        private readonly FashionDbContext _context;
        public PhotoRepository(FashionDbContext context)
        {
            _context = context;
        }

        public async Task AddPhotoAsync(Photo photo)
        {
            _context.Photos.AddAsync(photo);
        }

        public async Task DeletePhotoAsync(Photo photo)
        {
            _context.Photos.Remove(photo);
        }

        public async Task<Photo> GetPhotoById(int photoId)
        {
            return await _context.Photos
                .Include(p => p.Message)
                .FirstOrDefaultAsync(p=>p.PhotoId == photoId );
        }

        public async Task<List<Photo>> GetPhotoFromMessageId(int msgId)
        {
            return await _context.Photos
                .Include(p => p.Message)
                .Where(p => p.MessageId == msgId)
                .ToListAsync();
        }
    }
}
