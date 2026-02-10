using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Repositories.Entities;

namespace Repositories.Repos.ExpertFileRepos
{
    public interface IExpertFileRepository
    {
        Task<ExpertFile> GetById(int id);
    }
}
