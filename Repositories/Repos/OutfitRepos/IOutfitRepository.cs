using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Repositories.Repos.OutfitRepos
{
    public interface IOutfitRepository
    {
        Task AddAsync(Entities.Outfit outfit);
    }
}
