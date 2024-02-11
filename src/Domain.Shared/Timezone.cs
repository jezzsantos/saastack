using Common;
using Common.Extensions;
using Domain.Common.Extensions;
using Domain.Common.ValueObjects;
using Domain.Interfaces;
using Domain.Interfaces.Validations;

namespace Domain.Shared;

public sealed class Timezone : SingleValueObjectBase<Timezone, TimezoneIANA>
{
    public static readonly Timezone Default = Create(Timezones.Default).Value;

    public static Result<Timezone, Error> Create(string timezone)
    {
        if (timezone.IsNotValuedParameter(nameof(timezone), out var error1))
        {
            return error1;
        }

        if (timezone.IsInvalidParameter(CommonValidations.Timezone, nameof(timezone),
                Resources.Timezone_InvalidTimezone, out var error2))
        {
            return error2;
        }

        return new Timezone(Timezones.FindOrDefault(timezone));
    }

    public static Result<Timezone, Error> Create(TimezoneIANA timezone)
    {
        return new Timezone(timezone);
    }

    private Timezone(TimezoneIANA timezone) : base(timezone)
    {
    }

    public TimezoneIANA Code => Value;

    public static ValueObjectFactory<Timezone> Rehydrate()
    {
        return (property, _) => new Timezone(Timezones.FindOrDefault(property));
    }
}