using Domain.Interfaces.Validations;

namespace IdentityDomain;

public static class Validations
{
    private static readonly Validation RandomToken = new("^[a-zA-Z0-9+/]{41,44}[=]{0,3}$");

    public static class Machine
    {
        public static readonly Validation Name = CommonValidations.DescriptiveName(1, 200);
    }

    public static class Credentials
    {
        public static readonly Validation VerificationToken = RandomToken;

        public static class Person
        {
            public static readonly Validation Name = CommonValidations.DescriptiveName(1, 50);
            public static readonly Validation DisplayName = Name;
        }

        public static class Login
        {
            public const int DefaultMaxFailedPasswordAttempts = 5;
#if TESTINGONLY
            public const int DefaultCooldownPeriodMinutes = 5;
#else
        public const int DefaultCooldownPeriodMinutes = 10;
#endif
            public static readonly Validation<int> MaxFailedPasswordAttempts = new(x => x is > 0 and < 100);
            public static readonly Validation<TimeSpan> CooldownPeriod = new(
                x => x > TimeSpan.Zero && x <= TimeSpan.FromDays(1)
            );
        }

        public static class Password
        {
            public static readonly Validation ResetToken = RandomToken;
        }
    }
}