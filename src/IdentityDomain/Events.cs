using Common;
using Domain.Common.ValueObjects;
using Domain.Events.Shared.Identities.APIKeys;
using Domain.Events.Shared.Identities.AuthTokens;
using Domain.Events.Shared.Identities.PasswordCredentials;
using Domain.Events.Shared.Identities.SSOUsers;
using Domain.Shared;
using Domain.Shared.Identities;
using Created = Domain.Events.Shared.Identities.AuthTokens.Created;
using TokensChanged = Domain.Events.Shared.Identities.SSOUsers.TokensChanged;

namespace IdentityDomain;

public static class Events
{
    public static class AuthTokens
    {
        public static Created Created(Identifier id, Identifier userId)
        {
            return new Created(id)
            {
                UserId = userId
            };
        }

        public static Domain.Events.Shared.Identities.AuthTokens.TokensChanged TokensChanged(Identifier id,
            Identifier userId, string accessToken,
            DateTime accessTokenExpiresOn,
            string refreshToken, DateTime refreshTokenExpiresOn)
        {
            return new Domain.Events.Shared.Identities.AuthTokens.TokensChanged(id)
            {
                UserId = userId,
                AccessToken = accessToken,
                RefreshToken = refreshToken,
                AccessTokenExpiresOn = accessTokenExpiresOn,
                RefreshTokenExpiresOn = refreshTokenExpiresOn
            };
        }

        public static TokensRefreshed TokensRefreshed(Identifier id, Identifier userId, string accessToken,
            DateTime accessTokenExpiresOn,
            string refreshToken, DateTime refreshTokenExpiresOn)
        {
            return new TokensRefreshed(id)
            {
                UserId = userId,
                AccessToken = accessToken,
                RefreshToken = refreshToken,
                AccessTokenExpiresOn = accessTokenExpiresOn,
                RefreshTokenExpiresOn = refreshTokenExpiresOn
            };
        }

        public static TokensRevoked TokensRevoked(Identifier id, Identifier userId)
        {
            return new TokensRevoked(id)
            {
                UserId = userId
            };
        }
    }

    public static class PasswordCredentials
    {
        public static AccountLocked AccountLocked(Identifier id)
        {
            return new AccountLocked(id);
        }

        public static AccountUnlocked AccountUnlocked(Identifier id)
        {
            return new AccountUnlocked(id);
        }

        public static Domain.Events.Shared.Identities.PasswordCredentials.Created Created(Identifier id,
            Identifier userId, MfaOptions mfaOptions)
        {
            return new Domain.Events.Shared.Identities.PasswordCredentials.Created(id)
            {
                UserId = userId,
                IsMfaEnabled = mfaOptions.IsEnabled,
                MfaCanBeDisabled = mfaOptions.CanBeDisabled
            };
        }

        public static CredentialsChanged CredentialsChanged(Identifier id, string passwordHash)
        {
            return new CredentialsChanged(id)
            {
                PasswordHash = passwordHash
            };
        }

        public static MfaAuthenticationInitiated MfaAuthenticationInitiated(Identifier id,
            Identifier userId, MfaOptions mfaOptions)
        {
            return new MfaAuthenticationInitiated(id)
            {
                UserId = userId,
                AuthenticationToken = mfaOptions.AuthenticationToken.ValueOrDefault!,
                AuthenticationExpiresAt = mfaOptions.AuthenticationTokenExpiresAt.ValueOrDefault
            };
        }

        public static MfaAuthenticatorAdded MfaAuthenticatorAdded(Identifier id,
            Identifier userId, MfaAuthenticatorType type, bool isActive)
        {
            return new MfaAuthenticatorAdded(id)
            {
                UserId = userId,
                Type = type,
                AuthenticatorId = null,
                IsActive = isActive
            };
        }

        public static MfaAuthenticatorAssociated MfaAuthenticatorAssociated(Identifier id,
            MfaAuthenticator authenticator, Optional<string> oobCode, Optional<string> barCodeUri,
            Optional<string> secret, Optional<string> oobChannel)
        {
            return new MfaAuthenticatorAssociated(id)
            {
                UserId = authenticator.UserId.Value,
                AuthenticatorId = authenticator.Id,
                Type = authenticator.Type,
                OobChannelValue = oobChannel,
                OobCode = oobCode,
                BarCodeUri = barCodeUri,
                Secret = secret
            };
        }

        public static MfaAuthenticatorChallenged MfaAuthenticatorChallenged(Identifier id,
            MfaAuthenticator authenticator, Optional<string> oobCode, Optional<string> barCodeUri,
            Optional<string> secret, Optional<string> oobChannel)
        {
            return new MfaAuthenticatorChallenged(id)
            {
                UserId = authenticator.UserId.Value,
                AuthenticatorId = authenticator.Id,
                Type = authenticator.Type,
                OobChannelValue = oobChannel,
                OobCode = oobCode,
                BarCodeUri = barCodeUri,
                Secret = secret
            };
        }

