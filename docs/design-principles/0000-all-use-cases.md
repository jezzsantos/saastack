# All Use Cases

These are the main use cases of this product that are exposed via "public" APIs in the Backend, e.g., `ApiHost1`.

> This does not include all the interactions of all subdomains, of which there are many more "private" interactions that are not accessible by "public" clients.

## Core Subdomains

Legend:

* <sup>$$$</sup> denotes a customer API that requires a paid/trial subscription.

* <sup>TSTO</sup> denotes an API that is only available for testing (not compiled into production builds).

* <sup>SVC</sup> denotes an internal API that is only accessible for service accounts of the system itself.

* <sup>OPS</sup> denotes a support API that is only accessible to operations team of the platform

### Cars (Sample)

> This is sample subdomain, and is expected to be deleted when this product goes to production

1. Register a new car <sup>$$$</sup>
2. Delete a car <sup>$$$</sup>
3. Schedule the car for "maintenance" <sup>$$$</sup>
4. Take a car "offline" <sup>$$$</sup>
5. Inspect a specific car <sup>$$$</sup>
6. Find all the un-availabilities for a specific car <sup>TSTO</sup>
7. Find all the cars on the platform <sup>$$$</sup>
8. Find all the available cars for a specific time frame <sup>$$$</sup>

### Bookings (Sample)

> This is sample subdomain, and is expected to be deleted when this product goes to production

1. Make a booking for a specific car and time frame <sup>$$$</sup>
2. Cancel an existing booking <sup>$$$</sup>
3. Find all bookings for all cars <sup>$$$</sup>

## Generic Subdomains

### Health

1. Check the health of the web service

### Ancillary

#### Audits

For permanently recording compliance and business critical events that are performed on the platform.

1. [Asynchronously] persist an audit to permanent storage <sup>SVC</sup>
2. Find all stored audits <sup>TSTO</sup>
3. Deliver all pending audits <sup>TSTO</sup>

#### Emails

For delivering emails to 3rd party services

1. [Asynchronously] deliver an email to an email gateway <sup>SVC</sup>
2. Find all delivered emails <sup>TSTO</sup>
3. Deliver all pending emails <sup>TSTO</sup>

#### Feature Flags

For controlling feature flags for software releases

1. Fetch a specific flag <sup>SVC</sup>
2. Fetch all feature flags (for the current deployment environment) <sup>SVC</sup>
3. Fetch a specific flag for the current (Authenticated) user

#### Provisioning

Used for registering new tenants on the platform, when provisioning physical cloud infrastructure for individual tenants.

1. [Asynchronously] notify the provisioning of a new tenant <sup>SVC</sup>
2. Deliver all pending provisionings <sup>TSTO</sup>

#### Recording

Recording combines, logging, auditing, metrics and usages in a single concept.

1. Record a new measurement event (from a Frontend) <sup>SVC</sup>
2. Record a new usage event (from a Frontend) <sup>SVC</sup>

#### Usages (Product)

Usages are the means to record the usage of a product by a user.

1. [Asynchronously] deliver a usage event to product usage service <sup>SVC</sup>
2. Deliver all pending usages <sup>TSTO</sup>

### Users (End User)

These are the end users on the platform.

1. Assign [platform] roles to an existing user <sup>OPS</sup>
2. Unassign [platform] roles to an existing user (except `Standard` role) <sup>OPS</sup>
3. Invite a guest to register on the platform (a referral)
4. Resend an invitation to a guest
5. Guest verifies an invitation is still valid
6. Change the default organization for the current (Authenticated) user <sup>$$$</sup>
7. List all memberships of the current (Authenticated) user
8. Inspect a specific user <sup>TSTO</sup>

### Identities

#### API Keys

API Key are the way a user (person or machine) can authenticate with the platform using an API key.

1. Create a new API key for the current (Authenticated) user <sup>$$$</sup>
2. List all API keys of the current (Authenticated) user <sup>$$$</sup>
3. Delete an API Key <sup>$$$</sup>

#### Auth Tokens

Auth Tokens are the way that a user can authenticate with the platform using one or more tokens.

1. Refresh an access token
2. Revoke an existing access token

#### Machines

Machines are the way that non-human entities can operate on the platform.

1. Register a new machine (anonymously or by authenticated user)

#### Password Credentials

Is the way a user can authenticate with the platform using a username and password.

