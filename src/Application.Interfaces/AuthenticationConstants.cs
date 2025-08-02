using System.Security.Claims;

namespace Application.Interfaces;

public static class AuthenticationConstants
{
    public static class Claims
    {
        public const string ForAtHash = "at_hash";
        public const string ForAuthTime = "auth_time";
        public const string ForCHash = "c_hash";
        public const string ForEmail = "email";
        public const string ForEmailVerified = "email_verified";
        public const string ForFamilyName = "family_name";
        public const string ForFeature = "Feature";
        public const string ForFullName = "name";
        public const string ForGivenName = "given_name";
        public const string ForId = "sub";
        public const string ForIssuedAt = "iat";
        public const string ForNonce = "nonce";
        public const string ForPhoneNumber = "phone_number";
        public const string ForPicture = "picture";
        public const string ForRole = ClaimTypes.Role;
        public const string ForTimezone = "zoneinfo";
        public const string PlatformPrefix = "Platform";
        public const string TenantPrefix = "Tenant";
        public const string ForNickName = "nickname";
    }

    public static class Authorization
    {
        public const string HMACPolicyName = "HMAC";
        public const string PrivateInterHostPolicyName = "PrivateInterHost";
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
    }

    public static class Tokens
    {
        public static readonly TimeSpan DefaultAccessTokenExpiry = TimeSpan.FromMinutes(15);
        public static readonly TimeSpan DefaultIdTokenExpiry = TimeSpan.FromMinutes(30);
        public static readonly TimeSpan DefaultRefreshTokenExpiry = TimeSpan.FromDays(14);
    }

    public static class ErrorCodes
    {
        public const string MfaRequired = "mfa_required";
    }
}