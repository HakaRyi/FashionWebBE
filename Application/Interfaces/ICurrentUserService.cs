namespace Application.Interfaces
{
    public interface ICurrentUserService
    {
        int? GetUserId();
        int GetRequiredUserId();
        string? GetEmail();
        bool IsAuthenticated();
    }
}