using Common;
using Common.Extensions;
using Domain.Common.Extensions;
using Domain.Common.ValueObjects;
using Domain.Interfaces;
using Domain.Interfaces.Validations;

namespace Domain.Shared;

public sealed class PhoneNumber : SingleValueObjectBase<PhoneNumber, string>
{
    public static Result<PhoneNumber, Error> Create(string phoneNumber)
    {
        if (phoneNumber.IsNotValuedParameter(nameof(phoneNumber), out var error1))
        {
            return error1;
        }

        if (phoneNumber.IsInvalidParameter(CommonValidations.PhoneNumber, nameof(phoneNumber),
                Resources.PhoneNumber_InvalidPhoneNumber, out var error2))
        {
            return error2;
        }

        return new PhoneNumber(phoneNumber);
    }

    private PhoneNumber(string phoneNumber) : base(phoneNumber)
    {
    }

    public string Number => Value;

    public static ValueObjectFactory<PhoneNumber> Rehydrate()
    {
        return (property, _) => new PhoneNumber(property);
    }
}