using Domain.Common.ValueObjects;
using Domain.Interfaces.Entities;
using Domain.Shared;

namespace IdentityDomain;

public static class Events
{
    public static class PasswordCredentials
    {
        public sealed class Created : IDomainEvent
        {
            public static Created Create(Identifier id, Identifier userId)
            {
                return new Created
                {
                    RootId = id,
                    UserId = userId,
                    OccurredUtc = DateTime.UtcNow
                };
            }

            public required string UserId { get; set; }

            public required string RootId { get; set; }

            public required DateTime OccurredUtc { get; set; }
        }

        public sealed class CredentialsChanged : IDomainEvent
        {
            public static CredentialsChanged Create(Identifier id, string passwordHash)
            {
                return new CredentialsChanged
                {
                    RootId = id,
                    PasswordHash = passwordHash,
                    OccurredUtc = DateTime.UtcNow
                };
            }

            public required string PasswordHash { get; set; }

            public required string RootId { get; set; }

            public required DateTime OccurredUtc { get; set; }
        }

        public sealed class RegistrationChanged : IDomainEvent
        {
            public static RegistrationChanged Create(Identifier id, EmailAddress emailAddress, PersonDisplayName name)
            {
                return new RegistrationChanged
                {
                    RootId = id,
                    EmailAddress = emailAddress,
                    Name = name,
                    OccurredUtc = DateTime.UtcNow
                };
            }

            public required string EmailAddress { get; set; }

            public required string Name { get; set; }

            public required string RootId { get; set; }

            public required DateTime OccurredUtc { get; set; }
        }

        public sealed class PasswordVerified : IDomainEvent
        {
            public static PasswordVerified Create(Identifier id, bool isVerified,
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

            public required bool AuditAttempt { get; set; }

            public required bool IsVerified { get; set; }

            public required string RootId { get; set; }

            public required DateTime OccurredUtc { get; set; }
        }

        public sealed class AccountLocked : IDomainEvent
        {
            public static AccountLocked Create(Identifier id)
            {
                return new AccountLocked
                {
                    RootId = id,
                    OccurredUtc = DateTime.UtcNow
                };
            }

            public required string RootId { get; set; }

            public required DateTime OccurredUtc { get; set; }
        }

        public sealed class AccountUnlocked : IDomainEvent
        {
            public static AccountUnlocked Create(Identifier id)
            {
                return new AccountUnlocked
                {
                    RootId = id,
                    OccurredUtc = DateTime.UtcNow
                };
            }

            public required string RootId { get; set; }

            public required DateTime OccurredUtc { get; set; }
        }

        public sealed class RegistrationVerificationCreated : IDomainEvent
        {
            public static RegistrationVerificationCreated Create(Identifier id, string token)
            {
                return new RegistrationVerificationCreated
                {
                    RootId = id,
                    Token = token,
                    OccurredUtc = DateTime.UtcNow
                };
            }

            public required string Token { get; set; }

            public required string RootId { get; set; }

            public required DateTime OccurredUtc { get; set; }
        }

        public sealed class RegistrationVerificationVerified : IDomainEvent
        {
            public static RegistrationVerificationVerified Create(Identifier id)
            {
                return new RegistrationVerificationVerified
                {
                    RootId = id,
                    OccurredUtc = DateTime.UtcNow
                };
            }

            public required string RootId { get; set; }

            public required DateTime OccurredUtc { get; set; }
        }

        public sealed class PasswordResetInitiated : IDomainEvent
        {
            public static PasswordResetInitiated Create(Identifier id, string token)
            {
                return new PasswordResetInitiated
                {
                    RootId = id,
                    Token = token,
                    OccurredUtc = DateTime.UtcNow
                };
            }

            public required string Token { get; set; }

            public required string RootId { get; set; }

            public required DateTime OccurredUtc { get; set; }
        }

        public sealed class PasswordResetCompleted : IDomainEvent
        {
            public static PasswordResetCompleted Create(Identifier id, string token, string passwordHash)
            {
                return new PasswordResetCompleted
                {
                    RootId = id,
                    Token = token,
                    PasswordHash = passwordHash,
                    OccurredUtc = DateTime.UtcNow
                };
            }

            public required string PasswordHash { get; set; }

            public required string Token { get; set; }

