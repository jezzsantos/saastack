# Authentication

* status: accepted
* date: 2024-01-01
* deciders: jezzsantos

# Context and Problem Statement

AuthN (short for Authentication) is not to be confused with AuthZ (short for Authorization).

AuthN is essentially the process of identifying an end-user given some kind of proof that they provide (common forms include usernames+passwords, pin numbers etc). Once that proof is verified, the end-user can be identified, and information about the end-user can be used downstream to authorize the user and apply rules to their use of the software.

Today there are many options for integrating AuthN into a SaaS product, and each and everyone of those options represents a set of trade-offs in many things, including:

* Cost
* Maintainability
* Complexity
* Flexibility
* Capability and Familiarity,
* Vendor Lock-In,
* Security, etc

Given this is a SaaS context, for small teams, and given we are building a backend API plus a separate front end web app, we need to aim to:

* Choose a decent (well-known) integration that can added easily (at low cost),
* Offer reasonable flexibility (to expand to changing needs of a particular business).
* The chosen solution must be customized easily, and can be easily upgraded or replaced later with other integrations.
* It must be secure and multi-client friendly (e.g., web app and mobile app and machine-to-machine) when applied to a backend API (i.e. not bound to cookies).
* We prefer to utilize refreshable, transparent, signed JWT tokens.
* We will need it to be extensible to accommodate Single-Sign-On (SSO) scenarios.
* It needs to be very secure, and we need to ship user credentials solution out of the box.

Lastly, we need to introduce some minimal abstractions to make that integration easier to understand and to rip out and change later (e.g., ports and adapters).

## Considered Options

The options are:

1. Custom Implementation
2. ASP.NET Core Identity
3. Auth0
4. Duende IdentityServer (https://duendesoftware.com/products/identityserver)
5. OpenIddict (https://documentation.openiddict.com/)
6. etc

## Decision Outcome

`Custom Implementation`

- No additional operational costs, (unlike IdentityServer, Auth0, etc)
- Can be swapped out for an implementation of IdentityServer, Auth0, Okta, or other solution.
- Has decent support for most of the most common capabilities for SaaS business e.g. Transparent JWT tokens, custom claims, Single Sign On integrations, MFA, Authenticator apps, password management, etc)
- Is a superior option to `ASP.NET Core Identity` since we are not limited to opaque non-JWT tokens (as they are in ANCI), and we can control the behaviour of each of the APIs, which cannot be done in ANCI.

## More Information

See Andrew Locks discussion on using the [ASP.NET Core Identity APIs in .NET 8.0](https://andrewlock.net/should-you-use-the-dotnet-8-identity-api-endpoints/#what-are-the-new-identity-api-endpoints-)
