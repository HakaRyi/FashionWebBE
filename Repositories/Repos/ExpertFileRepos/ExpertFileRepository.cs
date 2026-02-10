using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Repositories.Data;
using Repositories.Entities;

namespace Repositories.Repos.ExpertFileRepos
{
    public class ExpertFileRepository : IExpertFileRepository
    {
        private readonly FashionDbContext _db;
        public ExpertFileRepository(FashionDbContext db)
        {
            _db = db;
        }

        public async Task<ExpertFile> GetById(int id)
        {
            return await _db.ExpertFiles
                .FindAsync(id);
        }
    }
}
