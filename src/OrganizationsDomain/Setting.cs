using Common;
using Common.Extensions;
using Domain.Common.ValueObjects;
using Domain.Interfaces;

namespace OrganizationsDomain;

public sealed class Setting : ValueObjectBase<Setting>
{
    public static Result<Setting, Error> Create(string value, bool isEncrypted)
    {
        return new Setting(value, isEncrypted);
    }

    private Setting(string value, bool isEncrypted)
    {
        Value = value;
        IsEncrypted = isEncrypted;
    }

    public bool IsEncrypted { get; }

    public string Value { get; }

    public static ValueObjectFactory<Setting> Rehydrate()
    {
        return (property, _) =>
        {
            var parts = RehydrateToList(property, false);
            return new Setting(parts[0]!, parts[1].ToBool());
        };
    }

    protected override IEnumerable<object> GetAtomicValues()
    {
        return new object[] { Value, IsEncrypted };
    }
}