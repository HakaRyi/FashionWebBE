using Microsoft.EntityFrameworkCore;
using Repositories.Data;
using Repositories.Entities;

namespace Repositories.Repos.ImageRepos
{
    public class ImageReository : IImageRepository
    {
        private readonly FashionDbContext _db;
        public ImageReository(FashionDbContext db)
        {
            _db = db;
        }

        public async Task<List<Image>> GetAllMyAvatar(int userId)
        {
            return await _db.Images
                .Where(i => i.AccountAvatarId == userId)
                .ToListAsync();
        }

        public async Task<Image> GetNewestAvatar(int userId)
        {
            return await _db.Images
                .OrderByDescending(i => i.CreatedAt)
                .FirstOrDefaultAsync(i => i.AccountAvatarId == userId);
        }
        public async Task<List<Image>> GetAllAvatar()
        {
            return await _db.Images
                .ToListAsync();
        }

        public async Task<int> CreateImage(Image image)
        {
            _db.Images.Add(image);
            return await _db.SaveChangesAsync();
        }

        public async Task<bool> DeteleImage(Image image)
        {
            _db.Images.Remove(image);
            var result = await _db.SaveChangesAsync();
            if (result > 0)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        public async Task<Image> GetById(int id)
        {
            return await _db.Images.FirstOrDefaultAsync(i => i.ImageId == id);
        }
    }
}
