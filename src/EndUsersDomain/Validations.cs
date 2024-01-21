using Domain.Interfaces.Validations;

namespace EndUsersDomain;

public static class Validations
{
    public static readonly Validation Role = CommonValidations.RoleLevel;
}