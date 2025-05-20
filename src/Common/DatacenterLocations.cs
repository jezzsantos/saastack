using Common.Extensions;

namespace Common;

public static class DatacenterLocations
{
    //EXTEND: Add new regions here
    public const string AustraliaEastCode = "australiaeast";
    public const string LocalCode = "localonly";
    public const string UnknownCode = "unknown";
    public const string UsaCentralCode = "centralus";
    public static readonly DatacenterLocation Unknown = DatacenterLocation.Create(UnknownCode, "???");
    public static readonly DatacenterLocation Local = DatacenterLocation.Create(LocalCode, "loc");
    public static readonly DatacenterLocation AustraliaEast = DatacenterLocation.Create(AustraliaEastCode, "aus");

    public static readonly DatacenterLocation UsaCentral = DatacenterLocation.Create(UsaCentralCode, "usc");
    private static readonly IReadOnlyList<DatacenterLocation>
        AllLocations = [Unknown, Local, AustraliaEast, UsaCentral];

    /// <summary>
    ///     Returns the specified location by its <see cref="locationCode" /> if it exists
    /// </summary>
    public static DatacenterLocation? Find(string? locationCode)
    {
        if (locationCode.NotExists())
        {
            return null;
        }

        //Find the matching location
        return AllLocations.FirstOrDefault(l => l.Code.EqualsIgnoreCase(locationCode));
    }

    /// <summary>
    ///     Returns the specified location by its <see cref="locationCode" /> if it exists,
    ///     or returns <see cref="Unknown" />
    /// </summary>
    public static DatacenterLocation FindOrDefault(string? locationCode)
    {
        var exists = Find(locationCode);
        return exists.Exists()
            ? exists
            : Unknown;
    }
}

/// <summary>
///     Provides a data center location that hosts can be running in
/// </summary>
public sealed class DatacenterLocation : IEquatable<DatacenterLocation>
{
    public static DatacenterLocation Create(string code, string abbreviation)
    {
        code.ThrowIfNotValuedParameter(nameof(code));
        abbreviation.ThrowIfInvalidParameter(IsValidCode, nameof(abbreviation),
            Resources.TimezoneIana_InvalidStandardCode.Format(abbreviation));

        return new DatacenterLocation(code, abbreviation);

        static bool IsValidCode(string? code)
        {
            if (code.HasNoValue())
            {
                return false;
            }

            return code.IsMatchWith(@"^[\w\?]{3,5}$");
        }
    }

    private DatacenterLocation(string code, string abbreviation)
    {
        code.ThrowIfNotValuedParameter(nameof(code));
        abbreviation.ThrowIfNotValuedParameter(nameof(abbreviation));

        Code = code;
        Abbreviation = abbreviation;
    }

    public string Abbreviation { get; }

    public string Code { get; }

    public bool Equals(DatacenterLocation? other)
    {
        if (ReferenceEquals(null, other))
        {
            return false;
        }

        if (ReferenceEquals(this, other))
        {
            return true;
        }

        return Code == other.Code;
    }

    public override bool Equals(object? obj)
    {
        if (ReferenceEquals(null, obj))
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

        return Equals((DatacenterLocation)obj);
    }

    public override int GetHashCode()
    {
        Code.ThrowIfNotValuedParameter(nameof(Code));

        return Code.GetHashCode();
    }

    public static bool operator ==(DatacenterLocation left, DatacenterLocation right)
    {
        return Equals(left, right);
    }

    public static bool operator !=(DatacenterLocation left, DatacenterLocation right)
    {
        return !Equals(left, right);
    }

    /// <summary>
    ///     Returns the <see cref="Code" /> of the location
    /// </summary>
    public override string ToString()
    {
        return Code;
    }
}