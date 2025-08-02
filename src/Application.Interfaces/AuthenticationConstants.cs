using System.Security.Claims;
using Domain.Interfaces;

namespace Application.Interfaces;

public static class AuthenticationConstants
{
    public static class Claims
    {
        public const string ForAddress = OAuth2Constants.StandardClaims.Address;
        public const string ForAtHash = OpenIdConnectConstants.StandardClaims.AtHash;
        public const string ForAuthTime = OpenIdConnectConstants.StandardClaims.AuthTime;
        public const string ForCHash = OpenIdConnectConstants.StandardClaims.CHash;
        public const string ForClientId = OpenIdConnectConstants.StandardClaims.ClientId;
        public const string ForEmail = OAuth2Constants.StandardClaims.Email;
        public const string ForEmailVerified = OAuth2Constants.StandardClaims.EmailVerified;
        public const string ForFamilyName = OAuth2Constants.StandardClaims.FamilyName;
        public const string ForFeature = "Feature";
        public const string ForFullName = OAuth2Constants.StandardClaims.Name;
        public const string ForGivenName = OAuth2Constants.StandardClaims.GivenName;
        public const string ForId = "sub";
        public const string ForIssuedAt = "iat";
        public const string ForLocale = OAuth2Constants.StandardClaims.Locale;
        public const string ForNickName = OAuth2Constants.StandardClaims.Nickname;
        public const string ForNonce = OpenIdConnectConstants.StandardClaims.Nonce;
        public const string ForPhoneNumber = OAuth2Constants.StandardClaims.PhoneNumber;
        public const string ForPhoneNumberVerified = OAuth2Constants.StandardClaims.PhoneNumberVerified;
        public const string ForPicture = OAuth2Constants.StandardClaims.Picture;
        public const string ForRole = ClaimTypes.Role;
        public const string ForScope = OpenIdConnectConstants.StandardClaims.Scope;
        public const string ForTimezone = OAuth2Constants.StandardClaims.Zoneinfo;
        public const string PlatformPrefix = "Platform";
        public const string TenantPrefix = "Tenant";
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