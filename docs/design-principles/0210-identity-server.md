# Identity Server

## Overview

An Identity Server is a centralized authentication and authorization service that manages user identities, credentials, and access tokens across a distributed system. It serves as the single source of truth for user authentication and provides standardized protocols for secure access to protected resources.

The Identity Server in SaaStack provides:

1. **Authentication Services** - Verifying user credentials through multiple mechanisms (e.g., password, API keys, SSO)
2. **Authorization Services** - Managing user permissions and access control through claims in JWT tokens
3. **Token Management** - Issuing, validating, and refreshing access and refresh tokens
4. **OpenID Connect/OAuth2 Support** - Standards-compliant identity protocols for modern applications
5. **Single Sign-On (SSO)** - Integration with third-party identity providers (e.g., Microsoft, Google, Facebook, etc.)
6. **Multi-Factor Authentication (MFA)** - Enhanced security through additional authentication factors
7. **API Key Management** - Machine-to-machine authentication for automated systems
8. **User Credential Management** - Password policies, registration, and account recovery

The notion of an 'Identity Server' is that a single server can provide all this functionality, but that server can be replaced by integrating with another 3rd party, and removed from this codebase as a whole unit, or in parts. By integrating with 3rd party services such as Auth0, Okta, IdentityServer, or Keycloak.

## Design Principles

1. **Provider Abstraction** - We implement a provider pattern that allows the identity server implementation to be swapped out without changing consuming code. The `IIdentityServerProvider` interface abstracts all identity services.

2. **Standards Compliance** - Support a industry-standard protocols (OpenID Connect, OAuth2) to a limited degree, to ensure interoperability with existing systems and third-party integrations.

3. **Extensibility Over Completeness** - We provide a functional native implementation to get developers started quickly, but these implementations are not fully featured nor recommended for systems of scale. The architecture is designed for easy integration with enterprise-grade solutions like Auth0, Okta, IdentityServer, or Keycloak.

4. **Security by Design** - All authentication mechanisms implement security best practices including token expiration, secure password hashing, PKCE support, and protection against common attacks.

5. **Multi-Modal Authentication** - Support for multiple authentication methods (credentials, API keys, SSO, HMAC) to accommodate different client types and security requirements.

6. **Backward Compatibility** - All public APIs maintain backward compatibility when extending or replacing identity providers.

7. **Separation of Concerns** - Identity services are cleanly separated into distinct domains: credentials, tokens, OAuth2 clients, SSO providers, and API keys.

## Implementation

The Identity Server is implemented as a modular subdomain within SaaStack, providing both native implementations and extensibility points for third-party integrations.

### Architecture Overview

The Identity subdomain follows the established SaaStack patterns with clear separation between Domain, Application, and Infrastructure layers:

```
IdentityDomain/          # Domain models and business rules
IdentityApplication/     # Application services and use cases
IdentityInfrastructure/  # API controllers and infrastructure adapters
```

### Core Components

#### Identity Server Provider

The `IIdentityServerProvider` serves as the main abstraction point, providing access to all identity services:

<augment_code_snippet path="src/Application.Services.Shared/IIdentityServerProvider.cs" mode="EXCERPT">
````csharp
public interface IIdentityServerProvider
{
    public IIdentityServerApiKeyService ApiKeyService { get; }
    public IIdentityServerCredentialsService CredentialsService { get; }
    public IIdentityServerOAuth2ClientService OAuth2ClientService { get; }
    public IIdentityServerOpenIdConnectService OpenIdConnectService { get; }
    public IIdentityServerSingleSignOnService SingleSignOnService { get; }
    string ProviderName { get; }
}
````
</augment_code_snippet>

The native implementation (`NativeIdentityServerProvider`) provides a complete identity server that manages all identity state internally:

<augment_code_snippet path="src/IdentityInfrastructure/ApplicationServices/NativeIdentityServerProvider.cs" mode="EXCERPT">
````csharp
/// <summary>
///     Provides a native identity server provider that manages OIDC, Person Credentials, API Keys and Single Sign On
///     by self-persisting identity state
/// </summary>
public class NativeIdentityServerProvider : IIdentityServerProvider
{
    public string ProviderName => Constants.ProviderName;
    // ... service implementations
}
````
</augment_code_snippet>

#### Domain Aggregates

The Identity subdomain manages several key aggregates:

- **PersonCredentialRoot** - User credentials, passwords, and MFA settings
- **AuthTokensRoot** - Access and refresh token management
- **APIKeyRoot** - API key generation and validation
- **OAuth2ClientRoot** - OAuth2 client registration and management
- **SSOUserRoot** - Single sign-on user mappings
- **ProviderAuthTokensRoot** - Third-party provider token storage
- **MfaAuthenticator** - Multi-factor authentication devices

#### Application Services

Each identity concern is handled by a dedicated application service:

