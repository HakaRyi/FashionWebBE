using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Services.Request.SearchReq
{
    public class UserSuggestionDto
    {
        public int AccountId { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string Username { get; set; } = string.Empty;
        public string AvatarUrl { get; set; } = string.Empty;
        public int FollowerCount { get; set; }
        public bool IsFollowing { get; set; }
    }

    public class SearchHistoryDto
    {
        public int Id { get; set; }
        public string Keyword { get; set; } = string.Empty;
    }

    public class AddSearchHistoryRequest
    {
        public string Keyword { get; set; } = string.Empty;
    }
}
