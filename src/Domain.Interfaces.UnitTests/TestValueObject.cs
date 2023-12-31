using Common;
using Domain.Common.ValueObjects;

namespace Domain.Interfaces.UnitTests;

public class TestValueObject : ValueObjectBase<TestValueObject>
{
    public static Result<TestValueObject, Error> Create(string value)
    {
        return new TestValueObject(value);
    }

    private TestValueObject(string value)
    {
        Text = value;
    }

    public string Text { get; }

    protected override IEnumerable<object?> GetAtomicValues()
    {
        return new object?[] { Text };
    }
}