1. Authenticate the current user (with a password)
2. Register a new person (with a password and with optional invitation)
3. Confirm registration of a person (from email)
4. Initiate a password reset
5. Resend password reset notification
6. Verify a password reset token
7. Reset password
8. Fetch the registration confirmation token <sup>TSTO</sup>

#### Single-Sign On

Is the way that a user can authenticate with the platform using an external OAuth2 provider (like Google, Facebook, etc.)

1. Authenticate and (auto-register) a person from another OAuth2 provider (with an optional invitation)

### Images

Provides a simple image service for uploading and downloading images.

1. Upload a new image (supported image types: jpeg,png, gif, maximum size: 100MB) <sup>$$$</sup>
2. Update the metadata about an image (i.e. Description) <sup>$$$</sup>
3. Delete an image <sup>$$$</sup>
4. Inspect a specific image
5. Download the image file

### Organizations

Organizations are the primary way that users are grouped together on the platform. An organization can be a "personal" organization (for a single user) or a "shared" organization (for multiple users). An organization is the manifestation of a tenant on the platform.

1. Create a new (shared) organization for the current user <sup>$$$</sup>
2. Inspect a specific organization
3. Change the organization's details
4. Add an Avatar image to the organization
5. Remove the Avatar from the organization
6. Invite another guest or person to an organization (guest by email, or an existing person by email or by ID) <sup>$$$</sup>
7. Un-invite a member from the organization <sup>$$$</sup>
8. Assign roles to a member <sup>$$$</sup>
9. Unassign roles from a member <sup>$$$</sup>
10. List all members of the organization
11. Delete the organization (must be no remaining members) <sup>$$$</sup>

### Event Notifications

Event Notifications are the way that subdomains can listen to each other in a loosely-coupled way. A "producing" subdomain produces "domain_events" which are stored on a message bus. This API provides an endpoint to consume those "domain_events".

1. Handle a domain_event published to a message bus <sup>SVC</sup>
2. Find all delivered domain_events <sup>TSTO</sup>
3. Deliver all pending domain_events <sup>TSTO</sup>

### Subscriptions

A billing subscription is created for every
`Organization` (personal and shared) on the platform for any (person or machine). It represents the billing subscription plan for that tenant/organization (i.e., pricing, cost, and features). The subscription plan determines the
`Features` each user has access to on the platform, and it defines the fiscal responsibilities that each
`EndUser` has an obligation for (i.e., scheduled payments).

Every `Organization` must have a single `EndUser` that owns the fiscal responsibility of the
`Organization` (called the "buyer"), who can control the extent of that responsibility.

1. Inspect the subscription for a specific organization
2. Upgrade/Downgrade the subscription plan (or transfer the subscription to another authorized buyer when the buyer has left the platform)
3. Cancel the subscription
4. List all the available pricing plans
5. Search the billing history for a subscription
6. Transfer the subscription to another authorized buyer
7. Export all subscriptions that could be migrated (when migrating off of an existing billing provider) <sup>SVC</sup>
8. Migrate the billing provider data (from one billing provider to a new one) <sup>OPS</sup>
9. Force the cancellation of a subscription for a specific organization <sup>OPS</sup>

### User Profiles

A user profile will be automatically created for every new registered `EndUser` on the platform (person or machine).
When a person is registered we also query the
`IAvatarService` to see if we can find a default avatar for the persons email. The default adapter is Gravatar.com.

1. Change the details (i.e. names, phone, time zone) of the profile
2. Change the address of the profile
3. Add an Avatar image the profile
4. Remove the Avatar from the profile
5. Inspect the profile of the current (Authenticated) user

## Backend for Frontend

These are the main use cases of this product that are exposed via "public" APIs in the Frontend BEFFE, e.g.,
`WebsiteHost`.

> In many cases, these API calls are made from a JavaScript client and are forwarded to the Backend APIs.
> Most of these APIs are protected by CSRF protection, and only accessible to the JavaScript application

### Health

1. Check the health of the web service

### Feature Flags

1. Fetch all feature flags (for the current deployment environment)
2. Fetch a specific flag for a specific user

### Recording

1. Record a new measurement event
2. Record a new usage event
3. Record a crash report
4. Record a diagnostic trace
5. Record a page view

### Authentication

1. Authenticate the user (with a password or for the specified SSO provider)
2. Refresh an authenticated session
3. Logout of an authenticated session 

