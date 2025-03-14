using Common;
using Common.Extensions;
using Domain.Common.Extensions;
using Domain.Common.ValueObjects;
using Domain.Interfaces;
using Domain.Interfaces.Validations;
using Domain.Interfaces.ValueObjects;
using JetBrains.Annotations;

namespace Domain.Shared;

public sealed class EmailAddress : SingleValueObjectBase<EmailAddress, string>
{
    public static Result<EmailAddress, Error> Create(string emailAddress)
    {
        if (emailAddress.IsNotValuedParameter(nameof(emailAddress), out var error1))
        {
            return error1;
        }

        if (emailAddress.IsInvalidParameter(CommonValidations.EmailAddress, nameof(emailAddress),
                Resources.EmailAddress_InvalidAddress, out var error2))
        {
            return error2;
        }

        return new EmailAddress(emailAddress);
    }

    private EmailAddress(string emailAddress) : base(emailAddress.ToLowerInvariant())
    {
    }

    public string Address => Value;

    [UsedImplicitly]
    public static ValueObjectFactory<EmailAddress> Rehydrate()
    {
        return (property, _) => new EmailAddress(property);
    }

    [SkipImmutabilityCheck]
    public PersonName GuessPersonFullName()
    {
        var name = GuessPersonNameFromEmailAddress(Value);

        return PersonName.Create(name.FirstName, name.LastName).Value;
    }

    /// <summary>
    ///     Tries to guess the first and last names of the specified <see cref="emailAddress" />.
    /// </summary>
    private static (string FirstName, Optional<string> LastName) GuessPersonNameFromEmailAddress(string emailAddress)
    {
        if (emailAddress.HasNoValue())
        {
            return (emailAddress, Optional<string>.None);
        }

        const char usernameDelimiter = '.';
        var parts = emailAddress.Split(['+', '@'], StringSplitOptions.RemoveEmptyEntries);
        var username = parts[0];

        var usernameParts = username.Split(usernameDelimiter);

        var firstName = RefineName(usernameParts[0]);
        var lastName = usernameParts.Length > 1
            ? RefineName(usernameParts[^1]).ToOptional()
            : Optional<string>.None;

        if (!IsValidName(firstName))
        {
            firstName = Resources.EmailAddress_FallbackGuessedFirstName;
        }

        if (lastName.HasValue
            && !IsValidName(lastName.Value))
        {
            lastName = Optional<string>.None;
        }

        return (firstName, lastName);

        bool IsValidName(string name)
        {
            return name.IsMatchWith(@"^[\w]{2,}$");
        }

        string RefineName(string name)
        {
            return name.TrimNonAlpha().ToTitleCase();
        }
    }
}