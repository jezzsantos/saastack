using Common;
using Common.Extensions;
using Domain.Common.Extensions;
using Domain.Common.ValueObjects;
using Domain.Interfaces;

namespace AncillaryDomain;

public sealed class QueuedMessageId : SingleValueObjectBase<QueuedMessageId, string>
{
    public static readonly QueuedMessageId Empty = new(string.Empty);

    public static Result<QueuedMessageId, Error> Create(string id)
    {
        if (id.IsNotValuedParameter(nameof(id), out var error1))
        {
            return error1;
        }

        if (id.IsInvalidParameter(Validations.EmailDelivery.MessageId, nameof(id),
                Resources.QueuedMessageId_InvalidId, out var error2))
        {
            return error2;
        }

        return new QueuedMessageId(id);
    }

    private QueuedMessageId(string value) : base(value)
    {
    }

    public string Identifier => Value;

    public static ValueObjectFactory<QueuedMessageId> Rehydrate()
    {
        return (property, _) =>
        {
            var parts = RehydrateToList(property, true);
            return new QueuedMessageId(parts[0]!);
        };
    }
}