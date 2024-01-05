using System.Security.Claims;

namespace Infrastructure.Web.Hosting.Common.Auth;

public static class AuthenticationConstants
{
    public const string ClaimForFeatureLevel = "Feature";
    public const string ClaimForId = "sub";
    public const string ClaimForRole = ClaimTypes.Role;
    public const string HMACPolicyName = "HMAC";
    public const string TokenPolicyName = "Token";
    public static readonly TimeSpan DefaultAccessTokenExpiry = TimeSpan.FromMinutes(15);
}