using Domain.Interfaces.Validations;

namespace CarsDomain;

public static class Validations
{
    public static class Car
    {
        public static readonly Validation Jurisdiction = new(@"^[\d\w\-\. ]{1,50}$", 1, 50);
        public static readonly Validation Make = CommonValidations.DescriptiveName(2, 50);
        public static readonly TimeSpan MinScheduledMaintenanceLeadTime = TimeSpan.FromHours(24);
        public static readonly Validation Model = CommonValidations.DescriptiveName(2, 50);
        public static readonly Validation NumberPlate = new(@"^[\d\w ]{1,15}$", 1, 15);
        public static readonly Validation Reason = CommonValidations.FreeformText(0, 200);

        public static class Year
        {
            public const int Min = 1900;
            public static readonly int Max = DateTime.UtcNow.Year + 1;
        }
    }

    public static class Unavailability
    {
        public static readonly Validation Reference = CommonValidations.DescriptiveName(1, 250);
    }
}