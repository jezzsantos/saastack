# Authentication & Authorization

## Design Principles

1. We want a simple authentication and authorization mental model that developers are already familiar with.
2. For authentication we want to create and maintain our own JWT tokens (transparent or opaque), irrespective of how the user is actually authenticated, be that through Credentials, HMAC, or even SSO with 3rd parties (e.g., Google, Microsoft etc.). At the end of the day, our token is the authority in the entire system.
3. We want to store only the bare necessary claims in a JWT token, that have short lifetimes (with longer-lived refresh tokens), to minimize these tokens get out of sync when roles/permissions change for any particular end user of the system.
4. We want Authorization to be discoverable and well defined (not too many options), but also extensible (add your own options)
5. We want to layer several authorization schemes overlaid on each other. e.g. Role-based (RBAC), Feature-based and feature flags.
6. We want it to be declarative and seamlessly integrated (and tooled) along with the other patterns we are introducing, particularly in the API declarations.
7. Since roles and permissions can quickly get out of hand in many systems, we will want to avoid defining fine-grained permissions that could end up bloating tokens. Initially, we wont be managing tokens centrally, but that can change in the long run.
8. We are offering credential-based authentication (username + password) as a starting point, and encourage the adoption of 3rd party integrations later as the product finds success (e.g. Auth0, Okta, IdentityServer, etc).

## Implementation

We are using and configuring standard Microsoft AP.NET authentication and authorization mechanisms to implement both authentication and authorization, and to keep close to that familiar model for most ASPNET developers.

For Authentication schemes, we are initially supporting Password Credentials, HMAC, and API Key. Others can be added as needed. (i.e. to support various webhook callbacks from 3rd party systems).

For Authorization, we are utilizing the minimal API "authorization policies" mechanism, and also defining dynamic policies based on static declarative configuration of APIs.

In web clients, we will use (HTTPOnly) cookies to store JWT tokens (between the browser and the BEFFE), and will relay those JWTs to backend APIs via a reverse proxy.

​ We will prevent those JWT tokens ever being seen by any JavaScript running in a browser, and go to extra lengths to guard against CSRF attacks.

![Credential Authentication](../images/Authentication-Credentials.png)

![SSO Authentication](../images/Authentication-SSO.png)

### Password Credential Authentication

Out of the box, we will offer Password-Credential authentication via the `IPasswordCredentialsApplication`

This will enable quick adoption of the SaaS product, and a foundation to extend out into SSO and 3rd party identity solutions later.

We are providing support for password authentication in a standalone "Identities" subdomain to provide all the services around password management. This whole subdomain/module can then either be extended later, or deleted, and replaced with integration to 3rd parties.

Today it manages these aspects of identity: Authentication via Password, Email Registration Confirmation. Password Reset, 2FA, API key management, etc.

#### Passwords

Passwords must be 8-200 characters long, and contain at lest 1 number, 1 lowercase letter, 1 uppercase letter, and 1 special character.

