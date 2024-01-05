using Application.Interfaces.Resources;

namespace Application.Resources.Shared;

public class AuthenticateTokens
{
    public required string AccessToken { get; set; }

    public required DateTime ExpiresOn { get; set; }

    public required string RefreshToken { get; set; }
}

public class APIKey : IIdentifiableResource
{
    public string? Description { get; set; }

    public DateTime? ExpiresOnUtc { get; set; }

    public required string Key { get; set; }

    public required string UserId { get; set; }

    public required string Id { get; set; }
}