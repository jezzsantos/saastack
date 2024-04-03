namespace Domain.Shared.Identities;

public class APIKeyToken
{
    public required string ApiKey { get; set; }

    public required string Key { get; set; }

    public required string Prefix { get; set; }

    public required string Token { get; set; }
}