- **IdentityApplication** - Main identity operations and token validation
- **PersonCredentialsApplication** - User credential management
- **AuthTokensApplication** - Token lifecycle management
- **APIKeysApplication** - API key operations
- **SingleSignOnApplication** - SSO authentication flows
- **OpenIdConnectApplication** - OIDC protocol implementation
- **OAuth2ClientApplication** - OAuth2 client management

### Supported Standards

#### OpenID Connect (OIDC)

The implementation supports core OpenID Connect flows:

**Authorization Code Flow** - Standard web application authentication:
- Authorization endpoint: `/oauth2/authorize`
- Token endpoint: `/oauth2/token`
- UserInfo endpoint: `/oauth2/userinfo`

**Discovery Document** - Standard OIDC discovery:
- Well-known endpoint: `/.well-known/openid-configuration`
- JWKS endpoint: `/.well-known/jwks.json`

**Supported Grant Types**:

<augment_code_snippet path="src/IdentityDomain/OpenIdConnectConstants.cs" mode="EXCERPT">
````csharp
public static class GrantTypes
{
    public const string AuthorizationCode = "authorization_code";
    public const string RefreshToken = "refresh_token";
    // Additional grant types...
}
````
</augment_code_snippet>

**Supported Scopes**:

<augment_code_snippet path="src/IdentityDomain/OpenIdConnectConstants.cs" mode="EXCERPT">
````csharp
public static class Scopes
{
    public const string OpenId = "openid";
    public const string Profile = "profile";
    public const string Email = "email";
    public const string OfflineAccess = "offline_access";
    // Additional scopes...
}
````
</augment_code_snippet>

#### OAuth2 Features

- **PKCE Support** - Proof Key for Code Exchange for enhanced security
- **Client Credentials Flow** - Machine-to-machine authentication
- **Refresh Token Flow** - Token renewal without re-authentication
- **Scope-based Authorization** - Fine-grained permission control

#### JWT Token Management

All tokens are issued as signed JWTs with:
- Short-lived access tokens (15 minutes default)
- Long-lived refresh tokens (7 days default)
- Configurable expiration times
- Secure signing with HMAC-SHA512

### Authentication Methods

#### Password Credentials

Traditional username/password authentication with:
- Strong password policies (8-200 characters, complexity requirements)
- BCrypt password hashing with 4,096 iterations
- Email confirmation workflows
- Password reset capabilities
- Account lockout protection

#### Multi-Factor Authentication (MFA)

Comprehensive MFA support including:
- **TOTP Authenticator Apps** - Time-based one-time passwords
- **SMS Out-of-Band** - SMS-delivered verification codes
- **Email Out-of-Band** - Email-delivered verification codes
- **Recovery Codes** - 16 backup codes for account recovery

#### API Keys

Machine-to-machine authentication via:
- Configurable expiration dates
- Basic Authentication or query parameter delivery
- Secure key generation and hashing
- Per-user and per-machine key management

#### Single Sign-On (SSO)

Integration with third-party identity providers:
- OAuth2 Authorization Code Flow
- Provider abstraction for multiple SSO providers
- Token storage and refresh capabilities
- User account linking and provisioning

### Provider Abstraction Pattern

The identity system uses a comprehensive provider pattern to enable third-party integrations:

#### SSO Provider Abstraction

<augment_code_snippet path="src/IdentityApplication/ApplicationServices/ISSOAuthenticationProvider.cs" mode="EXCERPT">
````csharp
/// <summary>
///     Defines a Single Sign On provider of authentication services
/// </summary>
public interface ISSOAuthenticationProvider
{
    string ProviderName { get; }

    Task<Result<SSOAuthUserInfo, Error>> AuthenticateAsync(ICallerContext caller,
        string authCode, string? emailAddress, CancellationToken cancellationToken);

    Task<Result<ProviderAuthenticationTokens, Error>> RefreshTokenAsync(ICallerContext caller,
        string refreshToken, CancellationToken cancellationToken);
}
````
</augment_code_snippet>

#### OAuth2 Service Abstraction

<augment_code_snippet path="src/Application.Services.Shared/IOAuth2Service.cs" mode="EXCERPT">
````csharp
/// <summary>
///     Defines a generic service for exchanging OAuth2 codes for tokens.
/// </summary>
public interface IOAuth2Service
{
    Task<Result<List<AuthToken>, Error>> ExchangeCodeForTokensAsync(ICallerContext caller,
        OAuth2CodeTokenExchangeOptions options, CancellationToken cancellationToken);

    Task<Result<List<AuthToken>, Error>> RefreshTokenAsync(ICallerContext caller,
        OAuth2RefreshTokenOptions options, CancellationToken cancellationToken);
}
````
</augment_code_snippet>

### Third-Party Integration

#### Plugging in External Identity Providers

