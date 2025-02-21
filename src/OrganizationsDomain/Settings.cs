using Common;
using Common.Extensions;
using Domain.Common.ValueObjects;
using Domain.Interfaces;
using Domain.Interfaces.ValueObjects;
using JetBrains.Annotations;

namespace OrganizationsDomain;

public sealed class Settings : ValueObjectBase<Settings>
{
    public static readonly Settings Empty = new();

    public static Result<Settings, Error> Create(Dictionary<string, Setting> properties)
    {
        return new Settings(properties);
    }

    private Settings()
    {
        Properties = new Dictionary<string, Setting>();
    }

    private Settings(Dictionary<string, Setting> properties)
    {
        Properties = properties;
    }

    public Dictionary<string, Setting> Properties { get; }

    [UsedImplicitly]
    public static ValueObjectFactory<Settings> Rehydrate()
    {
        return (property, _) =>
        {
            var parts = RehydrateToList(property, false);
            return new Settings(parts[0]!.FromJson<Dictionary<string, Setting>>()!);
        };
    }

    protected override IEnumerable<object> GetAtomicValues()
    {
        return new[] { Properties.ToJson()! };
    }

    public Result<Settings, Error> AddOrUpdate(string name, object value, bool isEncrypted)
    {
        var settingValue = Setting.Create(value, isEncrypted);
        if (settingValue.IsFailure)
        {
            return settingValue.Error;
        }

        return AddOrUpdate(name, settingValue.Value);
    }

    public Result<Settings, Error> AddOrUpdate(string name, Setting setting)
    {
        var settings = new Settings(new Dictionary<string, Setting>(Properties))
        {
            Properties =
            {
                [name] = setting
            }
        };

        return settings;
    }

    [SkipImmutabilityCheck]
    public bool TryGet(string key, out Setting? setting)
    {
        return Properties.TryGetValue(key, out setting);
    }
}