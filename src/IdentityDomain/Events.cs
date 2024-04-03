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
            return new Created
            {
                RootId = id,
                UserId = userId,
                OccurredUtc = DateTime.UtcNow
            };
        }

        public static TokensChanged TokensChanged(Identifier id, Identifier userId, string accessToken,
            DateTime accessTokenExpiresOn,
            string refreshToken, DateTime refreshTokenExpiresOn)
        {
            return new TokensChanged
            {
                RootId = id,
                UserId = userId,
                AccessToken = accessToken,
                RefreshToken = refreshToken,
                AccessTokenExpiresOn = accessTokenExpiresOn,
                RefreshTokenExpiresOn = refreshTokenExpiresOn,
                OccurredUtc = DateTime.UtcNow
            };
        }

        public static TokensRefreshed TokensRefreshed(Identifier id, Identifier userId, string accessToken,
            DateTime accessTokenExpiresOn,
            string refreshToken, DateTime refreshTokenExpiresOn)
        {
            return new TokensRefreshed
            {
                RootId = id,
                UserId = userId,
                AccessToken = accessToken,
                RefreshToken = refreshToken,
                AccessTokenExpiresOn = accessTokenExpiresOn,
                RefreshTokenExpiresOn = refreshTokenExpiresOn,
                OccurredUtc = DateTime.UtcNow
            };
        }

        public static TokensRevoked TokensRevoked(Identifier id, Identifier userId)
        {
            return new TokensRevoked
            {
                RootId = id,
                UserId = userId,
                OccurredUtc = DateTime.UtcNow
            };
        }
    }

    public static class PasswordCredentials
    {
        public static AccountLocked AccountLocked(Identifier id)
        {
            return new AccountLocked
            {
                RootId = id,
                OccurredUtc = DateTime.UtcNow
            };
        }

        public static AccountUnlocked AccountUnlocked(Identifier id)
        {
            return new AccountUnlocked
            {
                RootId = id,
                OccurredUtc = DateTime.UtcNow
            };
        }

        public static Domain.Events.Shared.Identities.PasswordCredentials.Created Created(Identifier id,
            Identifier userId)
        {
            return new Domain.Events.Shared.Identities.PasswordCredentials.Created
            {
                RootId = id,
                UserId = userId,
                OccurredUtc = DateTime.UtcNow
            };
        }

        public static CredentialsChanged CredentialsChanged(Identifier id, string passwordHash)
        {
            return new CredentialsChanged
            {
                RootId = id,
                PasswordHash = passwordHash,
                OccurredUtc = DateTime.UtcNow
            };
        }

        public static PasswordResetCompleted PasswordResetCompleted(Identifier id, string token, string passwordHash)
        {
            return new PasswordResetCompleted
            {
                RootId = id,
                Token = token,
                PasswordHash = passwordHash,
                OccurredUtc = DateTime.UtcNow
            };
        }

        public static PasswordResetInitiated PasswordResetInitiated(Identifier id, string token)
        {
            return new PasswordResetInitiated
            {
                RootId = id,
                Token = token,
                OccurredUtc = DateTime.UtcNow
            };
        }

        public static PasswordVerified PasswordVerified(Identifier id, bool isVerified,
            bool auditAttempt)
        {
            return new PasswordVerified
            {
                RootId = id,
                IsVerified = isVerified,
                AuditAttempt = auditAttempt,
                OccurredUtc = DateTime.UtcNow
            };
        }

        public static RegistrationChanged RegistrationChanged(Identifier id, EmailAddress emailAddress,
            PersonDisplayName name)
        {
            return new RegistrationChanged
            {
                RootId = id,
                EmailAddress = emailAddress,
                Name = name,
                OccurredUtc = DateTime.UtcNow
            };
        }

        public static RegistrationVerificationCreated RegistrationVerificationCreated(Identifier id, string token)
        {
            return new RegistrationVerificationCreated
            {
                RootId = id,
                Token = token,
                OccurredUtc = DateTime.UtcNow
            };
        }

        public static RegistrationVerificationVerified RegistrationVerificationVerified(Identifier id)
        {
            return new RegistrationVerificationVerified
            {
                RootId = id,
                OccurredUtc = DateTime.UtcNow
            };
        }
    }

    public static class APIKeys
    {
        public static Domain.Events.Shared.Identities.APIKeys.Created Created(Identifier id, Identifier userId,
            string keyToken, string keyHash)
        {
            return new Domain.Events.Shared.Identities.APIKeys.Created
            {
                RootId = id,
                UserId = userId,
                KeyToken = keyToken,
                KeyHash = keyHash,
                OccurredUtc = DateTime.UtcNow
            };
        }

        public static KeyVerified KeyVerified(Identifier id, bool isVerified)
        {
            return new KeyVerified
            {
                RootId = id,
                IsVerified = isVerified,
                OccurredUtc = DateTime.UtcNow
            };
        }

        public static ParametersChanged ParametersChanged(Identifier id, string description,
            DateTime expiresOn)
        {
            return new ParametersChanged
            {
                RootId = id,
                Description = description,
                ExpiresOn = expiresOn,
                OccurredUtc = DateTime.UtcNow
            };
        }
    }

    public static class SSOUsers
    {
        public static Domain.Events.Shared.Identities.SSOUsers.Created Created(Identifier id, string providerName,
            Identifier userId)
        {
            return new Domain.Events.Shared.Identities.SSOUsers.Created
            {
                RootId = id,
                ProviderName = providerName,
                UserId = userId,
                OccurredUtc = DateTime.UtcNow
            };
        }

        public static TokensUpdated TokensUpdated(Identifier id, string tokens, EmailAddress emailAddress,
            PersonName name, Timezone timezone, Address address)
        {
            return new TokensUpdated
            {
                RootId = id,
                Tokens = tokens,
                EmailAddress = emailAddress,
                FirstName = name.FirstName,
                LastName = name.LastName.ValueOrDefault?.Text,
                Timezone = timezone.Code.ToString(),
                CountryCode = address.CountryCode.ToString(),
                OccurredUtc = DateTime.UtcNow
            };
        }
    }
}