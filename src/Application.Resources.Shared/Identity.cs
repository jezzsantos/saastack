namespace Application.Resources.Shared;

public class AuthenticateTokens
{
    public required string AccessToken { get; set; }

    public required DateTime ExpiresOn { get; set; }

    public required string RefreshToken { get; set; }
}