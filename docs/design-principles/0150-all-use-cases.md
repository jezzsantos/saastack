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

1. Assign [platform] roles to the current user

### Identities

#### API Keys

1. Create a new API key for the current user

#### Auth Tokens

1. Refresh an access token
2. Revoke an existing access token

#### Machines

1. Register a new machine

#### Passwords

1. Authenticate the current user (with a password)
2. Register a new person (with a password)
3. Confirm registration of a person (from email)

#### Single-Sign On

1. Authenticate and (auto-register) a person from another OAuth2 provider

### Images

TBD

### Organizations

1. Create a new organization for the current user
2. Inspect a specific organization
3. 

### Subscriptions

TBD

### User Profiles

TBD

## BEFFE

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

