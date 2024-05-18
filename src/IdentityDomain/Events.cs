using Domain.Common.ValueObjects;
using Domain.Events.Shared.Identities.APIKeys;
using Domain.Events.Shared.Identities.AuthTokens;
using Domain.Events.Shared.Identities.PasswordCredentials;
using Domain.Events.Shared.Identities.SSOUsers;
using Domain.Shared;
using Created = Domain.Events.Shared.Identities.AuthTokens.Created;

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

        public static TokensChanged TokensChanged(Identifier id, Identifier userId, string accessToken,
            DateTime accessTokenExpiresOn,
            string refreshToken, DateTime refreshTokenExpiresOn)
        {
            return new TokensChanged(id)
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
            Identifier userId)
        {
            return new Domain.Events.Shared.Identities.PasswordCredentials.Created(id)
            {
                UserId = userId
            };
        }

        public static CredentialsChanged CredentialsChanged(Identifier id, string passwordHash)
        {
            return new CredentialsChanged(id)
            {
                PasswordHash = passwordHash
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

        public static TokensUpdated TokensUpdated(Identifier id, string tokens, EmailAddress emailAddress,
            PersonName name, Timezone timezone, Address address)
        {
            return new TokensUpdated(id)
            {
                Tokens = tokens,
                EmailAddress = emailAddress,
                FirstName = name.FirstName,
                LastName = name.LastName.ValueOrDefault?.Text,
                Timezone = timezone.Code.ToString(),
                CountryCode = address.CountryCode.ToString()
            };
        }
    }
}