# Deployment of SaaStack

This document details the basic steps required to deploy your software into a production environment.

> Your production environment might be in the cloud or on premise. We assume the cloud, and we assume either Azure, AWS or GC.
>
> The deployment process will be similar in either case, except for the tools you choose to perform the deployment.

By default, deployment is assumed to take place from a GitHub repository using GitHub Actions, from a build that is triggered with a specific commit message. Other mechanisms are possible too.

> You can use any CI/CD tool you prefer, many of the steps below will be similar, but will differ based on the toolset you use.

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
  * An Azure FunctionsHost for the functions that monitor queues and message buses.
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
* `DEPLOY_AZURE_CREDENTIALS`

> These are used to automate the deployment to your Azure subscription.

then, you will need these to define the physical components to deploy your code to:
* `DEPLOY_APIHOST1_APP_NAME`
* `DEPLOY_WEBSITEHOST_APP_NAME`
* `DEPLOY_AZUREFUNCTIONS_APP_NAME`

> Note: the prefix `DEPLOY_` groups these settings as distinct from others in the GitHub project.

## Triggering a deployment

The deployment script (`deploy-azure.yml` and `deploy-aws.yml`) are both triggered manually (by a human) on the `main` branch

> See the GitHub event [workflow_dispatch](https://docs.github.com/en/actions/writing-workflows/choosing-when-your-workflow-runs/events-that-trigger-workflows#workflow_dispatch).

> This is a safety feature to prevent accidental deployments in the course of normal development of your product. This step should be very intentional in environment where you are not practicing "Continuous Deployment".

## Initial Deployment

By default, we assume that before deployment of the software, you have already (manually/automated) the creation of the actual target infrastructure, in your preferred cloud environment. (i.e, in Azure, AWS or GC).

When the automated deployment step is run, it is simply deploying new software packages to existing running infrastructure components.

> However, it is possible, to create a deployment process where the infrastructure itself is re-provisioned every time a deployment occurs. You can do this with tools like Terraform. However, this is not the default, and will not be discussed here.

### Build the initial infrastructure in Azure

![](images/Deployment-Azure.png)

To build out an initial infrastructure in Azure, we can use an ARM template like that found in [Azure-Seed.json](../iac/Azure/ARM/Azure-Seed.json).

> This template can be easily customized, and run once to create your initial infrastructure in Azure.
>
> It has made some low cost choices to get started with.

Open the template, and edit the values you see the `parameters` section, at the start of the file

> These are essentially the names of the resource that will be created in your resource group in you Azure subscription, and you may want to change them to match your product

1. Determine the name of your new resource group in Azure. This will be referenced in the commands below as `<resourcegroupname>`

2. To run this template, and deploy your new resource group, use the following commands:

* `az login` and sign in to your Azure subscription. Your subscription ID will be listed.
* `az account set --subscription <subscriptionid>`
* `az group create --name <resourcegroupname> --location 'Central US'` make sure to 
* `az deployment group create --name saastack-initial --resource-group <resourcegroupname> --template-file '../iac/Azure/ARM/Azure-Seed.json'`

  * >  You will be prompted for the admin username and password for the SQL server database (SQL authentication).


### Deployment credentials

We now need to create a Service Principal to retrieve credentials, and the build pipeline will use these credentials to perform the automated deployment.

```powershell
az ad sp create-for-rbac --name "saastack" --role contributor --scopes /subscriptions/<subscriptionid>/resourceGroups/<resourcegroupname> --json-auth
```

* where, `<subscriptionid>` is your azure subscription id

* where `<resourcegroupname>` is the name of the resource group you created

> Warning: this service principal gives access to all the resources in the specific resource group

This command will return a response as a block of JSON, like this:

```json
{
  "clientId": "a5c33a65-2773-4215-a3b2-d347a83fd094",
  "clientSecret": "r~E8S~-acDDM8tFpLLy.klcEW221HxucBCzcxcYT",
  "subscriptionId": "43d46a07-e36e-448c-af7e-e2b02366cc9b",
  "tenantId": "c979adb7-5649-468e-b2e8-a53891a07cf9",
  "activeDirectoryEndpointUrl": "https://login.microsoftonline.com",
  "resourceManagerEndpointUrl": "https://management.azure.com/",
  "activeDirectoryGraphResourceId": "https://graph.windows.net/",
  "sqlManagementEndpointUrl": "https://management.core.windows.net:8443/",
  "galleryEndpointUrl": "https://gallery.azure.com/",
  "managementEndpointUrl": "https://management.core.windows.net/"
}
```

Now, you will copy and paste that whole output text into a GitHub secret called: `DEPLOY_AZURE_CREDENTIALS`, either at the GitHub repository level, or as a secret into the GitHub deployment environment you are using.

### Deployment variables and secrets

Now that your Azure environment is provisioned, you need to update the following variables and secrets in your GitHub Project.

Assign these GitHub variables (or secrets) in your deployment environment, depending on the technology adapters you are using :

* `APPLICATIONINSIGHTS_CONNECTIONSTRING` (read from Azure Portal: AppInsights -> Properties -> Connection String)
* `APPLICATIONSERVICES_PERSISTENCE_AZURESERVICEBUS_CONNECTIONSTRING` (read from Azure Portal: ServiceBus -> Shared Access Policies -> RootManageSharedAccessKey -> Primary Connection String)
* `APPLICATIONSERVICES_PERSISTENCE_AZURESTORAGEACCOUNT_ACCOUNTKEY` (read from Azure Portal: Storage Account -> Access keys -> key1)
* `APPLICATIONSERVICES_PERSISTENCE_SQLSERVER_DBCREDENTIALS` (as defined in initial setup, in format: `User Id=<USERNAME>;Password=<PASSWORD>`)
* `APPLICATIONSERVICES_PERSISTENCE_AZURESTORAGEACCOUNT_ACCOUNTNAME` (as defined in initial setup)
* `APPLICATIONSERVICES_PERSISTENCE_SQLSERVER_DBSERVERNAME` (as defined in initial setup)
* `APPLICATIONSERVICES_PERSISTENCE_SQLSERVER_DBNAME` (as defined in initial setup)
* `DEPLOY_APIHOST1_APP_NAME` (as defined in initial setup)
* `DEPLOY_WEBSITEHOST_APP_NAME` (as defined in initial setup)
* `DEPLOY_AZUREFUNCTIONS_APP_NAME` (as defined in initial setup)
* `HOSTS_ALLOWEDCORSORIGINS` (as defined in initial setup, in format: `https://<DEPLOY_WEBSITEHOST_APP_NAME>.azurewebsites.com`)
* `HOSTS_ANCILLARYAPI_BASEURL` (as defined in initial setup, in format: `https://<DEPLOY_APIHOST1_APP_NAME>.azurewebsites.com`)
* `HOSTS_APIHOST1_BASEURL` (as defined in initial setup, in format: `https://<DEPLOY_APIHOST1_APP_NAME>.azurewebsites.com`)
* `HOSTS_IDENTITYAPI_BASEURL` (as defined in initial setup, in format: `https://<DEPLOY_APIHOST1_APP_NAME>.azurewebsites.com`)
* `HOSTS_IMAGESAPI_BASEURL` (as defined in initial setup, in format: `https://<DEPLOY_APIHOST1_APP_NAME>.azurewebsites.com`)
* `HOSTS_WEBSITEHOST_BASEURL` (as defined in initial setup, in format: `https://<DEPLOY_WEBSITEHOST_APP_NAME>.azurewebsites.com`)

### Initialize SQL Database

Your database has been created, but it has no schema at this point. You will need to initialize the schema.

Using your favorite database tool, connect to the database in Azure.

> By default, your Azure SQL database is protected by a firewall.
>
> In order to connect your local machine to Azure, you will need to set a firewall rule in the Azure Portal to allow access from your IP address.
>
> Go to Azure Portal, then for the SQL database, in the Overview -> Set server firewall, then in the firewall rules, select "Add your client IPv4 address", and hit Save

Once you have access to your database, you can write the database schema files located at: `../iac/Azure/SQLServer`.

For each of these files, (in no particular order) edit the file, change the name of the database on the first line, and execute the entire file.

### Deploy your build

Now that your cloud infrastructure is up and running, its time to trigger a build and deploy the code to your infrastructure.