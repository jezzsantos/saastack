namespace IdentityDomain;

public static class OpenIdConnectConstants
{
    public static class GrantTypes
    {
        public const string AuthorizationCode = "authorization_code";
        public const string ClientCredentials = "client_credentials";
        public const string Implicit = "implicit";
        public const string Password = "password";
        public const string RefreshToken = "refresh_token";
    }

    public static class Scopes
    {
        public const string Address = "address";
        public const string Email = "email";
        public const string OfflineAccess = "offline_access";
        public const string OpenId = "openid";
        public const string Phone = "phone";
        public const string Profile = "profile";
        public static readonly IReadOnlyList<string> AllScopes =
            [OpenId, Profile, Email, Address, Phone, OfflineAccess];
    }

    public static class ResponseTypes
    {
        public const string Code = "code";
        public const string IdToken = "id_token";
        public const string Token = "token";
    }

    public static class TokenTypes
    {
        public const string Bearer = "Bearer";
    }

    public static class ClientAuthenticationMethods
    {
        public const string ClientSecretBasic = "client_secret_basic";
        public const string ClientSecretJwt = "client_secret_jwt";
        public const string ClientSecretPost = "client_secret_post";
        public const string None = "none";
        public const string PrivateKeyJwt = "private_key_jwt";
    }

    public static class CodeChallengeMethods
    {
        public const string Plain = "plain";
        public const string S256 = "S256";
        public static readonly IReadOnlyList<string> AllMethods = [Plain, S256];
    }

    public static class SubjectTypes
    {
        public const string Pairwise = "pairwise";
        public const string Public = "public";
    }

    public static class SigningAlgorithms
    {
        public const string Es256 = "ES256";
        public const string Es384 = "ES384";
        public const string Es512 = "ES512";
        public const string Hs256 = "HS256";
        public const string Hs384 = "HS384";
        public const string Hs512 = "HS512";
        public const string Rs256 = "RS256";
        public const string Rs384 = "RS384";
        public const string Rs512 = "RS512";
    }

    public static class StandardClaims
    {
        public const string Address = "address";
        public const string Birthdate = "birthdate";
        public const string Email = "email";
        public const string EmailVerified = "email_verified";
        public const string FamilyName = "family_name";
        public const string Gender = "gender";
        public const string GivenName = "given_name";
        public const string Locale = "locale";
        public const string MiddleName = "middle_name";
        public const string Name = "name";
        public const string Nickname = "nickname";
        public const string PhoneNumber = "phone_number";
        public const string PhoneNumberVerified = "phone_number_verified";
        public const string Picture = "picture";
        public const string PreferredUsername = "preferred_username";
        public const string Profile = "profile";
        public const string Subject = "sub";
        public const string UpdatedAt = "updated_at";
        public const string Website = "website";
        public const string Zoneinfo = "zoneinfo";
    }

    public static class Endpoints
    {
        public const string Authorization = "/oauth2/authorize";
        public const string Discovery = "/.well-known/openid_configuration";
        public const string EndSession = "/oauth2/endsession";
        public const string Introspection = "/oauth2/introspect";
        public const string Jwks = "/.well-known/jwks.json";
        public const string Revocation = "/oauth2/revoke";
        public const string Token = "/oauth2/token";
        public const string UserInfo = "/oauth2/userinfo";
    }

    public static class ErrorCodes
    {
        public const string AccessDenied = "access_denied";
        public const string InvalidClient = "invalid_client";
        public const string InvalidGrant = "invalid_grant";
        public const string InvalidRequest = "invalid_request";
        public const string InvalidScope = "invalid_scope";
        public const string ServerError = "server_error";
        public const string TemporarilyUnavailable = "temporarily_unavailable";
        public const string UnauthorizedClient = "unauthorized_client";
        public const string UnsupportedGrantType = "unsupported_grant_type";
        public const string UnsupportedResponseType = "unsupported_response_type";
    }
}