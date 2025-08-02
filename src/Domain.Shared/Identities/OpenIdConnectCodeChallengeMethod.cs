namespace Domain.Shared.Identities;

public enum OpenIdConnectCodeChallengeMethod
{
    Plain = 0, // OAuth2Constants.CodeChallengeMethods.Plain
    S256 = 1 // OAuth2Constants.CodeChallengeMethods.S256
}