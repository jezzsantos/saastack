using Common;
using Common.Extensions;
using Domain.Common.ValueObjects;
using Domain.Interfaces;
using Domain.Interfaces.Services;
using Domain.Shared.Organizations;
using JetBrains.Annotations;

namespace OrganizationsDomain;

public sealed class Setting : ValueObjectBase<Setting>
{
    public static Result<Setting, Error> Create(object value, bool isEncrypted)
    {
        var valueType = value switch
        {
            string _ => SettingValueType.String,
            int _ => SettingValueType.Number,
            long _ => SettingValueType.Number,
            double _ => SettingValueType.Number,
            decimal _ => SettingValueType.Number,
            bool _ => SettingValueType.Boolean,
            _ => throw new InvalidOperationException(Resources.Setting_InvalidDataType.Format(value.GetType().Name))
        };

        var canBeEncrypted = valueType == SettingValueType.String && isEncrypted;
        return new Setting(value, valueType, canBeEncrypted);
    }

    public static Result<Setting, Error> Create(string value, bool isEncrypted)
    {
        return new Setting(value, SettingValueType.String, isEncrypted);
    }

    public static Result<Setting, Error> Create(int value)
    {
        return new Setting(value, SettingValueType.Number, false);
    }

    public static Result<Setting, Error> Create(double value)
    {
        return new Setting(value, SettingValueType.Number, false);
    }

    public static Result<Setting, Error> Create(bool value)
    {
        return new Setting(value, SettingValueType.Boolean, false);
    }

    private Setting(object value, SettingValueType valueType, bool isEncrypted)
    {
        Value = value;
        ValueType = valueType;
        IsEncrypted = isEncrypted;
    }

    public bool IsEncrypted { get; }

    public object Value { get; }

    public SettingValueType ValueType { get; }

    [UsedImplicitly]
    public static ValueObjectFactory<Setting> Rehydrate()
    {
        return (property, _) =>
        {
            var parts = RehydrateToList(property, false);
            return new Setting(parts[0]!, parts[1].ToEnumOrDefault(SettingValueType.String), parts[2].ToBool());
        };
    }

    protected override IEnumerable<object?> GetAtomicValues()
    {
        return [Value, ValueType, IsEncrypted];
    }

    public static Result<Setting, Error> From(string stringValue, SettingValueType valueType, bool isEncrypted,
        ITenantSettingService tenantSettingService)
    {
        object? value;
        if (isEncrypted && valueType == SettingValueType.String)
        {
            value = tenantSettingService.Decrypt(stringValue);
        }
        else
        {
            value = valueType switch
            {
                SettingValueType.String => stringValue,
                SettingValueType.Number => stringValue.ToDouble(),
                SettingValueType.Boolean => stringValue.ToBool(),
                _ => throw new InvalidOperationException(Resources.Setting_InvalidValueType.Format(valueType))
            };
        }

        return new Setting(value, valueType, isEncrypted);
    }
}