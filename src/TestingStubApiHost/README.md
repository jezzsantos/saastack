# What is this host?

This host is used ONLY in TESTINGONLY environments (Never in CI, staging or production environments)

It contains stubs that are required in local development (i.e. when manually testing or debugging locally), but it is not used/required to be running in automated integration testing.

It contains:

1. Stubbed "API endpoints" for 3rd party HTP services (used by the adapters in any of the other hosts), in the TESTINGONLY environment.
2. Stubbed "workers" that stand in for other infrastructural components such as Azure Functions/AWS Lambdas, that cannot be easily run locally, that are required to integrate the other hosts together to work locally.

## How should it be used?

This host should be run along the other web hosts (i.e. WebsiteHost, and ApiHost1, ApiHost2, etc.) when running the code locally.

> Warning: If this host is not run in local development, then neither the 3rd party services can be called, nor will any host receive domain_events from the other hosts.  