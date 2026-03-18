using Microsoft.AspNetCore.Http;

namespace Repositories.Dto.Social.Post
{
    public class CreatePostDto
    {
        public string? Title { get; set; }
        public string? Content { get; set; }
        public int? EventId { get; set; }
        public List<IFormFile>? Images { get; set; }
    }
}