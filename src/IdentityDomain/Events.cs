using Common;
using Domain.Common.ValueObjects;
using Domain.Events.Shared.Identities.APIKeys;
using Domain.Events.Shared.Identities.AuthTokens;
using Domain.Events.Shared.Identities.OAuth2.ClientConsents;
using Domain.Events.Shared.Identities.OAuth2.Clients;
using Domain.Events.Shared.Identities.OpenIdConnect.Authorizations;
using Domain.Events.Shared.Identities.PersonCredentials;
using Domain.Events.Shared.Identities.SSOUsers;
using Domain.Shared;
using Domain.Shared.Identities;
using Created = Domain.Events.Shared.Identities.AuthTokens.Created;
using Deleted = Domain.Events.Shared.Identities.APIKeys.Deleted;
using TokensChanged = Domain.Events.Shared.Identities.AuthTokens.TokensChanged;

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

        public static TokensChanged TokensChanged(Identifier id, Identifier userId, AuthToken accessToken,
            AuthToken refreshToken,
            Optional<AuthToken> idToken, string refreshTokenDigest)
        {
            return new TokensChanged(id)
            {
                UserId = userId,
                AccessToken = accessToken.EncryptedValue,
                RefreshToken = refreshToken.EncryptedValue,
                IdToken = idToken.ToNullable(tok => tok.EncryptedValue),
                AccessTokenExpiresOn = accessToken.ExpiresOn,
                RefreshTokenExpiresOn = refreshToken.ExpiresOn,
                IdTokenExpiresOn = idToken.ToNullable<AuthToken, DateTime>(tok => tok.ExpiresOn),
                RefreshTokenDigest = refreshTokenDigest
            };
        }

        public static TokensRefreshed TokensRefreshed(Identifier id, Identifier userId, AuthToken accessToken,
            AuthToken refreshToken, Optional<AuthToken> idToken, string refreshTokenDigest)
        {
            return new TokensRefreshed(id)
            {
                UserId = userId,
                AccessToken = accessToken.EncryptedValue,
                RefreshToken = refreshToken.EncryptedValue,
                IdToken = idToken.ToNullable(tok => tok.EncryptedValue),
                AccessTokenExpiresOn = accessToken.ExpiresOn,
                RefreshTokenExpiresOn = refreshToken.ExpiresOn,
                IdTokenExpiresOn = idToken.ToNullable<AuthToken, DateTime>(tok => tok.ExpiresOn),
                RefreshTokenDigest = refreshTokenDigest
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

    public static class ProviderAuthTokens
    {
        public static Domain.Events.Shared.Identities.ProviderAuthTokens.Created Created(Identifier id,
            Identifier userId, string providerName)
        {
            return new Domain.Events.Shared.Identities.ProviderAuthTokens.Created(id)
            {
                UserId = userId,
                ProviderName = providerName
            };
        }

        public static Domain.Events.Shared.Identities.ProviderAuthTokens.TokensChanged TokensChanged(Identifier id,
            IdentityDomain.AuthTokens tokens)
        {
            return new Domain.Events.Shared.Identities.ProviderAuthTokens.TokensChanged(id)
            {
                Tokens = tokens
                    .ToList()
                    .Select(tok => new Domain.Events.Shared.Identities.ProviderAuthTokens.AuthToken
                    {
                        Type = tok.Type.ToString(),
                        EncryptedValue = tok.EncryptedValue,
                        ExpiresOn = tok.ExpiresOn
                    }).ToList()
            };
        }
    }

    public static class PersonCredentials
    {
        public static AccountLocked AccountLocked(Identifier id)
        {
            return new AccountLocked(id);
        }

        public static AccountUnlocked AccountUnlocked(Identifier id)
        {
            return new AccountUnlocked(id);
        }

        public static Domain.Events.Shared.Identities.PersonCredentials.Created Created(Identifier id,
            Identifier userId, MfaOptions mfaOptions)
        {
            return new Domain.Events.Shared.Identities.PersonCredentials.Created(id)
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

        public static Expired Expired(Identifier id, Identifier userId)
        {
            return new Expired(id)
            {
                ExpiredOn = DateTime.UtcNow,
                UserId = userId
            };
        }

        public static KeyVerified KeyVerified(Identifier id, bool isVerified)
        {
            return new KeyVerified(id)
            {
                IsVerified = isVerified
            };
        }

        public static ParametersChanged ParametersChanged(Identifier id, string description,
            Optional<DateTime> expiresOn)
        {
            return new ParametersChanged(id)
            {
                Description = description,
                ExpiresOn = expiresOn.ToNullable()
            };
        }

        public static Revoked Revoked(Identifier id, Identifier userId)
        {
            return new Revoked(id)
            {
                RevokedOn = DateTime.UtcNow,
                UserId = userId
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

        public static DetailsChanged DetailsChanged(Identifier id, string providerUniqueId, EmailAddress emailAddress,
            PersonName name, Timezone timezone, Locale locale, Address address)
        {
            return new DetailsChanged(id)
            {
                ProviderUId = providerUniqueId,
                EmailAddress = emailAddress,
                FirstName = name.FirstName,
                LastName = name.LastName.ValueOrDefault?.Text,
                Timezone = timezone.Code.ToString(),
                Locale = locale.Code.ToString(),
                CountryCode = address.CountryCode.ToString()
            };
        }
    }

    public static class OAuth2
    {
        public static class Clients
        {
            public static Domain.Events.Shared.Identities.OAuth2.Clients.Created Created(Identifier id, Name name)
            {
                return new Domain.Events.Shared.Identities.OAuth2.Clients.Created(id)
                {
                    Name = name
                };
            }

            public static Domain.Events.Shared.Identities.OAuth2.Clients.Deleted Deleted(Identifier id,
                Identifier deletedById)
            {
                return new Domain.Events.Shared.Identities.OAuth2.Clients.Deleted(id, deletedById);
            }

            public static NameChanged NameChanged(Identifier id, Name name)
            {
                return new NameChanged(id)
                {
                    Name = name
                };
            }

            public static RedirectUriChanged RedirectUriChanged(Identifier id, string redirectUri)
            {
                return new RedirectUriChanged(id)
                {
                    RedirectUri = redirectUri
                };
            }

            public static SecretAdded SecretAdded(Identifier id, OAuth2ClientSecret secret,
                Optional<DateTime> expiresOn)
            {
                return new SecretAdded(id)
                {
                    SecretHash = secret.SecretHash,
                    FirstFour = secret.FirstFour,
                    ExpiresOn = expiresOn.ToNullable()
                };
            }
        }

        public static class ClientConsents
        {
            public static ConsentChanged ConsentChanged(Identifier id, bool isConsented, OAuth2Scopes scopes)
            {
                return new ConsentChanged(id)
                {
                    IsConsented = isConsented,
                    Scopes = scopes.Items
                };
            }

            public static Domain.Events.Shared.Identities.OAuth2.ClientConsents.Created Created(Identifier id,
                Identifier clientId, Identifier userId)
            {
                return new Domain.Events.Shared.Identities.OAuth2.ClientConsents.Created(id)
                {
                    ClientId = clientId,
                    UserId = userId,
                    IsConsented = false,
                    Scopes = []
                };
            }
        }
    }

    public static class OpenIdConnect
    {
        public static CodeAuthorized CodeAuthorized(Identifier id,
            Identifier clientId, Identifier userId, OAuth2Scopes scopes, string redirectUri, Optional<string> nonce,
            Optional<string> codeChallenge, Optional<OpenIdConnectCodeChallengeMethod> codeChallengeMethod, string code,
            DateTime expiresAt)
        {
            return new CodeAuthorized(id)
            {
                ClientId = clientId,
                UserId = userId,
                Scopes = scopes.Items,
                RedirectUri = redirectUri,
                Nonce = nonce,
                CodeChallenge = codeChallenge,
                CodeChallengeMethod = codeChallengeMethod,
                Code = code,
                ExpiresAt = expiresAt,
                AuthorizedAt = DateTime.UtcNow
            };
        }

        public static CodeExchanged CodeExchanged(Identifier id, OAuth2TokenMemento accessToken,
            OAuth2TokenMemento refreshToken)
        {
            return new CodeExchanged(id)
            {
                ExchangedAt = DateTime.UtcNow,
                AccessTokenDigest = accessToken.DigestValue,
                AccessTokenExpiresOn = accessToken.ExpiresOn,
                RefreshTokenDigest = refreshToken.DigestValue,
                RefreshTokenExpiresOn = refreshToken.ExpiresOn
            };
        }

        public static Domain.Events.Shared.Identities.OpenIdConnect.Authorizations.Created Created(Identifier id,
            Identifier clientId, Identifier userId)
        {
            return new Domain.Events.Shared.Identities.OpenIdConnect.Authorizations.Created(id)
            {
                ClientId = clientId,
                UserId = userId
            };
        }

        public static TokenRefreshed TokenRefreshed(Identifier id, OAuth2TokenMemento accessToken,
            OAuth2TokenMemento refreshToken, OAuth2Scopes refreshedScopes)
        {
            return new TokenRefreshed(id)
            {
                RefreshedAt = DateTime.UtcNow,
                AccessTokenDigest = accessToken.DigestValue,
                AccessTokenExpiresOn = accessToken.ExpiresOn,
                RefreshTokenDigest = refreshToken.DigestValue,
                RefreshTokenExpiresOn = refreshToken.ExpiresOn,
                Scopes = refreshedScopes.Items
            };
        }
    }
}