namespace IdentityDomain;

public enum OAuth2ResponseType
{
    Code = 0 // OAuth2Constants.ResponseTypes.Code
}

public enum OAuth2TokenType
{
    Bearer = 0 // OAuth2Constants.TokenTypes.Bearer
}

public enum OAuth2GrantType
{
    AuthorizationCode = 0, // OAuth2Constants.GrantTypes.AuthorizationCode
    RefreshToken = 1 // OAuth2Constants.GrantTypes.RefreshToken
}