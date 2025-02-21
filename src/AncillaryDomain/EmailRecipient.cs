using Common;
using Common.Extensions;
using Domain.Common.ValueObjects;
using Domain.Interfaces;
using Domain.Shared;
using JetBrains.Annotations;

namespace AncillaryDomain;

public sealed class EmailRecipient : ValueObjectBase<EmailRecipient>
{
    public static Result<EmailRecipient, Error> Create(EmailAddress emailAddress, string displayName)
    {
        if (displayName.IsNotValuedParameter(nameof(displayName), out var error))
        {
            return error;
        }

        return new EmailRecipient(emailAddress, displayName);
    }

    private EmailRecipient(EmailAddress emailAddress, string displayName)
    {
        EmailAddress = emailAddress;
        DisplayName = displayName;
    }

    public string DisplayName { get; }

    public EmailAddress EmailAddress { get; }

    [UsedImplicitly]
    public static ValueObjectFactory<EmailRecipient> Rehydrate()
    {
        return (property, container) =>
        {
            var parts = RehydrateToList(property, false);
            return new EmailRecipient(EmailAddress.Rehydrate()(parts[0]!, container), parts[1]!);
        };
    }

    protected override IEnumerable<object?> GetAtomicValues()
    {
        return new object[] { EmailAddress, DisplayName };
    }
}