        public static MfaAuthenticatorConfirmed MfaAuthenticatorConfirmed(Identifier id,
            MfaAuthenticator authenticator, Optional<string> oobCode, Optional<string> confirmationCode,
            Optional<string> verifiedState)
        {
            return new MfaAuthenticatorConfirmed(id)
            {
                UserId = authenticator.UserId.Value,
                AuthenticatorId = authenticator.Id,
                Type = authenticator.Type,
                IsActive = true,
                OobCode = oobCode,
                ConfirmationCode = confirmationCode,
                VerifiedState = verifiedState
            };
        }

        public static MfaAuthenticatorRemoved MfaAuthenticatorRemoved(Identifier id,
            Identifier userId, MfaAuthenticator authenticator)
        {
            return new MfaAuthenticatorRemoved(id)
            {
                UserId = authenticator.UserId.Value,
                AuthenticatorId = authenticator.Id,
                Type = authenticator.Type
            };
        }

        public static MfaAuthenticatorVerified MfaAuthenticatorVerified(Identifier id,
            MfaAuthenticator authenticator, Optional<string> oobCode, Optional<string> confirmationCode,
            Optional<string> verifiedState)
        {
            return new MfaAuthenticatorVerified(id)
            {
                UserId = authenticator.UserId.Value,
                AuthenticatorId = authenticator.Id,
                Type = authenticator.Type,
                OobCode = oobCode,
                ConfirmationCode = confirmationCode,
                VerifiedState = verifiedState
            };
        }

        public static MfaOptionsChanged MfaOptionsChanged(Identifier id,
            Identifier userId, MfaOptions mfaOptions)
        {
            return new MfaOptionsChanged(id)
            {
                UserId = userId,
                IsEnabled = mfaOptions.IsEnabled,
                CanBeDisabled = mfaOptions.CanBeDisabled
            };
        }

        public static MfaStateReset MfaStateReset(Identifier id,
            Identifier userId, MfaOptions mfaOptions)
        {
            return new MfaStateReset(id)
            {
                UserId = userId,
                IsEnabled = mfaOptions.IsEnabled,
                CanBeDisabled = mfaOptions.CanBeDisabled
            };
        }

        public static PasswordResetCompleted PasswordResetCompleted(Identifier id, string token, string passwordHash)
        {
            return new PasswordResetCompleted(id)
            {
                Token = token,
                PasswordHash = passwordHash
            };
        }

        public static PasswordResetInitiated PasswordResetInitiated(Identifier id, string token)
        {
            return new PasswordResetInitiated(id)
            {
                Token = token
            };
        }

        public static PasswordVerified PasswordVerified(Identifier id, bool isVerified,
            bool auditAttempt)
        {
            return new PasswordVerified(id)
            {
                IsVerified = isVerified,
                AuditAttempt = auditAttempt
            };
        }

        public static RegistrationChanged RegistrationChanged(Identifier id, EmailAddress emailAddress,
            PersonDisplayName name)
        {
            return new RegistrationChanged(id)
            {
                EmailAddress = emailAddress,
                Name = name
            };
        }

        public static RegistrationVerificationCreated RegistrationVerificationCreated(Identifier id, string token)
        {
            return new RegistrationVerificationCreated(id)
            {
                Token = token
            };
        }

        public static RegistrationVerificationVerified RegistrationVerificationVerified(Identifier id)
        {
            return new RegistrationVerificationVerified(id);
        }
    }

    public static class APIKeys
    {
        public static Domain.Events.Shared.Identities.APIKeys.Created Created(Identifier id, Identifier userId,
            string keyToken, string keyHash)
        {
            return new Domain.Events.Shared.Identities.APIKeys.Created(id)
            {
                UserId = userId,
                KeyToken = keyToken,
                KeyHash = keyHash
            };
        }

        public static Deleted Deleted(Identifier id, Identifier deletedById)
        {
            return new Deleted(id, deletedById);
        }

        public static KeyVerified KeyVerified(Identifier id, bool isVerified)
        {
            return new KeyVerified(id)
            {
                IsVerified = isVerified
            };
        }

        public static ParametersChanged ParametersChanged(Identifier id, string description,
            DateTime expiresOn)
        {
            return new ParametersChanged(id)
            {
                Description = description,
                ExpiresOn = expiresOn
            };
        }
    }

    public static class SSOUsers
    {
        public static Domain.Events.Shared.Identities.SSOUsers.Created Created(Identifier id, string providerName,
            Identifier userId)
        {
            return new Domain.Events.Shared.Identities.SSOUsers.Created(id)
            {
                ProviderName = providerName,
                UserId = userId
            };
        }

        public static DetailsAdded DetailsAdded(Identifier id, EmailAddress emailAddress,
            PersonName name, Timezone timezone, Address address)
        {
            return new DetailsAdded(id)
            {
                EmailAddress = emailAddress,
                FirstName = name.FirstName,
                LastName = name.LastName.ValueOrDefault?.Text,
                Timezone = timezone.Code.ToString(),
                CountryCode = address.CountryCode.ToString()
            };
        }

        public static TokensChanged TokensChanged(Identifier id, SSOAuthTokens tokens)
        {
            return new TokensChanged(id)
            {
                Tokens = tokens
                    .ToList()
                    .Select(tok => new SSOToken
                    {
                        Type = tok.Type.ToString(),
                        EncryptedValue = tok.EncryptedValue,
                        ExpiresOn = tok.ExpiresOn
                    }).ToList()
            };
        }
    }
}