namespace Repositories.Dto.Social.Comment
{
    public class PagedCommentsResponseDto
    {
        public List<CommentDto> Items { get; set; } = new();

        public int TotalCount { get; set; }

        public int Skip { get; set; }

        public int Take { get; set; }

        public bool HasMore { get; set; }
    }
}