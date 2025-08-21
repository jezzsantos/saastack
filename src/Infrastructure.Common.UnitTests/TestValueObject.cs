using Domain.Common.ValueObjects;
using Domain.Interfaces;

namespace Infrastructure.Common.UnitTests;

public class TestValueObject : ValueObjectBase<TestValueObject>
{
    private TestValueObject(string property)
    {
        APropertyValue = property;
    }

    public string APropertyValue { get; }

    public static ValueObjectFactory<TestValueObject> Rehydrate()
    {
        return (property, _) => new TestValueObject(property);
    }

    protected override IEnumerable<object?> GetAtomicValues()
    {
        return [APropertyValue];
    }
}