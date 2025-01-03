# Deployment of SaaStack

This document details the basic steps required to deploy your software into a production environment.

A production environment might be in the cloud or on premise. The deployment process is similar, except for the tools used to perform the deployment.

By default, this deployment is assumed to take place from a GitHub repository using GitHub Actions. However, you can use any CI/CD tool you prefer, many of the steps below are similar.

## Automated deployment

The deployment process is automated using GitHub Actions.

This deployment process is defined in the [deploy.yml](../.github/workflows/deploy.yml) file.

Essentially we are deploying the following pieces of infrastructure:
* Azure:
  * An Azure App Service for each API Host (i.e., `ApiHost`) for the Backend APIs.
  * An Azure App Service for the `WebsiteHost` for the Frontend website.
  * An Azure FunctionHost for the functions that monitor queues and message buses.
  * An Azure Service Bus for publishing domain_events to various registered subscribers (i.e., `ApiHost1`).
  * An Azure Storage Account for its Queues and Blob storage.
* AWS:
  * An AWS Lambda function for each API Host (i.e., `ApiHost`) for the Backend APIs.
  * An AWS Lambda function for the `WebsiteHost` for the Frontend website.
  * An AWS Lambda function, and Queues for each of the Lambdas that monitor queues and message buses.
  * An AWS SNS for publishing domain_events to various registered subscribers (i.e., `ApiHost1`).
  * An AWS S3 bucket for Blob storage.

To deploy any of these services to the respective cloud provider, we use standard GitHub Actions to perform the deployment.
Each one of these services may require one or more variables/secrets to be defined in the GitHub repository.

We define an environment in the GitHub project first, then we define the variables and secrets for that environment.
The starting list of those variables and secrets are detailed in the `appsettings.json` files of each of the deployable hosts in the solution. They are defined in the `Deploy -> Required -> Keys` sections of the `appsettings.json` files.

For each of these settings, we must define an equivalent variable or secret in the GitHub project.

The naming convention is to take the fully qualified name in `appsettings.json`, and convert it to uppercase, and replace the following characters: `:`, `-` and `.` with `_`.

For example:
```json
{
  "ApplicationServices": {
    "Persistence": {
      "Kurrent": {
        "ConnectionString": "esdb://localhost:2113?tls=false"
      }
    }
  }
}
```
The equivalent GitHub variable of the above setting would be: `APPLICATIONSERVICES_PERSISTENCE_KURRENT_CONNECTIONSTRING`.

## Variables

Most of the required variables defined in the `Deploy -> Required -> Keys` section of `appsettings.json` should be self-explanatory.

Here are ones that might need a little more explanation:

### Operator Whitelist

Setting name: `HOSTS_ENDUSERSAPI_AUTHORIZATION_OPERATORWHITELIST`

This is a semicolon `;` delimited list of email addresses of user accounts that are authorized to act as operators in the system.
This list is populated before the user accounts are registered and when they are registered later, they are promoted to `operator` status.

**IMPORTANT**: You should always have at least one operator account email address defined in this list before your software goes to production for the first time, otherwise you will have no other way to promote other operators in the system. 

> For user accounts that already exist, they can be promoted by existing operator accounts.

## Secrets

The following MUST be defined as secrets in yor GitHub project, not as environment variables:

**IMPORTANT**: You MUST generate new random secrets for your deployed services!

**IMPORTANT**: You MUST never re-use the secrets defined in this repository in your production environment. They are far too well known to anyone who has access to this repository, using them compromises your production environment.

### HMAC Signing Key

For secrets such as `HOSTS_APIHOST1_HMACAUTHNSECRET` and `HOSTS_ANCILLARYAPI_HMACAUTHNSECRET`:
* Generate a random value using the [HMACSigner.GenerateKey()](https://github.com/jezzsantos/saastack/blob/main/src/Infrastructure.Web.Api.Common/HMACSigner.cs) method.

> Note: You can run the unit tests for this class and copy the value of the generated key in the first test.

**IMPORTANT**: It is vital that all the HMAC signing keys on each deployed host are identical. You can have different values on different hosts, as long as all their client hosts are also updated.  

### CSRF secrets

For the CSRF `HOSTS_WEBSITEHOST_CSRFHMACSECRET` secret
* Generate a new random value using the [CSRFToken.GenerateKey()](https://github.com/jezzsantos/saastack/blob/main/src/Infrastructure.Web.Api.Common/HMACSigner.cs) method as above for HMAC secrets.

> Note: You will want to use a different value than the HMAC signing keys.

For the CSRF `HOSTS_WEBSITEHOST_CSRFAESSECRET` secret:
* Generate a new random value using the [AesEncryptionService.GenerateAesSecret()](https://github.com/jezzsantos/saastack/blob/main/src/Infrastructure.Common/DomainServices/AesEncryptionService.cs) method.

> Note: You can run the unit tests for this class and copy the value of the generated key in the first test.

### SSO

For the `APPLICATIONSERVICES_SSOPROVIDERSSERVICE_SSOUSERTOKENS_AESSECRET` secret:
* Generate a new random value using the [AesEncryptionService.GenerateAesSecret()](https://github.com/jezzsantos/saastack/blob/main/src/Infrastructure.Common/DomainServices/AesEncryptionService.cs) method.

> Note: You can run the unit tests for this class and copy the value of the generated key in the first test.

### JWT Signing Key

For the `HOSTS_IDENTITYAPI_JWT_SIGNINGSECRET` secret:
* Generate a new random value using the [JwtTokenService.GenerateSigningKey()](https://github.com/jezzsantos/saastack/blob/main/src/IdentityInfrastructure/ApplicationServices/JWTTokensService.cs) method.

> Note: You can run the unit tests for this class and copy the value of the generated key in the first test.

## Invoking a deployment

The deployment script (`deploy.yml`) is triggered only by a push to the `main` branch, with a specific commit message, that includes the instruction `xxx`.

> This is a safety feature to prevent accidental deployments in the course of normal development of your product.