To integrate with enterprise identity providers like Auth0, Okta, or IdentityServer:

**1. Replace the Identity Server Provider**

In `IdentityModule.cs`, replace the native provider registration:

<augment_code_snippet path="src/IdentityInfrastructure/IdentityModule.cs" mode="EXCERPT">
````csharp
// EXTEND: Change the identity server provider and its supporting APIs/Applications/Services
services.AddPerHttpRequest<IIdentityServerProvider, NativeIdentityServerProvider>();
````
</augment_code_snippet>

Replace with your custom provider:

```csharp
services.AddPerHttpRequest<IIdentityServerProvider, Auth0IdentityServerProvider>();
// or
services.AddPerHttpRequest<IIdentityServerProvider, OktaIdentityServerProvider>();
```

**2. Implement Custom Provider Services**

Create implementations for each service interface:
- `IIdentityServerCredentialsService`
- `IIdentityServerOpenIdConnectService`
- `IIdentityServerSingleSignOnService`
- `IIdentityServerOAuth2ClientService`
- `IIdentityServerApiKeyService`

**3. Configure SSO Providers**

Register your SSO authentication providers:

<augment_code_snippet path="src/IdentityInfrastructure/IdentityModule.cs" mode="EXCERPT">
````csharp
#if TESTINGONLY
// EXTEND: replace these registrations with your own OAuth2 implementations
services.AddSingleton<ISSOAuthenticationProvider, FakeSSOAuthenticationProvider>();
#endif
````
</augment_code_snippet>

Replace with real providers:

```csharp
services.AddSingleton<ISSOAuthenticationProvider, GoogleSSOAuthenticationProvider>();
services.AddSingleton<ISSOAuthenticationProvider, MicrosoftSSOAuthenticationProvider>();
services.AddSingleton<ISSOAuthenticationProvider, Auth0SSOAuthenticationProvider>();
```

**4. Example Third-Party Provider Implementation**

<augment_code_snippet path="src/IdentityInfrastructure/ApplicationServices/MicrosoftSSOAuthenticationProvider.cs" mode="EXCERPT">
````csharp
public class MicrosoftSSOAuthenticationProvider : ISSOAuthenticationProvider
{
    public string ProviderName => SSOName;

    public async Task<Result<SSOAuthUserInfo, Error>> AuthenticateAsync(ICallerContext caller,
        string authCode, string? emailAddress, CancellationToken cancellationToken)
    {
        var retrievedTokens = await _auth2Service.ExchangeCodeForTokensAsync(caller,
            new OAuth2CodeTokenExchangeOptions(ServiceName, authCode), cancellationToken);

        return tokens.ToSSoUserInfo();
    }
}
````
</augment_code_snippet>

### Configuration

Identity server configuration is managed through standard ASP.NET configuration:

```json
{
  "ApplicationServices": {
    "JWTTokens": {
      "SigningSecret": "your-signing-secret",
      "AccessTokenExpiresAfter": "00:15:00",
      "RefreshTokenExpiresAfter": "7.00:00:00"
    },
    "Auth0": {
      "Domain": "your-domain.auth0.com",
      "ClientId": "your-client-id",
      "ClientSecret": "your-client-secret"
    }
  }
}
```

### Security Considerations

1. **Token Security** - All tokens are signed and have configurable expiration times
2. **Password Security** - BCrypt hashing with high iteration counts
3. **PKCE Support** - Prevents authorization code interception attacks
4. **Rate Limiting** - Built-in protection against brute force attacks
5. **Secure Defaults** - Conservative security settings out of the box
6. **Audit Logging** - Comprehensive logging of all authentication events

### Migration Strategy

When migrating from the native identity server to a third-party provider:

1. **Gradual Migration** - The provider pattern allows gradual migration of services
2. **Data Migration** - User accounts and credentials can be migrated incrementally
3. **Backward Compatibility** - Existing tokens remain valid during transition
4. **Rollback Capability** - Easy rollback to native provider if needed

### Testing Strategy

The identity system includes comprehensive testing support:

- **Unit Tests** - All domain logic and application services
- **Integration Tests** - API endpoints and database interactions
- **External Integration Tests** - Real third-party provider testing
- **Stub Providers** - Fake implementations for testing

<augment_code_snippet path="src/IdentityInfrastructure/ApplicationServices/FakeSSOAuthenticationProvider.cs" mode="EXCERPT">
````csharp
/// <summary>
///     Provides a fake example ISSOAuthenticationProvider that can be copied for real providers
/// </summary>
public class FakeSSOAuthenticationProvider : ISSOAuthenticationProvider
{
    public const string SSOName = "testsso";
    // ... implementation for testing
}
````
</augment_code_snippet>

This comprehensive identity server implementation provides a solid foundation for authentication and authorization while maintaining the flexibility to integrate with enterprise-grade identity solutions as your system scales.
