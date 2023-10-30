using Common;
using Common.Extensions;
using Domain.Common;
using Domain.Common.ValueObjects;

namespace CarsDomain;

public sealed class Year : SingleValueObjectBase<Year, int>
{
    public const int MinYear = 1917;
    public static readonly int MaxYear = DateTime.UtcNow.AddYears(3).Year;

    public static Result<Year, Error> Create(int number)
    {
        var value = new Year(number);
        if (number.IsInvalidParameter(y => y >= MinYear && y <= MaxYear, nameof(number),
                Resources.Year_InvalidNumber.Format(MinYear, MaxYear), out var error))
        {
            return error;
        }

        return value;
    }

    private Year(int number) : base(number)
    {
    }

    public static ValueObjectFactory<Year> Rehydrate()
    {
        return (property, _) =>
        {
            var parts = RehydrateToList(property, true);
            return new Year(parts[0].ToIntOrDefault(0));
        };
    }

    public int Number => Value;
}