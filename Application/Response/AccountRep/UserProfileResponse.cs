using Domain.Constants;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Response.AccountRep
{
    public class UserProfileResponse
    {
    }

    public class UserProfileResponseDto
    {
        public int Id { get; set; }
        public string? Email { get; set; }
        public string? UserName { get; set; }
        public GenderType? Gender { get; set; }
        public DateTime? DateOfBirth { get; set; }
        public bool HasCompletedOnboarding { get; set; }

        // Physical Metrics
        public double? Height { get; set; }
        public double? Weight { get; set; }
        public double? Waist { get; set; }
        public double? Hip { get; set; }
        public double? Bust { get; set; }
        public string? BodyShape { get; set; }
        public string? SkinTone { get; set; }

        // Preferences
        public List<string> FavoriteStyles { get; set; } = new();
        public List<string> FavoriteColors { get; set; } = new();
        public List<string> FavoriteMaterials { get; set; } = new();
        public List<string> FavoriteBrands { get; set; } = new();
    }
}
