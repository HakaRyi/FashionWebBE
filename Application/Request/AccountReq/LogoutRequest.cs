namespace Application.Request.AccountReq
{
    public class LogoutRequest
    {
        public string RefreshToken { get; set; } = string.Empty;
    }
}