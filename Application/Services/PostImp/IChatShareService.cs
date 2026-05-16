using Application.Request.PostReq;

namespace Application.Services.PostImp
{
    public interface IChatShareService
    {
        Task SharePostAsync(SharePostToChatRequest request);
    }
}