They are stored (at rest) as salted password hashes, using [B-Crypt encryption](https://github.com/BcryptNet/bcrypt.net), (salts are generated with 4,096 iterations).

When passwords are verified in a login attempt, the authentication process introduces a random time delay to counter timing attacks.

#### Confirmations

Since password credentials include email addresses, these emails addresses, and passwords require resetting, it is important to confirm that the email address is accessible by a human end-user to manage future communications.

### SSO Authentication

We will offer SSO authentication via the `ISingleSignOnApplication`

SSO authentication is typically achieved by authenticating with a 3rd party provider (e.g. Google Microsoft, Facebook, etc) using the [OAuth2](https://oauth.net/2/) "Authorization Code Flow", and once authenticated the user gains access to the whole system. Regardless of where the user is authenticated, access to this system is still governed by using a centralized JWT token.

The OAuth2 "Authorization Code Flow" is usually performed in a browser, that can be redirected to a 3rd party website and back. The flow has several steps where the user signs in and authorizes access to their data provided by the 3rd party.

The final step of this specific flow ([Authorization Code Flow](https://aaronparecki.com/oauth-2-simplified/#web-server-apps)) is to exchange a generated code for an set of tokens (typically an `access_token` and in some cases a `refresh_token`). This step often requires a "secret" to be provided that the 3rd party already knows belongs to a specific client (e.g. `client_id` and `client_secret`). Storing these secrets in browsers is problematic and represents security vulnerabilities.

Managing the first few steps of this flow is typically done by a 3rd party JavaScript library in the browser. However, the last step (exchange code for tokens) can be performed either in the front end browser or in the back end web server.

There are two reasons that this step is performed in the backend API (in the "Identities" subdomain).

1. The `client_secret` (if any) cannot be accessible to any JavaScript, nor stored anywhere in the browser where it is possible to be accessed by any XSS vulenrability.
2. The backend can trust and verify the "code", as being from a trusted 3rd party by performing the exchange itself. The returned tokens are proof of that.
3. The returned tokens (i.e. `access_token` and `refresh_token`) can be used to identify the user in the 3rd party system, and link them to a user in this system. (e.g. find the `EndUser` with the same email address as found int eh claims of the 3rd party tokens)
4. Furthermore, the tokens that are made available by the 3rd party service (i.e. `access_token` and `refresh_token`), can be stored for future use in the API repositories, and can be used to perform activities with the 3rd party system when necessary.
5. The API has full encapsulated control of what the user can and cannot do with 3rd party systems, as opposed to having that code deployed or duplicated to the front-end JavaScript.

### HMAC Authentication/Authorization

HMAC authentication is a legacy hybrid, authentication authorization system. HMAC authentication and authorization is performed as a single interaction between client and the API, by the `HMACAuthenticationHandler`

HMAC authentication is primarily used by trusted external services within the infrastructure of the architecture. (i.e. Azure Functions/AWS Lambdas).

HMAC authentication is performed by the client signing the body of an inbound HTTP request with a signing key (that the client knows). The signature that is calculated is then send with the request in a `X-Hub-Signature` header in the HTTP request.

When the request is received the signature is compared against the calculated signature of the request body (using a signing key that the server knows). If the signatures match it confirms the signature was created with the correct signing key, and thus proves that the sender is to be trusted.

> The signing keys are typically symmetrical keys, but can be asymmetrical also

When the signature check is confirmed, the API then assigns claims to the HTTP request, identifying the caller as a limited service account, and authorization checks are then performed for that service account.

#### Inter-service Communication

HMAC authentication is never used for inter-service communication, only for direct communication with a service.

### Declarative Authentication Syntax

When describing your API's, each API defines a single `IWebRequest` request type and associated `IWebResponse` type.

For example,

```c#
    public async Task<ApiGetResult<Car, GetCarResponse>> Get(GetCarRequest request, CancellationToken cancellationToken)
    {
        var car = await _carsApplication.GetCarAsync(_contextFactory.Create(),
            MultiTenancyConstants.DefaultOrganizationId, request.Id,
            cancellationToken);

        return () => car.HandleApplicationResult(c => new GetCarResponse { Car = c });
    }
```

In the API layer, authentication is declarative, using the `[Route]` attribute, and the `AccessType` enumeration which defines whether it should be authenticated or not and by what provider. The choices are:

* Token authentication (`AccessType.Token`)
* HMAC authentication (`AccessType.HMAC`)
* No authentication (`AccessType.Anonymous`) this is the default, if none is specified.

For example,

```c#
[Route("/cars/{id}", ServiceOperation.Get, AccessType.Token)]
[Authorize(Roles.Tenant_Member, Features.Tenant_Basic)]
public class GetCarRequest : TenantedRequest<GetCarResponse>
{
    public required string Id { get; set; }
}
```

### Token Authorization

Now that we've seen all the ways that users can be authenticated its time to see how they are authorized.

![API Authorization](../images/Authorization-API.png)

Token authorization is provided by the `JwtBearerHandler`, that produces JWT `access_tokens`.

> No matter how the user is actually authenticated (in-built or 3rd party), an `access_token` only issued by this system will be used to gain access to this system.

The primary form of performing authorization, used in this system, are to verify "claims". As opposed to proprietary forms of authorization in the past (i.e. cookies, and opaque tokens etc).

> Note: ASP.NET still supports and uses older forms of authorization is legacy libraries. These are to be avoided if possible.

Every end-user must have a set of "claims" that represent who the end-user is (authenticity) and what they are entitled to do (Access).

The first step in authorization is to ensure that the user has a set of claims that can be authorized. This is performed by the authentication step. Without which the user should be denied access.

In "Token" based authorization, those "claims" are contained within a [transparent] and signed JWT token, with a well defined structure. (see [JWT.io](https://jwt.io) for details)

> In some systems, making this JWT token "opaque" by encrypting it is necessary to hide the values of the actual claims inside it. However, this is not always necessary, and should be avoided if possible for performance reasons in high-throughput systems.

JWT tokens can be passed easily between modules and components of a whole system as a "bearer token" in the `Authorization` header of a HTTP request.

1. When an incoming request (that must be from an authenticated user) is processed, the token is extracted from the `Ahthorization` header first. If not present, then a `401 - Unauthorized` response if returned.
2. Next, the token itself is unpacked into its component parts, and the signature on the token is verified to be from the trusted issuer. In a system like this, the issuers is the system itself. If the signature check fails, then so does authorization and a `401 - Unauthorized` response i returned.
3. Lastly, if the signature is good, this means that the claims in the token, are to be trusted by the receiving party. The claims are unpacked and used to verify the token is valid (from the correct issuer, and not expired), and used to identify the user, and figure out what access the user might have.

JWT tokens, always declare their issuers (backed up with a signature), and always have an expiry date, which is usually short lived to limit exposure should the token fall into the wrong hands (e.g. 15mins).

#### Inter-service Communication

When the system is split into individual services each containing one or more subdomains (a.k.a micro-services), the incoming JWT token, used to access one service can be relayed to access other services. This is achieved using the `ICallerContext` and the `InterHostServiceClient` that know collaborate to relay calls between services.

#### Refresh Tokens

When an end-user is authenticated, either from one of the in-built authentication mechanisms above, or by 3rd parties, the authentication step often returns a short-lived `access_token`, along with a long-lived `refresh_token`.

The `refresh_-token` can then be stored and used later to re-issue another `access_token` when a previous one has expired. In this way, a user's access to a system can be longer lived (until the `refresh_token` expires). And older `access_tokens` automatically retired (expired).

When the `refresh_token` finally expires (e.g. after 7 days). The end-user will be forced to authenticate again, to obtain access to the system.

### API Key Authorization

API key authorization is provided as a convenient alternative to Token based authorization, primarily for use in long-lived machine-to-machine interactions. API Key authorization is provided by the `APIKeyAuthenticationHandler`

API keys can be issued for accessing the system by both `machine` and `person` end-users.

API keys can be included in any HTTP request as either the `username` component of a "Basic Authentication" request, or as a `&apikey=` parameter in the query string of a request.

They have expiry dates and can be more tightly controlled (in terms of validity) by the API, since they themselves do not contain any claims.

#### Inter-service Communication

When the system is split into individual services each containing one or more subdomains (a.k.a micro-services), the incoming API key, used to access one service can be relayed to access other services. This is achieved using the `ICallerContext` and the `InterHostServiceClient` that know collaborate to relay calls between services.

#### Refresh Tokens

API keys do not support refreshing issued API keys. When issuing the API key the client gets to define the expiry date, and should acquire a new API key before that expiry date themselves.

### Cookie Authorization

* TBD
* Performed by a BackendForFrontend (BEFFE) component, reverse-proxies the token hidden in the cookie, into a token passed to the backend

### Declarative Authorization Syntax

Authorization is both declarative (at the API layer), and enforced programmatically downstream in other layers.

> In the Application layer, the current users authorization can be viewed and checked using data on the `ICallerContext` object.

In the API layer, authorization is declarative, using the `[Authorize]` attribute.

For example,

```c#
[Route("/cars/{id}", ServiceOperation.Get, AccessType.Token)]
[Authorize(Roles.Tenant_Member, Features.Tenant_Basic)]
public class GetCarRequest : TenantedRequest<GetCarResponse>
{
    public required string Id { get; set; }
}
```

There are two kinds of aspects to authorize, Roles and Features.

### Role-Based Authorization

In this system, there are two sets of "roles" to manage access to any APIs and underlying subdomains.

This is commonly referred to as [RBAC](https://en.wikipedia.org/wiki/Role-based_access_control).

1. For "Platform" APIs and subdomains that are untenanted (all tenants)
2. For "Tenant" APIs and subdomains that are specific to a tenant (or an organization).

All `End-Users` should have, at least, a minimum level of access to all untenanted API's based on their role, otherwise they literally have no access to do anything in the system. By default, every end-user in the system should have the `PlatformRoles.Standard` role, used for accessing all untenanted APIs, and some Tenanted APIs.

This means, that despite what specific roles are required access to any tenanted API or subdomain (which are tenant specific), a user can access untenanted API and subdomains (e.g. `EndUsers`, `Organizations`, `Images`, etc).

While it is possible in some products to add more "Platform" level roles, it is not usually necessary.

However, adding or updating "Tenant" level roles is often the case for many SaaS products, beyond just `TenantRoles.Member` and `TenantRoles.Owner`.

> Warning: even though RBAC is common in many SaaS products, so is the next level of granularity: "Permissions". Using permissions (as well as roles) can get very fine-grained very quicky if not designed very carefully. It is not recommended to go there easily, and this approach should be done very cautiously. The implications of using fine grained permissions, over time, typically means that `access_token` start to contain a large number of claims related to permissions (for all kinds of resources), depending on the design of the system. There is a real danger of stuffing too much authorization information in these claims and this can lead to performance and synchronicity issues that have not been thought out carefully beforehand.

### Feature-Based Authorization

Feature-based authorization is about what level of access to a feature/capability does a user have with respect to their associated billing subscription (if any).

Just like the roles above, there are two sets of "features" that apply separately to "Platform" resources as well as "Tenant" resources.

1. For "Platform" APIs and subdomains that are untenanted (all tenants)
2. For "Tenant" APIs and subdomains that are specific to a tenant (or an organization).

All `End-Users` should have, at least, a minimum level of access to all untenanted API's based on a specific feature set, otherwise they literally have no access to do anything in the system. By default, every end-user in the system should have the `PlatformFeatures.Basic` feature set, used for accessing all untenanted APIs, and some Tenanted APIs, no matter what subscription plan they have.

In most SaaS products there are one or more pricing tiers. These are analog to "features".

It is likely that every product will define its own custom tiers and features as a result.

By default, we've defined `Basic` to represent a free set of features, that every user should have at a bear minimum. This "feature set" needs to be made available even when the end-user loses their access to the rest of the system. For example, their free-trial expired. We've also defined `PaidTrial` to be used for a free-trial notion, and other tiers for paid pricing tiers. These are expected to be renamed for each product.