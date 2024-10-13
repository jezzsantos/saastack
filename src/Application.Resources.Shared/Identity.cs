using Application.Interfaces.Resources;

namespace Application.Resources.Shared;

public class AuthenticateTokens
{
    public required AuthenticationToken AccessToken { get; set; }

    public required AuthenticationToken RefreshToken { get; set; }

    public required string UserId { get; set; }
}

public class ProviderAuthenticationTokens
{
    public required AuthenticationToken AccessToken { get; set; }

    public required List<AuthenticationToken> OtherTokens { get; set; }

    public required string Provider { get; set; }

    public required AuthenticationToken? RefreshToken { get; set; }
}

public class AuthenticationToken
{
    public required DateTime? ExpiresOn { get; set; }

    public required TokenType Type { get; set; }

    public required string Value { get; set; }
}

public class APIKey : IIdentifiableResource
{
    public string? Description { get; set; }

    public DateTime? ExpiresOnUtc { get; set; }

    public required string Key { get; set; }

    public required string UserId { get; set; }

    public required string Id { get; set; }
}

public class AuthToken
{
    public AuthToken(TokenType type, string value, DateTime? expiresOn)
    {
        Type = type;
        Value = value;
        ExpiresOn = expiresOn;
    }

    public DateTime? ExpiresOn { get; }

    public TokenType Type { get; }

    public string Value { get; }
}

public enum TokenType
{
    OtherToken = 0,
    AccessToken = 1,
    RefreshToken = 2
}