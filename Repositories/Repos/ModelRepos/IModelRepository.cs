using Repositories.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
namespace Repositories.Repos.ModelRepos
{
    public interface IModelRepository
    {
        Task CreateModelAsync(Model model);
        Task DeleteModelAsync(int modelId);

        Task<IEnumerable<Model>> GetModelsByAccountIdAsync(int accountId);
    }
}
