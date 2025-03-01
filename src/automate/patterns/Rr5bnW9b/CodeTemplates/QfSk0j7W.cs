using Domain.Interfaces.Validations;

namespace {{SubdomainName | string.pascalplural}}Domain;

public static class Validations
{
    public static class {{SubdomainName | string.pascalsingular}}
    {
        //TODO: add other specific validation fields
        //For example: public static readonly Validation Name = CommonValidations.DescriptiveName(2, 50);
    }
}