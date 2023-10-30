namespace Domain.Interfaces.ValueObjects;

public interface IPersistableValueObject
{
    string Dehydrate();
}