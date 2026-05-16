using Microsoft.AspNetCore.Http;

namespace Domain.Contracts.Social.Post
{
    public class UpdatePostDto
    {
        public string? Title { get; set; }

        public string? Content { get; set; }

        public List<IFormFile>? Images { get; set; }
    }
}