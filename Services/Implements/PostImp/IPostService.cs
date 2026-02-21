using Repositories.Entities;
using Services.Request.PostReq;
using Services.Response.PostResp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Services.Implements.PostImp
{
    public interface IPostService
    {
        public Task<Post> CreatePostAsync(int accountId, CreatePostRequest request);
        public Task<List<PostResponse>> GetAllPostAsync();
        public Task<PostResponse> GetPostByIdAsync(int postId);
        public Task<string> AdminCheckTheStatusPost(CheckPostRequest request, int id);

        public Task UpdatePostAsync(int postid, UpdatePostRequest post);
        public Task DeletePostAsync(int postId);
    }
}
