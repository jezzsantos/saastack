# Authentication

* status: accepted
* date: 2024-01-01
* deciders: jezzsantos

# Context and Problem Statement

AuthN (short for Authentication) is not to be confused with AuthZ (short for Authorization).

AuthN is essentially the process of identifying an end-user given some kind of proof that they provide. Common forms of proof include: usernames+passwords, pins, locations, keys etc. Once that proof is verified, the end-user can correctly and uniquely be identified, and information about the end-user can be used downstream to "Authorize" the user to access parts of the system, and to apply rules to their use of the software.

> Part of the reason that many developers confuse and conflate AuthN and AuthZ is to do with how they've experienced them in the various frameworks they've used in the past that have not been so explicit in the distinction. Another part of the reason is that some "implementations" of these mechanisms combine them as well.
>
> For example, in '[Basic Authentication](https://datatracker.ietf.org/doc/html/rfc7617)', we send a user's authentication credentials (username + password) along with the same request that we want to be performed. The credentials are first authenticated to identify the user, and then the request is authorized, all in the same interaction.
>
> It gets even more conflated for things like '[HMAC authentication](https://datatracker.ietf.org/doc/html/rfc2104)' where typically the user's identity is pre-assumed to be known ahead of time, so when the request arrives, the identity of the user is already known (you could say, implied by the HMAC signing key, or included in other data the request). There may be no explicit authentication step at all.
>
> However, in most systems (post 2012) that adopted the '[OAuth2 Authorization Framework](https://www.rfc-editor.org/rfc/rfc6749)' and then later the '[OpenID Connect Authentication Framework](https://openid.net/specs/openid-connect-core-1_0.html)' the AuthZ and AuthN steps are explicitly separated into different processes using tokens and claims.

Today there are many options for integrating AuthN into a SaaS product, it is not a straightforward choice to make, and each one of those options represents a set of trade-offs in many things, including:

* Cost
* Maintainability
* Complexity
* Flexibility
* Capability and Familiarity,
* Vendor Lock-In,
* Security, etc

Given this is a SaaS context, for small teams, and given we are building a backend API plus a separate front end web app, we need to aim to:

* Choose a decent (well-known) integration that can added plugged in (at low cost), and easily replaced with another plugin later without requiring re-engineering the architecture.
* Offers reasonable flexibility (to expand to changing needs of a particular business).
* It must be secure and multi-client friendly (e.g., web app and mobile app and machine-to-machine) when applied to a backend API (i.e. not bound to cookies and sessions of a front end web server).
* We prefer to utilize refreshable, transparent, and signed JWT tokens (with the option of encrypting them as opaque if necessary later).
* We will need it to be extensible to accommodate Single-Sign-On (SSO) scenarios.
* It needs to be very secure, and we need to ship a basic "user credentials" solution out of the box to get started with.

Lastly, we need to introduce some minimal abstractions to make that integration easier to understand and to rip out and change later (e.g., ports and adapters).

## Considered Options

The options are:

1. Custom Implementation
2. ASP.NET Core Identity (ANCI)
3. Auth0
4. Duende IdentityServer (https://duendesoftware.com/products/identityserver)
5. OpenIddict (https://documentation.openiddict.com/)
6. etc

## Decision Outcome

`Custom Implementation`

- Can support many authorization protocols, like: HMAC, Basic, APIKey, Claims, Cookies, etc. as prescribed by 3rd party integrations and web hooks.
- Can support many authorization assertions, like: Roles (RBAC), Features access, etc.
- Can support SSO authentication from 3rd parties, like: Microsoft, Google, Facebook etc.
- Would not be OIDC authentication compliant at first, but could be made to be OIDC compliant later, by either integrating with an external provider or implementing the endpoints and flows.
- No additional operational costs, (unlike IdentityServer, Auth0 require etc.)
- Can be ripped out and replaced out for an implementation of IdentityServer, Auth0, Okta, or other solution later.
- Has decent support for most of the most common capabilities for an early stage SaaS business (e.g. transparent JWT tokens, custom claims, Single Sign On integrations, MFA, Authenticator apps, password management, etc.)
- Is a superior option to `ASP.NET Core Identity` since we would not be limited, as ANCI is, to opaque non-JWT tokens, and we can control the behaviour of each of the APIs, which cannot be done in ANCI either.

Downsides:

* Increased risk of creating security vulnerabilities by developers (making mistakes)
* Increased risk of storing user password data in this system

## More Information

See Andrew Locks discussion on using the [ASP.NET Core Identity APIs in .NET 8.0](https://andrewlock.net/should-you-use-the-dotnet-8-identity-api-endpoints/#what-are-the-new-identity-api-endpoints-)
