using System.Runtime.Serialization;
using Common.Extensions;

namespace Common;

public static class Regions
{
    //EXTEND: Add new regions here
    public const string AustraliaEast = "australiaeast";
    public const string Local = "localonly";
    public const string UsaCentral = "centralus";
    private static readonly Dictionary<Region, string> CachedEnumValues = [];

    /// <summary>
    ///     Returns the assigned <see cref="EnumMemberAttribute.Value" /> for the value
    /// </summary>
    public static string GetDisplayName(this Region region)
    {
        if (CachedEnumValues.TryGetValue(region, out var name))
        {
            return name;
        }

        var enumName = GetEnumMemberValue();
        CachedEnumValues.TryAdd(region, enumName);
        return enumName;

        string GetEnumMemberValue()
        {
            var memberValue = region.ToString();
            var info = typeof(Region)
                .GetMember(region.ToString())[0]
                .GetCustomAttributes(typeof(EnumMemberAttribute), false)
                .OfType<EnumMemberAttribute>()
                .SingleOrDefault();
            if (info.Exists())
            {
                var value = info.Value;
                if (value.HasValue())
                {
                    memberValue = value;
                }
            }

            return memberValue;
        }
    }
}

/// <summary>
///     Defines the regions that hosts can be running in
/// </summary>
public enum Region
{
    [EnumMember(Value = "unknown")] Unknown = 0,
    [EnumMember(Value = Regions.Local)] Local = 1,

    [EnumMember(Value = Regions.AustraliaEast)]
    AustraliaEast = 2,

    [EnumMember(Value = Regions.UsaCentral)]
    // ReSharper disable once UnusedMember.Global
    UsaCentral = 3,

#if TESTINGONLY
    TestingOnly1 = 1000,
    [EnumMember] TestingOnly2 = 1001,
#endif
}