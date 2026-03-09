using Microsoft.EntityFrameworkCore;
using Repositories.Data;
using Repositories.Entities;

namespace Repositories.Repos.ImageRepos
{
    public class ImageRepository : IImageRepository
    {
        private readonly FashionDbContext _db;

        public ImageRepository(FashionDbContext db)
        {
            _db = db;
        }

        public async Task<Image?> GetByIdAsync(int id)
        {
            return await _db.Images
                .FirstOrDefaultAsync(i => i.ImageId == id);
        }

        public async Task<Image?> GetNewestAvatarAsync(int userId)
        {
            return await _db.Images
                .Where(i => i.AccountAvatarId == userId)
                .OrderByDescending(i => i.CreatedAt)
                .FirstOrDefaultAsync();
        }

        public async Task<List<Image>> GetAllMyAvatarAsync(int userId)
        {
            return await _db.Images
                .Where(i => i.AccountAvatarId == userId)
                .OrderByDescending(i => i.CreatedAt)
                .ToListAsync();
        }

        public async Task<List<Image>> GetAllAvatarAsync()
        {
            return await _db.Images
                .Where(i => i.OwnerType == "Avatar")
                .ToListAsync();
        }

        public async Task AddAsync(Image image)
        {
            await _db.Images.AddAsync(image);
        }

        public async Task AddRangeAsync(List<Image> images)
        {
            await _db.Images.AddRangeAsync(images);
        }

        public void Delete(Image image)
        {
            _db.Images.Remove(image);
        }

        public void DeleteRange(List<Image> images)
        {
            _db.Images.RemoveRange(images);
        }
    }
}