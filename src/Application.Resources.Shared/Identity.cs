using Application.Interfaces.Resources;

namespace Application.Resources.Shared;

public class AuthenticateTokens
{
    public required AuthenticateToken AccessToken { get; set; }

    public required AuthenticateToken RefreshToken { get; set; }

    public required string UserId { get; set; }
}

public class AuthenticateToken
{
    public required DateTime ExpiresOn { get; set; }

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
    AccessToken = 1,
    RefreshToken = 2,
    IdToken = 3
}