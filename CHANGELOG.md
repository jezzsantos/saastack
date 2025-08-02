# How It Works

> This change log is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/), and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html)

> All releases are documented in this file, in reverse order.

> We keep at least one `[Unreleased]` section that is to be used to capture changes as you work. When the release is ready, this section is versioned and moved down the file (past the horizontal break), AND a new `[Unreleased]` is created for the next release.

---

## [Unreleased]

### Non-breaking Changes

### Breaking Changes

### Fixed

---


## [1.0.0] - 2025-08-31

### Non-breaking Changes
- Open ID Connect is now partially supported. Only the Authorization Code Flow.
- Two new APIs have been added to support OAuth2 client, to manage client_secrets and consents.
- All services of the Identity subdomain, have been abstracted behind the `IIdentityServerProvider` interface. This permits the native implementation to be replaced with a 3rd party implementation, such as Auth0, Okta, IdentityServer, or Keycloak.
- Location of the `JwtTokenService` has moved from `Infrastructure.Web.Hosting.Common` to `Infrastructure.Web.Hosting.Identity`.
- Added codeVerifier to the SSO authentication pathway for passing to the SSO Provider, in case the case where PKCE was used to obtain the authorization code.

### Breaking Changes
- All security JWT tokens (specifically the `access_token` and `refresh_token` and now `id_token`) that are created by this API, and verified by this API are now signed using an RSA256 asymmetric algorithm, instead of the previous HMAC-SHA512 symmetric algorithm.
  - Any previously issued tokens that have been stored in any repositories or event stores will now not be able to be verified. Please analyze your data first.
  - Two new configuration settings are required to be added to your deployment, they are: `Hosts:IdentityApi:JWT:PublicKey` and `Hosts:IdentityApi:JWT:PrivateKey`.
- Two new tables have been added to the main SqlServer database, and will need to be created:
  - (snapshotting-generic) `OpenIdConnectAuthorization`
  - (eventing-generic) `OAuth2Client` and `OAuth2ClientConsent`
- The domain events of the AuthTokenRoot (`TokensChanged`, and `TokenRefreshed`) now have encrypted values of the tokens. Whereas before they were the unencrypted raw values.
  - The following columns were added to the `AuthToken` table in the SqlServer database:
    - `IdToken`
    - `IdTokenExiresOn`
    - `RefreshTokenDigest`

### Fixed
- All APIS now support a new APiResult type called `ApiRedirectResult`, which , expected to be used rarely, can be used to return a HTTP302-Found redirect response. This was necessary to implement the new OpenIdConnect endpoints.
- JsoncClient now recognizes properties marked with the `[FromQuery(Name="aname")]` attribute, used to read incoming query string parameters for GET requests. This was necessary to implement the new OpenIdConnect endpoints.
- Removed the need for using any ASPNET binding attributes, such as `[FromQuery]`, except in exceptional cases. Improved the custom BindAsync. Removed the `[AsParameters]` usage from minimal API generator, and fixed up the affected OpenApi generation.

---

## [1.0.0] - YYYY-MM-DD

### Changed

- The codebase was copied on this day
