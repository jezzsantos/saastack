using Common;
using Domain.Common.Extensions;
using Domain.Common.ValueObjects;
using Domain.Interfaces;
using Domain.Shared;
using JetBrains.Annotations;

namespace IdentityDomain;

public sealed class Registration : ValueObjectBase<Registration>
{
    public static Result<Registration, Error> Create(string emailAddress, string name)
    {
        if (name.IsInvalidParameter(Validations.Credentials.Person.DisplayName, nameof(name), null, out var error1))
        {
            return error1;
        }

        var em = EmailAddress.Create(emailAddress);
        if (em.IsFailure)
        {
            return em.Error;
        }

        var pdm = PersonDisplayName.Create(name);
        if (pdm.IsFailure)
        {
            return pdm.Error;
        }

        return new Registration(em.Value, pdm.Value);
    }

    private Registration(EmailAddress emailAddress, PersonDisplayName name)
    {
        EmailAddress = emailAddress;
        Name = name;
    }

    public EmailAddress EmailAddress { get; }

    public PersonDisplayName Name { get; }

    [UsedImplicitly]
    public static ValueObjectFactory<Registration> Rehydrate()
    {
        return (property, container) =>
        {
            var parts = RehydrateToList(property, false);
            return new Registration(EmailAddress.Rehydrate()(parts[0]!, container),
                PersonDisplayName.Rehydrate()(parts[1]!, container));
        };
    }

    protected override IEnumerable<object> GetAtomicValues()
    {
        return new object[] { EmailAddress, Name };
    }
}