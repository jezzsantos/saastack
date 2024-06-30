using Domain.Interfaces.Validations;

namespace IdentityDomain;

public static class Validations
{
    public static class AuthTokens
    {
        public static readonly Validation RefreshToken = CommonValidations.RandomToken();
    }

    public static class Machine
    {
        public static readonly Validation Name = CommonValidations.DescriptiveName(1, 200);
    }

    public static class Credentials
    {
        public static readonly Validation InvitationToken = CommonValidations.RandomToken();

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
            public static readonly Validation ResetToken = CommonValidations.RandomToken();
            public static readonly Validation VerificationToken = CommonValidations.RandomToken();
        }
    }

    public static class ApiKey
    {
        public static readonly Validation Description = CommonValidations.DescriptiveName();
        public static readonly TimeSpan MaximumExpiryPeriod = TimeSpan.FromDays(30);
        public static readonly TimeSpan MinimumExpiryPeriod = TimeSpan.FromHours(1);
    }
}