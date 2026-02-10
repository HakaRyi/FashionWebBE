using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Repositories.Entities;
using Repositories.Repos.AccountRepos;
using Repositories.Repos.ExpertFileRepos;
using Repositories.Repos.ExpertProfileRepos;
using Services.Request.AccountReq;
using Services.Response.AccountRep;

namespace Services.Implements.AccountService
{
    public class AccountService : IAccountService
    {
        private readonly IAccountRepository accountRepository;
        private readonly IExpertProfileRepository expertProfileRepository;
        public AccountService(IAccountRepository accountRepository, IExpertProfileRepository expertProfileRepository)
        {
            this.accountRepository = accountRepository;
            this.expertProfileRepository = expertProfileRepository;
        }

        public async Task<int> CountAccount()
        {
            var accounts = await accountRepository.GetAllAccounts();
            return accounts.Count;
        }

        public async Task<int> CountExpert()
        {
            var experts = await accountRepository.GetFashionExperts();
            return experts.Count;
        }

        public async Task<AccountResponse?> GetAccountById(int accountId)
        {
            var account = await accountRepository.GetAccountById(accountId);
            if (account == null)
            {
                return new AccountResponse();
            }
            var accountResponse = new AccountResponse
            {
                Id = account.AccountId,
                Username = account.Username,
                Email = account.Email,
                Avatar = account.Avatar,
                Role = account.Role.RoleName,
                CreatedAt = account.CreatedAt,
                Status = account.Status
            };
            return accountResponse;

        }

        public async Task<List<FashionExpertResponse>> GetFashionExpert()
        {
            var experts = await accountRepository.GetFashionExperts();
            if (experts == null || experts.Count == 0)
            {
                return new List<FashionExpertResponse>();
            }
            var expertResponses = experts.Select(e => new FashionExpertResponse
            {
                Avatar = e.Avatar,
                AccountId = e.AccountId,
                ExpertProfileId = e.ExpertProfile.ExpertProfileId,
                FullName = e.Username,
                Verified = e.ExpertProfile.Verified,
                ExpertiseField = e.ExpertProfile.ExpertiseField,
                Rating = e.ExpertProfile.ExpertFile.RatingAvg
            }).ToList();
            return expertResponses;

        }

   

        public async Task<FashionExpertDetail> GetFashionExpertDetail(int id)
        {
            var account = await accountRepository.GetAccountById(id);
            var expertProfile = await expertProfileRepository.GetById(id);
            if (account == null || expertProfile == null)
            {
                return new FashionExpertDetail();
            }
            var fashionExpertDetail = new FashionExpertDetail
            {
                AccountId = account.AccountId,
                ExpertProfileId = expertProfile.ExpertProfileId,
                Username = account.Username,
                Email = account.Email,
                PasswordHash = account.PasswordHash,
                RoleId = account.RoleId,
                CreatedAt = account.CreatedAt,
                Status = account.Status,
                Avatar = account.Avatar,
                YearsOfExperience = expertProfile.YearsOfExperience,
                Bio = expertProfile.Bio,
                Verified = expertProfile.Verified,
                CreatedAtProfile = expertProfile.CreatedAt,
                UpdatedAtProfile = expertProfile.UpdatedAt
            };
            return fashionExpertDetail;
        }

        public async Task<List<AccountResponse>> GetListAccount()
        {
            var accounts = await accountRepository.GetAllAccounts();
            if (accounts == null || accounts.Count == 0)
            {
                return new List<AccountResponse>();
            }
            var accountResponses = accounts.Select(a => new AccountResponse
            {
                Id = a.AccountId,
                Username = a.Username,
                Email = a.Email,
                Avatar = a.Avatar,
                Role = a.Role.RoleName ,
                CreatedAt = a.CreatedAt,
                Status = a.Status
            }).ToList();
            return accountResponses;

        }

        public async Task<string> updateAccountRequest(int accountId,UpdateAccountRequest request)
        {
            var account = await accountRepository.GetAccountById(accountId);
            if (account == null)
            {
                return null;
            }
            account.Username = request.Username;
            account.Avatar = request.Avatar;
            account.Status = request.Status;
            account.Email = request.Email;
            account.RoleId = request.Role;
            await accountRepository.UpdateAccount(account);
            return "Update success";
        }

    }
}
