namespace Domain.Shared.Identities;

public enum OAuth2CodeChallengeMethod
{
    Plain = 0, // OAuth2Constants.CodeChallengeMethods.Plain
    S256 = 1 // OAuth2Constants.CodeChallengeMethods.S256
}