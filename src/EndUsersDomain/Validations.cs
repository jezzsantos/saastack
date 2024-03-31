using Domain.Interfaces.Validations;

namespace EndUsersDomain;

public static class Validations
{
    public static readonly Validation Role = CommonValidations.RoleLevel;

    public static class Invitation
    {
        public static readonly Validation Token = CommonValidations.RandomToken();
    }
}