using Application.Request.AccountReq;
using Application.Response.AccountRep;

namespace Application.Interfaces
{
    public interface IAccountService
    {
        Task<List<AccountResponse>> GetListAccount();
        Task<List<FashionExpertResponse>> GetFashionExpert();
        Task<FashionExpertDetail> GetFashionExpertDetail(int id);
        Task<AccountResponse?> GetAccountById(int accountId);
        Task<AccountResponse?> GetAccountByMe();
        Task<string> updateAccountRequest(int accountId, UpdateAccountRequest request);
        Task<int> updateProfile(UpdateProfileRequest request);
        Task<int> CountAccount();
        Task<int> CountExpert();
        Task<bool> CompleteOnboardingAsync(int accountId, OnboardingRequest request);
    }
}
