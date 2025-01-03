# Deployment of SaaStack

This document details the basic steps required to deploy your software into a production environment.

A production environment might be in the cloud or on premise. The deployment process is similar, except for the tools used to perform the deployment.

By default, this deployment is assumed to take place from a GitHub repository using GitHub Actions. However, you can use any CI/CD tool you prefer. 

## Automated deployment

The deployment process is automated using GitHub Actions. The deployment process is defined in the `.github/workflows/deploy.yml` file.

## Variables

Most of the required variables should be self-explanatory.

Here are ones that might need a little more explanation:

### Operator Whitelist

Setting name: `HOSTS_ENDUSERSAPI_AUTHORIZATION_OPERATORWHITELIST`

This is a semicolon `;` delimited list of email addresses of user accounts that are authorized to act as operators in the system.
This list is populated before the user accounts are registered and when they are registered later, they are promoted to `operator` status.

**IMPORTANT**: You should always have at least one operator account email address defined in this list before your software goes to production for the first time, otherwise you will have no other way to promote other operators in the system. 

> For user accounts that already exist, they can be promoted by existing operator accounts.



## Secrets

You MUST generate new secrets for your deployed services.

**IMPORTANT**: You MUST never re-use the secrets in this repository in a production environment. hey are well known to anyone who has access to this repository.

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
