namespace Infrastructure.Interfaces;

public static class AuditingConstants
{
    public const string APIKeyAuthenticationFailed = "APIKeyAuthentication.Failed.InvalidAPIKey";
    public const string HMACAuthenticationFailed = "HMACAuthentication.Failed.InvalidSignature";
    public const string PrivateInterHostAuthenticationFailed = "PrivateInterHostAuthentication.Failed.InvalidSignature";
}