namespace Domain.Contracts.Social
{
    public class HashtagSuggestionDto
    {
        public int HashtagId { get; set; }
        public string Name { get; set; } = string.Empty;
        public int UsageCount { get; set; }
        public bool IsTrending { get; set; }
    }
}