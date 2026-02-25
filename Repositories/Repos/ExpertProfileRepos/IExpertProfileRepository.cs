using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Repositories.Entities;

namespace Repositories.Repos.ExpertProfileRepos
{
    public interface IExpertProfileRepository
    {
        Task<ExpertProfile> GetById(int id);
    }
}
