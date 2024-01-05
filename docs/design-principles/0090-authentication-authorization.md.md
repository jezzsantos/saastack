# Authentication & Authorization

## Design Principles

## Implementation

Cookie Authentication

Usually performed by a BackendForFrontend component, reverse-proxies the token hidden in the cookie, into a token passed to the backend

Authorization

For marked endpoints, verifies that the cookie exists.

 