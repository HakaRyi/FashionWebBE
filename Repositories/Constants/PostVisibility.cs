namespace Repositories.Constants
{
    public static class PostVisibility
    {
        // user muốn ẩn bài của họ
        public const string Visible = "Visible";
        public const string Hidden = "Hidden";

        public static readonly List<string> AllVisibilities = new()
        {
            Visible,
            Hidden
        };

        public static bool IsValid(string visibility)
        {
            return AllVisibilities.Contains(visibility);
        }
    }
}