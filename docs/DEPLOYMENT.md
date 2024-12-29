# Deployment of SaaStack

This document details the basic steps required to deploy your software into a production environment.

A production environment might be in the cloud or on premise. The deployment process will be similar in either case, except for the tools used to perform the deployment.

By default, this deployment is assumed to take place from a GitHub repository using GitHub Actions.

> However, you can use any CI/CD tool you prefer, many of the steps below will be similar, but will differ based on the toolset you use.

## Automated deployment

The deployment process is automated using GitHub Actions.

This deployment process is defined in the following files:
* [deploy-azure.yml](../.github/workflows/deploy-azure.yml) for deploying to Microsoft Azure.
* [deploy-aws.yml](../.github/workflows/deploy-aws.yml) for deploying to Amazon AWS.

> You can read and understand and reverse engineer these YML files to understand how the deployment process works for your environment or toolset.

Essentially we are deploying the following pieces of infrastructure in the cloud:
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

To deploy any of these services to the respective cloud provider, we will use standard GitHub Actions to perform the deployment, available from the [GitHub Actions Marketplace](https://github.com/marketplace?type=actions).

## Configuration

Each one of these deployable pieces of infrastructure will likely require production specific configuration, using variables/secrets that must be defined outside the source code.One safe place to store them is in the GitHub repository, categorized for a specific "environment".

We define a deployment "environment" in the GitHub project first, then we define the variables and secrets in that specific environment.

> Unlike, Variables in GitHub (which are only available in a specific environment), Secrets in GitHub can be defined at the organization level, or at the repository level or at the environment level. When reading Secrets, they are combined from Organization, Repository and Environment when read from any GitHub Actions.

The initial list of those variables and secrets is detailed in the `appsettings.json` files of each of the deployable hosts in the solution.

Some of these settings have default values already in `appsettings.json`.

Some of these settings will be "required" to be overwritten before being deployed into a production environment, with production environment settings. These settings will not be known at development time, and MUST NEVER be defined in the source code. 

Since, adding new configuration settings is common as products evolve, we need a reliable way to mark up settings as being necessarily "required" to be overwritten in a production environment. As opposed to using the default values already defined in the `appsettings.json` files. 

To this end, in the `appsettings.json` file, there is section: `Deploy -> Required -> Keys` which declares the settings that MUST be overwritten for deployment to any environment.

> This list is not inclusive of all other variables that you may want to change for your production environment, this list is the bare minimum that are REQUIRED to be replaced for deployment. If you add other secrets/variables to your GitHub project (using the same naming convention below), they will also be automatically applied to any `appsettings.json` files in the repository at deploy time too.

To overwrite any settings in `appsettings.json` at deployment time, we must define an equivalent environment variable or secret in the GitHub project. 

The naming convention is to take the fully qualified name in `appsettings.json`, and convert it to uppercase, and replace the following characters: `:`, `-` and `.` with `_`.

For example, if you had this setting in `appsettings.json`:
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
The equivalent GitHub variable/secret name of the setting above, would need to be: `APPLICATIONSERVICES_PERSISTENCE_KURRENT_CONNECTIONSTRING`.

It is more than likely that at some point in time, during normal development a new setting will be added to the `appsettings.json` file, and the developer may also remember to mark it up as "required" in the `Deploy -> Required -> Keys` section of `appsettings.json`, but the developer may forget to add the equivalent variable/secret ot the GitHib project. In this case, the deployment will detect this problem, and fail the deployment, and the error message will tell you which setting is missing, and how to fix it.

This is one of the safeguards of using the `VariableSubstitution` custom action. 

### GitHub environment variables

Most of the required variables defined in the `Deploy -> Required -> Keys` section of `appsettings.json` should be self-explanatory.

Here are ones that might need further explanation, about their origin and use:

#### Operator whitelist

Setting name: `HOSTS_ENDUSERSAPI_AUTHORIZATION_OPERATORWHITELIST`

This is a semicolon `;` delimited list of email addresses of user accounts that are authorized to act as operators in the system.
This list is populated before the user accounts are registered and when they are registered later, they are promoted to `operator` status.

**IMPORTANT**: You should always have at least one operator account email address defined in this list before your software goes to production for the first time, otherwise you will have no other way to promote other operators in the system. 

> For user accounts that already exist, they can be promoted by existing operator accounts.

### GitHub secrets

The following MUST be defined as secrets in yor GitHub project, NOT as environment variables:

> We recommend these are defined in the secrets of the environment, however, they can also be defined at the repository level, or at the organization level.

**IMPORTANT**: You MUST generate new random secrets for your deployed services!
**IMPORTANT**: You MUST never re-use the secrets defined in this repository in your production environment. They are far too well known to anyone who has access to this repository, using them compromises your production environment.

#### HMAC signing key

For secrets such as `HOSTS_APIHOST1_HMACAUTHNSECRET` and `HOSTS_ANCILLARYAPI_HMACAUTHNSECRET`:
* Generate a random value using the [HMACSigner.GenerateKey()](https://github.com/jezzsantos/saastack/blob/main/src/Infrastructure.Web.Api.Common/HMACSigner.cs) method.

> Note: You can run the unit tests for this class and copy the value of the generated key in the first test.

**IMPORTANT**: It is vital that all the HMAC signing keys on each deployed host are identical. You can have different values on different hosts, as long as all their client hosts are also updated.  

#### CSRF secrets

For the CSRF `HOSTS_WEBSITEHOST_CSRFHMACSECRET` secret
* Generate a new random value using the [CSRFToken.GenerateKey()](https://github.com/jezzsantos/saastack/blob/main/src/Infrastructure.Web.Api.Common/HMACSigner.cs) method as above for HMAC secrets.

> Note: You will want to use a different value than the HMAC signing keys.

For the CSRF `HOSTS_WEBSITEHOST_CSRFAESSECRET` secret:
* Generate a new random value using the [AesEncryptionService.GenerateAesSecret()](https://github.com/jezzsantos/saastack/blob/main/src/Infrastructure.Common/DomainServices/AesEncryptionService.cs) method.

> Note: You can run the unit tests for this class and copy the value of the generated key in the first test.

#### SSO

For the `APPLICATIONSERVICES_SSOPROVIDERSSERVICE_SSOUSERTOKENS_AESSECRET` secret:
* Generate a new random value using the [AesEncryptionService.GenerateAesSecret()](https://github.com/jezzsantos/saastack/blob/main/src/Infrastructure.Common/DomainServices/AesEncryptionService.cs) method.

> Note: You can run the unit tests for this class and copy the value of the generated key in the first test.

#### JWT signing key

For the `HOSTS_IDENTITYAPI_JWT_SIGNINGSECRET` secret:
* Generate a new random value using the [JwtTokenService.GenerateSigningKey()](https://github.com/jezzsantos/saastack/blob/main/src/IdentityInfrastructure/ApplicationServices/JWTTokensService.cs) method.

> Note: You can run the unit tests for this class and copy the value of the generated key in the first test.

### Additional GitHub secrets and variables

In order to deploy your code to Cloud based infrastructure (such as Azure or AWS) you will be using standard GitHub Actions to perform the deployment.
Most of these actions will require additional configuration with secrets to access your cloud provider and cloud accounts.

For example, when using the `deploy-azure.yml` file, you will need to define login secrets in order to deploy your code to Azure Infrastructure.
You can read about this process and the credentials required to do it in the [Azure Login Action](https://learn.microsoft.com/en-us/azure/app-service/deploy-github-actions?tabs=openid%2Caspnetcore).

For deploying to Azure, (using the `deploy-azure.yml` deployment workflow) you will also need to define the following secrets and variables in your GitHub deployment environment:
* `DEPLOY_AZURE_CLIENT_ID`
* `DEPLOY_AZURE_TENANT_ID`
* `DEPLOY_AZURE_SUBSCRIPTION_ID`

> These are used to automate the deployment to your Azure subscription.

then, you will need these to define the physical components to deploy your code to:
* `DEPLOY_APIHOST1_APP_NAME`
* `DEPLOY_WEBSITEHOST_APP_NAME`
* `DEPLOY_AZUREFUNCTIONS_APP_NAME`

> Note: the prefix `DEPLOY_` groups these settings as distinct from others in the GitHub project.

## Triggering a deployment

The deployment script (`deploy-azure.yml` and `deploy-aws.yml`) is triggered only by a push to the `main` branch, with a specific commit message, that includes the instruction `xxx`.

> This is a safety feature to prevent accidental deployments in the course of normal development of your product. This step should be very intentional in environment where you are not practicing "Continuous Deployment".