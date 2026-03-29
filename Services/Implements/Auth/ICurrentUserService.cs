namespace Services.Implements.Auth
{
    public interface ICurrentUserService
    {
        int? GetUserId();
        int GetRequiredUserId();
        string? GetEmail();
        bool IsAuthenticated();
    }
}
