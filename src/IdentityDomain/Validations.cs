using Common.Extensions;
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
            public static readonly Validation ConfirmationCode = new(@"^\d{6}$");
            public static readonly Validation MfaPhoneNumber = CommonValidations.PhoneNumber;
            public static readonly Validation MfaToken = CommonValidations.RandomToken();
            public static readonly Validation OobCode = CommonValidations.RandomToken();
            public static readonly Validation RecoveryConfirmationCode = new(@"^[a-fA-F0-9]{8}$");
            public static readonly Validation ResetToken = CommonValidations.RandomToken();
            public static readonly Validation VerificationToken = CommonValidations.RandomToken();
        }
    }

    public static class ApiKey
    {
        public static readonly Validation Description = CommonValidations.DescriptiveName();
        public static readonly TimeSpan MinimumExpiryPeriod = TimeSpan.FromHours(1);
    }

    public static class OpenIdConnect
    {
        public static readonly Validation ClientId = CommonValidations.GuidN;
        public static readonly Validation ClientSecret = CommonValidations.RandomToken();
        public static readonly Validation Code = new(s => s == OpenIdConnectConstants.ResponseTypes.Code);
        public static readonly Validation CodeChallenge = new(@"^[a-zA-Z0-9\-._~]{43,128}$", 43, 128);
        public static readonly Validation CodeChallengeMethod =
            new(s => OpenIdConnectConstants.CodeChallengeMethods.AllMethods.Contains(s));
        public static readonly Validation CodeVerifier = new(@"^[A-Za-z0-9\-._~]{43,128}$", 43, 128);
        public static readonly Validation GrantType = new(s =>
            s is OpenIdConnectConstants.GrantTypes.AuthorizationCode or OpenIdConnectConstants.GrantTypes.RefreshToken);
        public static readonly Validation Nonce = new(@"^[a-zA-Z0-9\-._~]{1,500}$", 1, 500);
        public static readonly Validation RefreshToken = CommonValidations.RandomToken();
        public static readonly Validation RefreshTokenScope = new(s =>
        {
            if (s.HasNoValue())
            {
                return true; // Optional for refresh token
            }

            var requestedScopes = s.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            return requestedScopes.All(scope => OpenIdConnectConstants.Scopes.AllScopes.Contains(scope));
        });
        public static readonly Validation Scope = new(s =>
        {
            if (s.HasNoValue())
            {
                return false;
            }

            var scopes = s.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            return scopes.Contains(OpenIdConnectConstants.Scopes.OpenId);
        });
        public static readonly Validation State = new(@"^[a-zA-Z0-9\-._~]{1,500}$", 1, 500);
    }
}