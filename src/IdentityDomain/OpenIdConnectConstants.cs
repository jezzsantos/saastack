namespace IdentityDomain;

public static class OpenIdConnectConstants
{
    public static class Scopes
    {
        public const string OpenId = "openid";
        public static readonly IReadOnlyList<string> AllScopes =
        [
            OpenId, OAuth2Constants.Scopes.Profile, OAuth2Constants.Scopes.Email, OAuth2Constants.Scopes.Address,
            OAuth2Constants.Scopes.Phone, OAuth2Constants.Scopes.OfflineAccess
        ];
        public static readonly IReadOnlyList<string> Default =
            [OpenId, OAuth2Constants.Scopes.Profile, OAuth2Constants.Scopes.Email];
        
    }

    public static class Endpoints
    {
        public const string Discovery = "/.well-known/openid_configuration";
        public const string Jwks = "/.well-known/jwks.json";
    }
}