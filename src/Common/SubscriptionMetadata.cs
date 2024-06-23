namespace Common;

/// <summary>
///     Maintains the metadata from a billing provider to maintain the state of a subscription
/// </summary>
public class SubscriptionMetadata : Dictionary<string, string>
{
    public SubscriptionMetadata(Dictionary<string, string> metadata) : base(metadata)
    {
    }

    public SubscriptionMetadata()
    {
    }
}