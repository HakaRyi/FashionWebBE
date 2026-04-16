namespace Application.Interfaces
{
    public interface ICurrentUserService
    {
        int? GetUserId();
        int GetRequiredUserId();
        string? GetEmail();
        string? GetUserName();
        bool IsAuthenticated();
    }
}