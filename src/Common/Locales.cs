using System.Text.RegularExpressions;
using Common.Extensions;

namespace Common;

public static class Locales
{
    public static readonly Bcp47Locale EnglishUs = Bcp47Locale.Create("en", null, "US");
    public static readonly Bcp47Locale EnglishNz = Bcp47Locale.Create("en", null, "NZ");
    public static readonly Bcp47Locale Default = EnglishUs;

    /// <summary>
    ///     Whether the specified locale by its <see cref="locale" /> exists
    /// </summary>
    public static bool Exists(string? locale)
    {
        return Find(locale).Exists();
    }

    /// <summary>
    ///     Returns the specified locale by its <see cref="locale" /> if it exists
    /// </summary>
    public static Bcp47Locale? Find(string? locale)
    {
        if (locale.NotExists())
        {
            return null;
        }

        var (languageCode, scriptCode, regionCode, isValid) = Bcp47Locale.ParseLocale(locale);
        if (isValid)
        {
            return Bcp47Locale.Create(languageCode, scriptCode, regionCode);
        }

        return null;
    }

    /// <summary>
    ///     Returns the specified currency by its <see cref="currencyCodeOrNumber" /> if it exists,
    ///     or returns <see cref="Default" />
    /// </summary>
    public static Bcp47Locale FindOrDefault(string? locale)
    {
        var exists = Find(locale);
        return exists.Exists()
            ? exists
            : Default;
    }
}

/// <summary>
///     Provides a locale based on the IETF BCP 47 language tag
///     Language tags from ISO 639 <see href="https://en.wikipedia.org/wiki/IETF_language_tag" />
///     Script codes from ISO 15924 <see href="https://unicode.org/iso15924/iso15924-codes.html" />
///     Region codes from ISO 3166-1 <see href="https://en.wikipedia.org/wiki/List_of_ISO_3166_country_codes" />
/// </summary>
public class Bcp47Locale : IEquatable<Bcp47Locale>
{
    public static Bcp47Locale Create(string languageCode, string? scriptCode, string? regionCode)
    {
        languageCode.ThrowIfInvalidParameter(IsLanguageCode, nameof(languageCode),
            Resources.Bcp47Locale_InvalidLanguageCode.Format(languageCode));
        if (scriptCode.HasValue())
        {
            scriptCode.ThrowIfInvalidParameter(IsScriptCode, nameof(scriptCode),
                Resources.Bcp47Locale_InvalidScriptCode.Format(scriptCode));
        }

        if (regionCode.HasValue())
        {
            regionCode.ThrowIfInvalidParameter(IsRegionCode, nameof(regionCode),
                Resources.Bcp47Locale_InvalidRegionCode.Format(regionCode));
        }

        return new Bcp47Locale(languageCode, scriptCode, regionCode);
    }

    private Bcp47Locale(string languageCode, string? scriptCode, string? regionCode)
    {
        LanguageCode = languageCode;
        ScriptCode = scriptCode;
        RegionCode = regionCode;
    }

    public string LanguageCode { get; }

    public string? RegionCode { get; }

    public string? ScriptCode { get; }

    public bool Equals(Bcp47Locale? other)
    {
        if (other is null)
        {
            return false;
        }

        if (ReferenceEquals(this, other))
        {
            return true;
        }

        return LanguageCode == other.LanguageCode && ScriptCode == other.ScriptCode
                                                  && RegionCode == other.RegionCode;
    }

    public override bool Equals(object? obj)
    {
        if (obj is null)
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

        return Equals((Bcp47Locale)obj);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(LanguageCode, ScriptCode, RegionCode);
    }

    public static bool operator ==(Bcp47Locale? left, Bcp47Locale? right)
    {
        return Equals(left, right);
    }

    public static bool operator !=(Bcp47Locale? left, Bcp47Locale? right)
    {
        return !Equals(left, right);
    }

    public static (string LanguageCode, string? ScriptCode, string? RegionCode, bool IsValid) ParseLocale(
        string? locale)
    {
        if (locale.HasNoValue())
        {
            return (string.Empty, null, null, false);
        }

        var matches = Regex.Match(locale,
            @"^(?<languageCode>[a-z]{2,4})(-(?<scriptCode>[A-Z][a-z]{3}))?(-(?<regionCode>[A-Z]{2}|[0-9]{3}))?$");
        if (!matches.Success)
        {
            return (string.Empty, null, null, false);
        }

        var languageCode = matches.Groups["languageCode"].Value;
        var scriptCodeMatch = matches.Groups["scriptCode"];
        var scriptCode = scriptCodeMatch.Success
            ? scriptCodeMatch.Value
            : null;
        var regionCodeMatch = matches.Groups["regionCode"];
        var regionCode = regionCodeMatch.Success
            ? regionCodeMatch.Value
            : null;

        return (languageCode, scriptCode, regionCode, true);
    }

    public override string ToString()
    {
        return
            $"{LanguageCode}{(ScriptCode.HasValue()
                ? $"-{ScriptCode}"
                : string.Empty)}{(RegionCode.HasValue()
                ? $"-{RegionCode}"
                : string.Empty)}";
    }

    private static bool IsLanguageCode(string? code)
    {
        if (code.HasNoValue())
        {
            return false;
        }

        return code.IsMatchWith(@"^[a-z]{2,4}$");
    }

    private static bool IsScriptCode(string? code)
    {
        if (code.HasNoValue())
        {
            return true;
        }

        return code.IsMatchWith(@"^[a-zA-Z]{4}$");
    }

    private static bool IsRegionCode(string? code)
    {
        if (code.HasNoValue())
        {
            return true;
        }

        return code.IsMatchWith(@"^([A-Z]{2}|[0-9]{3})$");
    }
}