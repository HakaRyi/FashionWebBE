namespace Repositories.Constants
{
    public static class PostStatus
    {
        public const string Draft = "Draft";
        public const string Verifying = "Verifying";
        public const string Published = "Published";
        public const string AIRejected = "AIRejected";
        public const string BlockedByAdmin = "BlockedByAdmin";

        public static readonly List<string> AllStatuses = new()
        {
            Draft,
            Verifying,
            Published,
            AIRejected,
            BlockedByAdmin
        };

        public static bool IsValid(string status)
        {
            return AllStatuses.Contains(status);
        }
    }
}


// Owner dc sửa bài với:
//Draft -> được sửa
//Verifying -> không
//Published -> được
//AIRejected -> được sửa rồi submit lại
//BlockedByAdmin -> không

//Owner dc hide/unhide khi:
//Published -> được
//AIRejected -> không cần cho hide/unhide vì nó vốn không public
//BlockedByAdmin -> không
//Verifying -> không cần
//Draft -> tuỳ, thường không cần