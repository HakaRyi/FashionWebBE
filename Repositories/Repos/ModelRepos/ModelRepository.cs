using Microsoft.EntityFrameworkCore;
using Repositories.Data;
using Repositories.Entities;

namespace Repositories.Repos.ModelRepos
{
    public class ModelRepository : IModelRepository
    {
        private readonly FashionDbContext _context;

        public ModelRepository(FashionDbContext context)
        {
            _context = context;
        }

        public async Task CreateModelAsync(Model model)
        {
            await _context.Models.AddAsync(model);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteModelAsync(int modelId)
        {
            try
            {
                var existModel = await _context.Models.FindAsync(modelId);
                if (existModel != null)
                {
                    _context.Models.Remove(existModel);
                }
                else
                {
                    throw new Exception($"Model with ID {modelId} not found.");

                }
            }
            catch
            {
                throw new Exception($"An error occurred while trying to delete the model with ID {modelId}.");
            }
        }

        public async Task<Model?> GetModelByIdAsync(int modelId)
        {
            return await _context.Models.FindAsync(modelId);
        }

        public async Task<IEnumerable<Model>> GetModelsByAccountIdAsync(int accountId)
        {
            var result = await _context.Models.Where(m => m.AccountId == accountId).ToListAsync();
            return result;
        }

        public async Task UpdateModelAsync(Model model)
        {
            _context.Models.Update(model);
            await _context.SaveChangesAsync();
        }
    }
}
