using System.Security.Claims;

namespace Infrastructure.Interfaces;

public static class AuthenticationConstants
{
    public static class Claims
    {
        public const string ForFeature = "Feature";
        public const string ForId = "sub";
        public const string ForRole = ClaimTypes.Role;
        public const string PlatformPrefix = "Platform";
        public const string TenantPrefix = "Tenant";
    }

    public static class Authorization
    {
        public const string HMACPolicyName = "HMAC";
        public const string RolesAndFeaturesPolicyNameForNone = $"{RolesAndFeaturesPolicyNamePrefix}None";
        public const string RolesAndFeaturesPolicyNamePrefix = "RolesAndFeatures_";
        public const string TokenPolicyName = "Token";
    }

    public static class Cookies
    {
        public const string RefreshToken = "auth-reftok";
        public const string Token = "auth-tok";
    }

    public static class Providers
    {
        public const string Credentials = "credentials";
        public const string SingleSignOn = "sso";
    }

    public static class Tokens
    {
        public static readonly TimeSpan DefaultAccessTokenExpiry = TimeSpan.FromMinutes(15);
        public static readonly TimeSpan DefaultRefreshTokenExpiry = TimeSpan.FromDays(14);
    }
}