using Application.Request.AccountReq;
using Application.Response.AccountRep;
using Application.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Interfaces
{
    public interface IUserProfileService
    {
        Task<UserProfileResponseDto?> GetUserProfileAsync();

        Task<AuthResponse> CompleteOnboardingAsync(OnboardingRequestDto request);

        Task<bool> UpdateUserProfileAsync(UpdateUserProfileDto request);
    }
}
