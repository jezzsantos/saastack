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

## [1.0.0] - 2025-08-22

### Non-breaking Changes
- Added new methods for mapping Optionals to Nullables and vice versa for ValuesObjects, Domain Events and Application Layer conversions.
- Native support for `Optional<T>` properties in ValueObjects, wrt to `GetAtomicValues()` and `Rehydrate()` methods.
- `ValueObjectBase<T>.RehydrateToList()` now returns `List<Optional<string>>` instead of `List<string?>` in order to simplify mapping values.
  - Please update all the ValueObject `Rehydrate()` methods to use the new `Optional<string>` return type, utilizing the new syntax, and new extension methods
  - Please update all the ValueObject `GetAtomicValues()` methods to use the new collection syntax, and no longer destruct the `Optional<T>` values, nor `DateTime` values anymore
  
### Breaking Changes


### Fixed
- Roslyn rules now support C#12 syntax for new collections syntax in `ValueObjectBase<T>.GetAtomicValues()`
- Fixed all ValueObjects to use Optionals instead of Nullables, and turned back on Roslyn rules that enforce that.
- Simplified/eliminated the use of nested `Optional<T>` in `CommandEntity` and `QueryEntity` persistence classes.

---

## [1.0.0] - 2025-08-09

### Non-breaking Changes
- Added `Locale` to `UserProfile` resource and all associated APIs, as backwards compatible
- Added `Locale` to `EndUserProfile` value object, but backwards compatible
- Existing domain events that added `Locale`, but are backwards compatible with previous events:
  - `Domain.Events.Shared.EndUsers.Registered`
  - `IdentityDomain.Events.SSOUsers.DetailsChanged`
- Added new domain event `Domain.Events.Shared.UserProfiles.LocaleChanged`

### Breaking Changes
- Read models tables have been added to the main SqlServer database, and will need to be updated:
- (eventing-generic):
  - `SSOUser` table added column `Locale`
  - `UserProfile` table added column `Locale`

---

## [1.0.0] - 2025-08-07

### Non-breaking Changes
- Open ID Connect is now minimally supported, with a new OAuth/OIDC API. Only the 'Authorization Code Flow' is supported, none of the other OAuth2 flows are implemented.
- Two additional APIs groups have been added to support OAuth2 clients and to manage client_secrets and user consents.
- All services of the Identity subdomain, have been abstracted behind the `IIdentityServerProvider` interface. This permits the native implementation to be replaced with a 3rd party implementation, such as Auth0, Okta, IdentityServer, or Keycloak at a later date.
- Location of the `JwtTokenService` has moved from `Infrastructure.Web.Hosting.Common` to `Infrastructure.Web.Hosting.Identity`.
- Added `codeVerifier` to the SSO authentication flow for passing to the 3rd party SSO Provider, in case the case where PKCE was used to obtain the authorization code in the client.

### Breaking Changes
- All security JWT tokens (specifically the `access_token` and `refresh_token` and now `id_token`) that are created by this API, and verified by this API, are now signed using an RSA256 asymmetric algorithm, instead of the previous HMAC-SHA512 symmetric algorithm.
  - Two new configuration settings are required to be added to your deployment, they are: `Hosts:IdentityApi:JWT:PublicKey` and `Hosts:IdentityApi:JWT:PrivateKey`. You can delete the old `Hosts:IdentityApi:JWT:SigningKey` setting, it is no longer used.
  - Any previously issued access_token and refresh_token by the API that may have been stored in any repositories or event stores (Such as the tokens stored in `AuthToken` table) will now not be able to be signature verified. Older ones can be deleted from this table, with no impact.
- New readmodel tables have been added to the main SqlServer database, and will need to be created:
  - (snapshotting-generic) `OpenIdConnectAuthorization`
  - (eventing-generic) `OAuth2Client` and `OAuth2ClientConsent`
  - (snapshotting-generic) The following columns were added to the `AuthToken` table in the SqlServer database:
    - `IdToken`
    - `IdTokenExiresOn`
    - `RefreshTokenDigest`
    - The `RefreshToken` colum data is now encrypted.
    - Existing rows can be safely deleted, and recreated automatically, fully populated when users next login. 
- The domain events of the AuthTokenRoot (`TokensChanged`, and `TokenRefreshed`) will now have encrypted values of all tokens. Whereas before they were the unencrypted raw values. If there are any consumers of these events they will need to be updated to decrypt the values before using them.
- These events both have new (optional) properties for the `IdToken` and `IdTokenExpiresOn` and `RefreshTokenDigest`, but are not event-sourced, by default.

### Fixed
- All APIS now have available a new ApiResult type called `ApiRedirectResult`, which is expected to be used rarely, but when used can be used to return a `HTTP 302-Found` redirect response. This was necessary to implement the new OpenIdConnect endpoints.
- `JsonClient` now recognizes properties marked with the `[FromQuery(Name="aname")]` attribute as well as `[JsonPropertyName("aname")]`, that are used to send requests with query string parameters for GET requests. This was necessary to implement the new OpenIdConnect endpoints.
- Removed the need for using any ASPNET binding attributes, such as `[FromQuery]` on many of the request types, except in exceptional cases. Improved the custom `BindAsync()` as a result, and removed the `[AsParameters]` usage from minimal API generator, and fixed up the affected OpenApi generation.
  - PLease remove the use of any `[FromQuery]` attributes on regular properties of POST, GET, PUTPATCH or DELETE request types. They are only needed in cases where you explicitly want a specific property to be in teh querystring, and not in the body of a POST or PUTPATCH request - which should be rare.

---

## [1.0.0] - YYYY-MM-DD

### Changed

- The codebase was copied on this day
