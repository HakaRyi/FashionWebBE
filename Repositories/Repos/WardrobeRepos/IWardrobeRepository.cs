using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Repositories.Entities;

namespace Repositories.Repos.WardrobeRepos
{
    public interface IWardrobeRepository
    {
        Task<Wardrobe?> GetById(int accountId);
        Task<int> CreateWardrobe(Wardrobe wardrobe);
        Task<List<Wardrobe>> GetAll();
    }
}
