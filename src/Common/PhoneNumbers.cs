using Common.Extensions;
using PhoneNumbers;

namespace Common;

public static class PhoneNumbers
{
    /// <summary>
    ///     Whether the specified number represents an international number
    /// </summary>
    public static bool IsValidInternational(string? value)
    {
        if (value.HasNoValue())
        {
            return false;
        }

        if (!value.StartsWith("+"))
        {
            return false;
        }

        var util = PhoneNumberUtil.GetInstance();
        try
        {
            var number = util.Parse(value, null);
            return util.IsValidNumber(number);
        }
        catch (NumberParseException)
        {
            return false;
        }
    }

    /// <summary>
    ///     Attempts to convert the specified phone number to an international format.
    /// </summary>
    public static bool TryToInternational(string? value, string? countryCode, out string? result)
    {
        if (value.HasNoValue())
        {
            result = null;
            return false;
        }

        var adjusted = value;
        if (countryCode.HasNoValue()
            && !value.StartsWith("+")
            && !value.StartsWith("0"))
        {
            adjusted = $"+{adjusted}";
        }

        var util = PhoneNumberUtil.GetInstance();
        try
        {
            var number = util.Parse(adjusted, countryCode);

            if (!util.IsValidNumber(number))
            {
                result = null;
                return false;
            }

            var international = util.Format(number, PhoneNumberFormat.INTERNATIONAL);

            result = international;
            return true;
        }
        catch (NumberParseException)
        {
            result = null;
            return false;
        }
    }
}