            public required string RootId { get; set; }

            public required DateTime OccurredUtc { get; set; }
        }
    }

    public static class AuthTokens
    {
        public sealed class Created : IDomainEvent
        {
            public static Created Create(Identifier id, Identifier userId)
            {
                return new Created
                {
                    RootId = id,
                    UserId = userId,
                    OccurredUtc = DateTime.UtcNow
                };
            }

            public required string UserId { get; set; }

            public required string RootId { get; set; }

            public required DateTime OccurredUtc { get; set; }
        }

        public sealed class TokensChanged : IDomainEvent
        {
            public static TokensChanged Create(Identifier id, Identifier userId, string accessToken,
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

            public required string AccessToken { get; set; }

            public required DateTime AccessTokenExpiresOn { get; set; }

            public required string RefreshToken { get; set; }

            public required DateTime RefreshTokenExpiresOn { get; set; }

            public required string UserId { get; set; }

            public required string RootId { get; set; }

            public required DateTime OccurredUtc { get; set; }
        }

        public sealed class TokensRefreshed : IDomainEvent
        {
            public static TokensRefreshed Create(Identifier id, Identifier userId, string accessToken,
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

            public required string AccessToken { get; set; }

            public required DateTime AccessTokenExpiresOn { get; set; }

            public required string RefreshToken { get; set; }

            public required DateTime RefreshTokenExpiresOn { get; set; }

            public required string UserId { get; set; }

            public required string RootId { get; set; }

            public required DateTime OccurredUtc { get; set; }
        }

        public sealed class TokensRevoked : IDomainEvent
        {
            public static TokensRevoked Create(Identifier id, Identifier userId)
            {
                return new TokensRevoked
                {
                    RootId = id,
                    UserId = userId,
                    OccurredUtc = DateTime.UtcNow
                };
            }

            public required string UserId { get; set; }

            public required string RootId { get; set; }

            public required DateTime OccurredUtc { get; set; }
        }
    }

    public static class APIKeys
    {
        public sealed class Created : IDomainEvent
        {
            public static Created Create(Identifier id, Identifier userId, string keyToken, string keyHash)
            {
                return new Created
                {
                    RootId = id,
                    UserId = userId,
                    KeyToken = keyToken,
                    KeyHash = keyHash,
                    OccurredUtc = DateTime.UtcNow
                };
            }

            public required string KeyHash { get; set; }

            public required string KeyToken { get; set; }

            public required string UserId { get; set; }

            public required string RootId { get; set; }

            public required DateTime OccurredUtc { get; set; }
        }

        public sealed class ParametersChanged : IDomainEvent
        {
            public static ParametersChanged Create(Identifier id, string description,
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

            public required string Description { get; set; }

            public required DateTime ExpiresOn { get; set; }

            public required string RootId { get; set; }

            public required DateTime OccurredUtc { get; set; }
        }

        public sealed class KeyVerified : IDomainEvent
        {
            public static KeyVerified Create(Identifier id, bool isVerified)
            {
                return new KeyVerified
                {
                    RootId = id,
                    IsVerified = isVerified,
                    OccurredUtc = DateTime.UtcNow
                };
            }

            public required bool IsVerified { get; set; }

            public required string RootId { get; set; }

            public required DateTime OccurredUtc { get; set; }
        }
    }

    public static class SSOUsers
    {
        public sealed class Created : IDomainEvent
        {
            public static Created Create(Identifier id, string providerName, Identifier userId)
            {
                return new Created
                {
                    RootId = id,
                    ProviderName = providerName,
                    UserId = userId,
                    OccurredUtc = DateTime.UtcNow
                };
            }

            public required string ProviderName { get; set; }

            public required string UserId { get; set; }

            public required string RootId { get; set; }

            public required DateTime OccurredUtc { get; set; }
        }

        public sealed class TokensUpdated : IDomainEvent
        {
            public static TokensUpdated Create(Identifier id, string tokens, EmailAddress emailAddress,
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

            public required string CountryCode { get; set; }

            public required string EmailAddress { get; set; }

            public required string FirstName { get; set; }

            public string? LastName { get; set; }

            public required string Timezone { get; set; }

            public required string Tokens { get; set; }

            public required string RootId { get; set; }

            public required DateTime OccurredUtc { get; set; }
        }
    }
}