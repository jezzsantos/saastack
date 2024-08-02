using Common.Extensions;

namespace Common;

/// <summary>
///     Maintains the metadata from a billing provider to maintain the state of a subscription
/// </summary>
public class SubscriptionMetadata : Dictionary<string, string>, IEquatable<SubscriptionMetadata>
{
    public SubscriptionMetadata(Dictionary<string, string> metadata) : base(metadata)
    {
    }

    public SubscriptionMetadata()
    {
    }

    public bool Equals(SubscriptionMetadata? other)
    {
        if (other.NotExists())
        {
            return false;
        }

        return Count == other.Count
               && !this.Except(other).Any();
    }

    public override bool Equals(object? obj)
    {
        if (ReferenceEquals(null, obj))
        {
            return false;
        }

        if (ReferenceEquals(this, obj))
        {
            return true;
        }

        if (obj.GetType() != GetType())
        {
            return false;
        }

        return Equals((SubscriptionMetadata)obj);
    }

    public override int GetHashCode()
    {
        var hash = 0;
        foreach (var pair in this)
        {
            hash ^= pair.GetHashCode();
        }

        return hash;
    }

    public static bool operator ==(SubscriptionMetadata? left, SubscriptionMetadata? right)
    {
        return Equals(left, right);
    }

    public static bool operator !=(SubscriptionMetadata? left, SubscriptionMetadata? right)
    {
        return !Equals(left, right);
    }
}