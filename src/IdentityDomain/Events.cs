using Common;
using Domain.Common.ValueObjects;
using Domain.Interfaces.Entities;
using Domain.Shared;

namespace IdentityDomain;

public static class Events
{
    public static class PasswordCredentials
    {
        public class Created : IDomainEvent
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

            public DateTime OccurredUtc { get; set; }
        }

        public class CredentialsChanged : IDomainEvent
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

            public DateTime OccurredUtc { get; set; }
        }

        public class RegistrationChanged : IDomainEvent
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

            public DateTime OccurredUtc { get; set; }
        }

        public class PasswordVerified : IDomainEvent
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

            public bool AuditAttempt { get; set; }

            public bool IsVerified { get; set; }

            public required string RootId { get; set; }

            public DateTime OccurredUtc { get; set; }
        }

        public class AccountLocked : IDomainEvent
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

            public DateTime OccurredUtc { get; set; }
        }

        public class AccountUnlocked : IDomainEvent
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

            public DateTime OccurredUtc { get; set; }
        }

        public class RegistrationVerificationCreated : IDomainEvent
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

            public DateTime OccurredUtc { get; set; }
        }

        public class RegistrationVerificationVerified : IDomainEvent
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

            public DateTime OccurredUtc { get; set; }
        }

        public class PasswordResetInitiated : IDomainEvent
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

        public class PasswordResetCompleted : IDomainEvent
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

            public DateTime OccurredUtc { get; set; }
        }
    }

    public static class AuthTokens
    {
        public class Created : IDomainEvent
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

            public DateTime OccurredUtc { get; set; }
        }

        public class TokensChanged : IDomainEvent
        {
            public static TokensChanged Create(Identifier id, Identifier userId, string accessToken,
                string refreshToken, DateTime expiresOn)
            {
                return new TokensChanged
                {
                    RootId = id,
                    UserId = userId,
                    AccessToken = accessToken,
                    RefreshToken = refreshToken,
                    ExpiresOn = expiresOn,
                    OccurredUtc = DateTime.UtcNow
                };
            }

            public required string AccessToken { get; set; }

            public required DateTime ExpiresOn { get; set; }

            public required string RefreshToken { get; set; }

            public required string UserId { get; set; }

            public required string RootId { get; set; }

            public DateTime OccurredUtc { get; set; }
        }

        public class TokensRefreshed : IDomainEvent
        {
            public static TokensRefreshed Create(Identifier id, Identifier userId, string accessToken,
                string refreshToken, DateTime expiresOn)
            {
                return new TokensRefreshed
                {
                    RootId = id,
                    UserId = userId,
                    AccessToken = accessToken,
                    RefreshToken = refreshToken,
                    ExpiresOn = expiresOn,
                    OccurredUtc = DateTime.UtcNow
                };
            }

            public required string AccessToken { get; set; }

            public required DateTime ExpiresOn { get; set; }

            public required string RefreshToken { get; set; }

            public required string UserId { get; set; }

            public required string RootId { get; set; }

            public DateTime OccurredUtc { get; set; }
        }

        public class TokensRevoked : IDomainEvent
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

            public DateTime OccurredUtc { get; set; }
        }
    }

    public static class APIKeys
    {
        public class Created : IDomainEvent
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

            public DateTime OccurredUtc { get; set; }
        }

        public class ParametersChanged : IDomainEvent
        {
            public static ParametersChanged Create(Identifier id, string description,
                Optional<DateTime?> expiresOn)
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

            public DateTime? ExpiresOn { get; set; }

            public required string RootId { get; set; }

            public DateTime OccurredUtc { get; set; }
        }

        public class KeyVerified : IDomainEvent
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

            public bool IsVerified { get; set; }

            public required string RootId { get; set; }

            public DateTime OccurredUtc { get; set; }
        }
    }
}