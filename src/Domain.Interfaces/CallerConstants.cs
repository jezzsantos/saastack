namespace Domain.Interfaces;

/// <summary>
///     Constants for the caller
/// </summary>
public static class CallerConstants
{
    public const string
        AnonymousUserId = "xxx_anonymous0000000000000"; // An account used by anonymous (unauthenticated) users

    public const string
        ExternalWebhookAccountUserId =
            "xxx_externalwebhook0000001"; // An account we use to represent inbound webhook calls from 3rd party integrations

    public const string
        MaintenanceAccountUserId =
            "xxx_maintenance00000000001"; // A service account we use to represent internal calls between distributed services and domain notifications

    public const string
        ServiceClientAccountUserId =
            "xxx_serviceclient000000001"; // A service account we use to represent outbound calls to 3rd party integrations

    private static readonly IReadOnlyList<string> ServiceAccounts = new List<string>
    {
        MaintenanceAccountUserId,
        ServiceClientAccountUserId,
        ExternalWebhookAccountUserId
    };

    public static bool IsAnonymousUser(string userId)
    {
        return userId == AnonymousUserId;
    }

    public static bool IsServiceAccount(string userId)
    {
        return ServiceAccounts.Contains(userId);
    }
}