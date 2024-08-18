using Application.Persistence.Interfaces;
using Common;
using Common.Extensions;
using Domain.Common.ValueObjects;
using Domain.Interfaces;
using JetBrains.Annotations;
using QueryAny;

namespace Infrastructure.Persistence.Shared.IntegrationTests;

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

    public TestComplexObject AComplexObjectValue { get; set; } = null!;

    public DateTimeOffset ADateTimeOffsetValue { get; set; }

    public DateTime ADateTimeUtcValue { get; set; }

    public decimal ADecimalValue { get; set; }

    public double ADoubleValue { get; set; }

    public Guid AGuidValue { get; set; }

    public long ALongValue { get; set; }

    public int AnIntValue { get; set; }

    public Optional<TestComplexObject> AnOptionalComplexObjectValue { get; set; } = null!;

    public Optional<DateTime> AnOptionalDateTimeUtcValue { get; set; }

    public Optional<TestEnum> AnOptionalEnumValue { get; set; }

    public Optional<DateTime?> AnOptionalNullableDateTimeUtcValue { get; set; }

    public Optional<string?> AnOptionalNullableStringValue { get; set; }

    public Optional<string> AnOptionalStringValue { get; set; }

    public Optional<TestValueObject> AnOptionalValueObjectValue { get; set; } = null!;

    public bool? ANullableBooleanValue { get; set; }

    public DateTimeOffset? ANullableDateTimeOffsetValue { get; set; }

    public DateTime? ANullableDateTimeUtcValue { get; set; }

    public decimal? ANullableDecimalValue { get; set; }

    public double? ANullableDoubleValue { get; set; }

    public TestEnum? ANullableEnumValue { get; set; }

    public Guid? ANullableGuidValue { get; set; }

    public int? ANullableIntValue { get; set; }

    public long? ANullableLongValue { get; set; }

    public string AStringValue { get; set; } = null!;

    public TestValueObject AValueObjectValue { get; set; } = null!;

    public TestEnum EnumValue { get; set; }

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

    public int AnIntValue { get; set; }

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

    public long ALongValue { get; set; }

    public int AnIntValue { get; set; }

    public string AStringValue { get; set; } = null!;

    public Optional<string> Id { get; set; }
}

public class TestComplexObject
{
    public string APropertyValue { get; set; } = null!;

    public override bool Equals(object? obj)
    {
        if (obj is not TestComplexObject other)
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

    public static bool operator ==(TestComplexObject? left, TestComplexObject? right)
    {
        return Equals(left, right);
    }

    public static bool operator !=(TestComplexObject? left, TestComplexObject? right)
    {
        return !Equals(left, right);
    }

    public override string ToString()
    {
        return this.ToJson()!;
    }

    protected bool Equals(TestComplexObject other)
    {
        return APropertyValue == other.APropertyValue;
    }
}

public class TestValueObject : ValueObjectBase<TestValueObject>
{
    public static TestValueObject Create(string @string, int integer, bool boolean)
    {
        return new TestValueObject(@string, integer, boolean);
    }

    private TestValueObject(string @string, int integer, bool boolean)
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

    public static ValueObjectFactory<TestValueObject> Rehydrate()
    {
        return (value, _) =>
        {
            var parts = RehydrateToList(value);
            return new TestValueObject(parts[0], parts[1].ToInt(), parts[2].ToBool());
        };
    }

    protected override IEnumerable<object> GetAtomicValues()
    {
        return [AStringProperty, AnIntName, ABooleanPropertyName];
    }

    private static List<string> RehydrateToList(string hydratedValue)
    {
        if (!hydratedValue.HasValue())
        {
            return [];
        }

        return hydratedValue
            .Split("::")
            .ToList();
    }
}

public enum TestEnum
{
    NoValue = 0,
    AValue1 = 1,
    AValue2 = 2
}