using Domain.Interfaces.Validations;

namespace OrganizationsDomain;

public static class Validations
{
    public static class Organization
    {
        public static readonly Validation DisplayName = CommonValidations.DescriptiveName();
    }
}