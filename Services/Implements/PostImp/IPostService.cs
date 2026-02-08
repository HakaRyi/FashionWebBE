using Repositories.Entities;
using Services.Request.PostReq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Services.Implements.PostImp
{
    public interface IPostService
    {
        public Task<Post> CreatePostAsync(CreatePostRequest request);
    }
}
