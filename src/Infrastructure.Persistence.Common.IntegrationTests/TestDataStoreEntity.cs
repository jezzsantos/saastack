using Application.Persistence.Interfaces;
using Common;
using Common.Extensions;
using Domain.Common.ValueObjects;
using Domain.Interfaces;
using JetBrains.Annotations;
using QueryAny;

namespace Infrastructure.Persistence.Common.IntegrationTests;

[EntityName("testentities")]
public class TestDataStoreEntity : IHasIdentity, IQueryableEntity
{
    private static int _instanceCounter;

    public TestDataStoreEntity()
    {
        Id = $"anid{++_instanceCounter:00000}";
    }

    public byte[] ABinaryValue { get; set; } = null!;

    public bool ABooleanValue { get; set; }

    public ComplexNonValueObject AComplexNonValueObjectValue { get; set; } = null!;

    public ComplexValueObject AComplexValueObjectValue { get; set; } = null!;

    public DateTimeOffset ADateTimeOffsetValue { get; set; }

    public DateTime ADateTimeUtcValue { get; set; }

    public double ADoubleValue { get; set; }

    public Guid AGuidValue { get; set; }

    public int AIntValue { get; set; }

    public long ALongValue { get; set; }

    public AnEnum AnEnumValue { get; set; }

    public AnEnum? AnNullableEnumValue { get; set; }

    public bool? ANullableBooleanValue { get; set; }

    public DateTimeOffset? ANullableDateTimeOffsetValue { get; set; }

    public DateTime? ANullableDateTimeUtcValue { get; set; }

    public double? ANullableDoubleValue { get; set; }

    public Guid? ANullableGuidValue { get; set; }

    public int? ANullableIntValue { get; set; }

    public long? ANullableLongValue { get; set; }

    public string AStringValue { get; set; } = null!;

    public DateTime? LastPersistedAtUtc { get; set; }

    public Optional<string> Id { get; set; }
}

[EntityName("testentities")]
[UsedImplicitly]
public class TestJoinedDataStoreEntity : TestDataStoreEntity
{
    public int AFirstIntValue { get; set; }

    public string AFirstStringValue { get; set; } = null!;
}

[EntityName("firstjoiningtestentities")]
public class FirstJoiningTestQueryStoreEntity : IHasIdentity, IQueryableEntity
{
    private static int _instanceCounter;

    public FirstJoiningTestQueryStoreEntity()
    {
        Id = $"anid{++_instanceCounter}";
    }

    public int AIntValue { get; set; }

    public string AStringValue { get; set; } = null!;

    public Optional<string> Id { get; set; }
}

[EntityName("secondjoiningtestentities")]
public class SecondJoiningTestQueryStoreEntity : IHasIdentity, IQueryableEntity
{
    private static int _instanceCounter;

    public SecondJoiningTestQueryStoreEntity()
    {
        Id = $"anid{++_instanceCounter}";
    }

    public int AIntValue { get; set; }

    public long ALongValue { get; set; }

    public string AStringValue { get; set; } = null!;

    public Optional<string> Id { get; set; }
}

public class ComplexNonValueObject
{
    public string APropertyValue { get; set; } = null!;

    public override bool Equals(object? obj)
    {
        if (obj is not ComplexNonValueObject other)
        {
            return false;
        }

        return Equals(other);
    }

    public override int GetHashCode()
    {
        // ReSharper disable once NonReadonlyMemberInGetHashCode
        return APropertyValue.HasValue()
            // ReSharper disable once NonReadonlyMemberInGetHashCode
            ? APropertyValue.GetHashCode()
            : 0;
    }

    public static bool operator ==(ComplexNonValueObject? left, ComplexNonValueObject? right)
    {
        return Equals(left, right);
    }

    public static bool operator !=(ComplexNonValueObject? left, ComplexNonValueObject? right)
    {
        return !Equals(left, right);
    }

    public override string ToString()
    {
        return this.ToJson()!;
    }

    protected bool Equals(ComplexNonValueObject other)
    {
        return APropertyValue == other.APropertyValue;
    }
}

public class ComplexValueObject : ValueObjectBase<ComplexValueObject>
{
    public static ComplexValueObject Create(string @string, int integer, bool boolean)
    {
        return new ComplexValueObject(@string, integer, boolean);
    }

    private ComplexValueObject(string @string, int integer, bool boolean)
    {
        AStringProperty = @string;
        AnIntName = integer;
        ABooleanPropertyName = boolean;
    }

    public bool ABooleanPropertyName { get; }

    public int AnIntName { get; }

    public string AStringProperty { get; }

    public override string Dehydrate()
    {
        return $"{AStringProperty}::{AnIntName}::{ABooleanPropertyName}";
    }

    public static ValueObjectFactory<ComplexValueObject> Rehydrate()
    {
        return (value, _) =>
        {
            var parts = RehydrateToList(value);
            return new ComplexValueObject(parts[0], parts[1].ToInt(), parts[2].ToBool());
        };
    }

    protected override IEnumerable<object> GetAtomicValues()
    {
        return new object[] { AStringProperty, AnIntName, ABooleanPropertyName };
    }

    private static List<string> RehydrateToList(string hydratedValue)
    {
        if (!hydratedValue.HasValue())
        {
            return new List<string>();
        }

        return hydratedValue
            .Split("::")
            .ToList();
    }
}

#pragma warning disable S2344
public enum AnEnum
#pragma warning restore S2344
{
    None = 0,
    AValue1 = 1,
    AValue2 = 2
}