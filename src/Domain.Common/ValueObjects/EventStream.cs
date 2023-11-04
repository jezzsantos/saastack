using Common;
using Common.Extensions;

namespace Domain.Common.ValueObjects;

/// <summary>
///     Provides a summary of an event stream
/// </summary>
public sealed class EventStream : ValueObjectBase<EventStream>
{
    public const int FirstVersion = 1;
    public const int NoVersion = 0;

    public static Result<EventStream, Error> Create(int firstEventVersion, int lastEventVersion)
    {
        if (firstEventVersion.IsInvalidParameter(ver => ver >= NoVersion, nameof(firstEventVersion),
                Resources.EventStream_ZeroFirstVersion, out var error1))
        {
            return error1;
        }

        if (lastEventVersion.IsInvalidParameter(ver => ver >= NoVersion, nameof(lastEventVersion),
                Resources.EventStream_ZeroLastVersion, out var error2))
        {
            return error2;
        }

        return new EventStream(firstEventVersion, lastEventVersion);
    }

    public static EventStream Create()
    {
        return new EventStream();
    }

    private EventStream()
    {
        FirstEventVersion = NoVersion;
        LastEventVersion = NoVersion;
    }

    private EventStream(int firstEventVersion, int lastEventVersion)
    {
        FirstEventVersion = firstEventVersion;
        LastEventVersion = lastEventVersion;
    }

    public int FirstEventVersion { get; }

    public bool HasChanges => FirstEventVersion != NoVersion;

    public int LastEventVersion { get; }

    public static ValueObjectFactory<EventStream> Rehydrate()
    {
        return (property, _) =>
        {
            var parts = RehydrateToList(property, false);
            return new EventStream(parts[0].ToInt(), parts[1].ToInt());
        };
    }

    protected override IEnumerable<object?> GetAtomicValues()
    {
        return new object[] { FirstEventVersion, LastEventVersion };
    }

    public Result<EventStream, Error> Next()
    {
        return Create(FirstEventVersion, LastEventVersion + 1);
    }

    public Result<EventStream, Error> UpdateChange(int version)
    {
        if (version.IsInvalidParameter(ver => ver > NoVersion, nameof(version),
                Resources.EventStream_OutOfOrderChange.Format(version, FirstVersion), out var error))
        {
            return error;
        }

        var updated = Create(FirstEventVersion, LastEventVersion);
        if (!updated.IsSuccessful)
        {
            return updated.Error;
        }

        var isFirstChange = !updated.Value.HasChanges;
        if (isFirstChange)
        {
            updated = Create(version, version);
            if (!updated.IsSuccessful)
            {
                return updated.Error;
            }
        }

        if (isFirstChange)
        {
            var expectedVersion = updated.Value.LastEventVersion;
            if (version != expectedVersion)
            {
                return Error.RuleViolation(Resources.EventStream_OutOfOrderChange.Format(version, expectedVersion));
            }
        }
        else
        {
            var next = updated.Value.Next();
            if (!next.IsSuccessful)
            {
                return next;
            }

            var expectedVersion = next.Value.LastEventVersion;
            if (version != expectedVersion)
            {
                return Error.RuleViolation(Resources.EventStream_OutOfOrderChange.Format(version, expectedVersion));
            }
        }

        if (!isFirstChange)
        {
            return updated.Value.Next();
        }

        return updated;
    }
}