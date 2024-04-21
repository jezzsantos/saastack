using Domain.Interfaces.Validations;

namespace OrganizationsDomain;

public static class Validations
{
    public static readonly Validation DisplayName = CommonValidations.DescriptiveName();
    public static readonly Validation Role = CommonValidations.RoleLevel;

    public static class Avatar
    {
        public const long MaxSizeInBytes = 134_217_728; //approx 100MB
        public static readonly IReadOnlyList<string> AllowableContentTypes = new[]
        {
            "image/jpeg",
            "image/png",
            "image/gif"
        };
    }
}