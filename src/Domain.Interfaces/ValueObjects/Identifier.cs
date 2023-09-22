namespace Domain.Interfaces.ValueObjects;

public class Identifier
{
    public static Identifier Create(string value)
    {
        return new Identifier(value);
    }

    public Identifier(string value)
    {
        Value = value;
    }

    public string Value { get; }

    public override string ToString()
    {
        return Value;
    }
}