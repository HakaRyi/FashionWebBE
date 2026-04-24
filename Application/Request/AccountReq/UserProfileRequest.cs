using Domain.Constants;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Request.AccountReq
{
    public class UserProfileRequest
    {
    }

    public class UpdateUserProfileDto
    {
        public GenderType Gender { get; set; }
        public DateTime? DateOfBirth { get; set; }
        public double? Height { get; set; }
        public double? Weight { get; set; }
        public double? Waist { get; set; }
        public double? Hip { get; set; }
        public double? Bust { get; set; }
        public string? BodyShape { get; set; }
        public string? SkinTone { get; set; }
        public List<string>? FavoriteStyles { get; set; }
        public List<string>? FavoriteColors { get; set; }
    }

    public class OnboardingRequestDto
    {
        public GenderType Gender { get; set; }
        public double? Height { get; set; }
        public double? Weight { get; set; }
        public double? Waist { get; set; }
        public double? Hip { get; set; }
        public double? Bust { get; set; }
        public string? BodyShape { get; set; }
        public string? SkinTone { get; set; }
        public List<string>? FavoriteStyles { get; set; }
        public List<string>? FavoriteColors { get; set; }
    }
}
