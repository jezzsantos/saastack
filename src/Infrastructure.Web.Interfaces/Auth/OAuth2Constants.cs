namespace Infrastructure.Web.Interfaces.Auth;

public static class OAuth2Constants
{
    public const string BearerTokenPrefix = "Bearer";

    public static class GrantTypes
    {
        public const string AuthorizationCodeFlow = "authorization_code";
        public const string PasswordFlow = "password";
        public const string RefreshFlow = "refresh_token";
    }
}