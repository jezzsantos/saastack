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
        private static readonly char[] Delimiters = [' ', ';', ','];
        public static readonly Validation Scope = new(scope =>
        {
            if (scope.HasNoValue())
            {
                return false;
            }

            var scopes = scope.Split(Delimiters, StringSplitOptions.RemoveEmptyEntries);
            return scopes.Contains(OpenIdConnectConstants.Scopes.OpenId);
        });
    }

    public static class OAuth2
    {
        public static readonly Validation ClientName = Domain.Shared.Validations.Names.Name;
        public static readonly Validation ClientSecret = CommonValidations.RandomToken();
        public static readonly Validation Code = new(code => code == OAuth2Constants.ResponseTypes.Code);
        public static readonly Validation CodeChallenge = new(@"^[a-zA-Z0-9\-._~]{43,128}$", 43, 128);
        public static readonly Validation CodeChallengeMethod =
            new(method => OAuth2Constants.CodeChallengeMethods.AllMethods.Contains(method));
        public static readonly Validation CodeVerifier = new(@"^[A-Za-z0-9\-._~]{43,128}$", 43, 128);
        public static readonly Validation GrantType = new(grant =>
            grant is OAuth2Constants.GrantTypes.AuthorizationCode or OAuth2Constants.GrantTypes.RefreshToken);
        public static readonly Validation Nonce = new(@"^[a-zA-Z0-9\-._~]{1,500}$", 1, 500);
        public static readonly Validation RefreshToken = CommonValidations.RandomToken();
        public static readonly Validation RefreshTokenScope = new(scope =>
        {
            if (scope.HasNoValue())
            {
                return true; // Optional for refresh token
            }

            var scopes = scope.Split(Delimiters, StringSplitOptions.RemoveEmptyEntries);
            return scopes.All(s => OpenIdConnectConstants.Scopes.AllScopes.Contains(s));
        });
        private static readonly char[] Delimiters = [' ', ';', ','];
        public static readonly Validation Scope = new(scope =>
        {
            if (scope.HasNoValue())
            {
                return false;
            }

            var scopes = scope.Split(Delimiters, StringSplitOptions.RemoveEmptyEntries);
            return scopes.All(s => OpenIdConnectConstants.Scopes.AllScopes.Contains(s));
        });
        public static readonly Validation State = new(@"^[a-zA-Z0-9\-._~]{1,500}$", 1, 500);
    }
}