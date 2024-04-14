# All Use Cases

These are the main use cases of this product that are exposed via "public" APIs in the Backend, e.g., `ApiHost1`.

> This does not include all the interactions of all subdomains, of which there are many more "private" interactions that are not accessible by "public" clients.

## Core Subdomains

### Cars

1. Register a new car
2. Delete a car
3. Schedule the car for "maintenance"
4. Take a car "offline"
5. Inspect a specific car
6. Find all the un-availabilities for a specific car
7. Find all the cars on the platform
8. Find all the available cars for a specific time frame

### Bookings

1. Make a booking for a specific car and time frame
2. Cancel an existing booking
3. Find all bookings for all cars

## Generic Subdomains

### Health

1. Check the health of the web service

### Ancillary

#### Audits

1. [Asynchronously] persist an audit to permanent storage

#### Emails

1. [Asynchronously] deliver an email to an email gateway
2. Find all delivered emails

#### Feature Flags

1. Fetch a specific flag
2. Fetch all feature flags (for the current deployment environment)
3. Fetch a specific flag for a specific user

#### Provisioning

1. [Asynchronously] notify the provisioning of a new tenant

#### Recording

1. Record a new measurement event (from a Frontend)
2. Record a new usage event (from a Frontend)

#### Usages (Product)

1. [Asynchronously] deliver a usage event to product usage service

### Users (End User)

1. Assign [platform] roles to an existing user
2. Unassign [platform] roles to an existing user (except `Standard` role)
3. Invite a guest to register on the platform (a referral)
4. Resend an invitation to a guest
5. Guest verifies an invitation is still valid

### Identities

#### API Keys

1. Create a new API key for the current user

#### Auth Tokens

1. Refresh an access token
2. Revoke an existing access token

#### Machines

1. Register a new machine (anonymously or by authenticated user)

#### Password Credentials

1. Authenticate the current user (with a password)
2. Register a new person (with a password and with optional invitation)
3. Confirm registration of a person (from email)

#### Single-Sign On

1. Authenticate and (auto-register) a person from another OAuth2 provider (with an optional invitation)

### Images

1. Upload a new image (supported image types: jpeg,png, gif, maximum size: 100MB)
2. Update the metadata about an image (i.e. Description)
3. Delete an image
4. Inspect a specific image
5. Download the image file

### Organizations

1. Create a new (shared) organization for the current user
2. Inspect a specific organization
3. Invite another guest or person to an organization (guest by email, or an existing person by email or by ID)

### Subscriptions

TBD

### User Profiles

A user profile will be automatically created for every new registered `EndUser` on the platform (person or machine).
When a person is registered we also query the `IAvatarService` to see if we can find a default avatar for the persons email. The default adapter is Gravatar.com.

1. Change the names, phone, time zone of the profile,
2. Change the address of the profile
3. Add an Avatar image the profile
4. Remove the Avatar from the profile

## Backend for Frontend

These are the main use cases of this product that are exposed via "public" APIs in the Frontend, e.g., `WebsiteHost`.

> In many cases, these API calls are made from a JavaScript client and are forwarded to the Backend APIs.

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

