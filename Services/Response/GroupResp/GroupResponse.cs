using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Repositories.Repos.AccountRepos;
using Repositories.Repos.ChatRepos;
using Repositories.Repos.GroupRepos;
using Repositories.UnitOfWork;
using Services.Implements.Auth;

namespace Services.Response.GroupResp
{
    public class GroupResponse 
    {
        public int GroupId { get; set; }

        public string? Name { get; set; }
        public string? Avatar { get; set; }

        public bool? IsGroup { get; set; } = true; // true cho group, false cho 1-1 chat

        public string? CreateBy { get; set; }

        public DateTime? CreatedAt { get; set; }

    }
}
