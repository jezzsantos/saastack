# AppSettings Variable Substitution

This action performs variable substitution of .NET projects (that use `appsettings.json` files) and Javascript/NodeJS projects (that use `.env` files), where those projects are built using GitHub actions, also choose store their variables and secrets in a GitHub repository.

It also provides a mechanism to verify that settings designated "required" (by the developer) are also defined in the GitHub repository, before deployment.

> Unlike the well-known GitHub action [`microsoft/variable-substitution`](https://github.com/microsoft/variable-substitution), this action:
> 1. Verifies the settings before substitution, and 
> 2. Does not require the developer to modify their `build.yml` files with hardcoded mappings.

This action will fail the build when there is a mismatch between what the `appsettings.json/.env` files declare, and what the GitHub repository has defined in its secrets and environment variables.

> This capability dramatically decreases the chances of deploying mis-configured software, as the code changes between deployments. In this way, this action is a safeguard for deployment.

## How It Works

This action scans for a set of `appsettings.json` files, or `.env` files across the current repository (using a glob pattern filter).

For each settings file found:
    If `appsettings.json`, JSON file it will:
        1. Verify that all settings that are referenced in the `Deploy -> Required -> Keys` sections, have corresponding variables defined in the GitHub secrets or in GitHub environment variables of the GitHub repository. It is up to the developer to define what is "required" and what is not "required". 
        2. Substitutes the values of all settings of all scanned `appsettings.json` files, with the values of all secrets and environment variables stored in the GitHub project (for the specified environment of the build). These files are then rewritten back to the build artifacts, for deployment.
    If `.env` name-value pair file, it will:
        1. Verify that all the settings that require substitution of their values (i.e. use the substitution notation), have corresponding variables defined in the GitHub secrets or in GitHub environment variables of the GitHub repository.
        2. Substitutes the values of all settings of all scanned `.env` files, with the values of all secrets and environment variables stored in the GitHub project (for the specified environment of the build). These files are then rewritten back to the build artifacts, for deployment.


## Set up

This action relies on the source code (i.e., `appsettings.json` and `.env` files) declaring what variables/secrets must be present in the GitHub repository, before deployment. 

In `appsettings.json` files, this is achieved with the definition of an additional `Deploy -> Required -> Keys` section of the `appsettings.json` file for each deployable host.

In `.env` files, this is achieved by using the substitution notation, e.g., `<name>=#{VAR_NAME}` in the file.

This action also relies on the variables/secrets being defined in the GitHub project for a specific environment, using a naming convention that can automatically match the variables in the `appsettings.json`/`.env` files.

### Source Code

#### AppSettings.json files

This action depends on the definition of a set of `Deploy -> Required -> Keys` settings defined in one of your `appsettings.json` files.

For example,

```json
{
  "Deploy": {
    "Notes": "Lists the required configuration keys that must be overwritten (by the GitHub configuration action) when we deploy this host",
    "Required": [
      {
        "Description": "General settings from this appsettings.json",
        "Keys": [
          "ApplicationServices:Persistence:Kurrent:ConnectionString",
          "Hosts:WebsiteHost:BaseUrl"
        ]
      },
      {
        "Description": "Azure specific settings from appsettings.Azure.json",
        "Keys": [
          "ApplicationInsights:ConnectionString",
          "ApplicationServices:Persistence:SqlServer:DbServerName",
          "ApplicationServices:Persistence:SqlServer:DbCredentials",
          "ApplicationServices:Persistence:SqlServer:DbName"
        ]
      },
      {
        "Description": "AWS specific settings from appsettings.AWS.json",
        "Disabled": true,
        "Keys": [
          "ApplicationServices:Persistence:AWS:AccessKey",
          "ApplicationServices:Persistence:AWS:SecretKey",
          "ApplicationServices:Persistence:AWS:Region",
          "ApplicationServices:Persistence:AWS:BucketNamespace"
        ]
      }
    ]
  }
}
```

> Note: These definitions are assumed exist within your main `appsettings.json` file. Or they can also exist in separate files that are just used for deployment use. e.g., `appsettings.Deploy.json`.
> Note: Keys where `Disabled` is set to `true` will be ignored. By default, a section without the `Disabled` value will be enabled.
> Note: Without any of these "Keys" definitions, this action will just perform variable substitution, without any verification.

#### .env files

This action depends on using this notation for key-value pairs in the text file.

For example,

```dotenv
APPLICATIONINSIGHTS-CONNECTIONSTRING=#{APPLICATIONINSIGHTS_CONNECTIONSTRING}
WEBSITEHOSTBASEURL="#{WEBSITEHOSTBASEURL}"
```

### GitHub Project

You define your variables and secrets in the GitHub project, either in the `Settings` -> `Secrets and variables` section, or in the `Settings` -> `Environments` -> `Secrets` section of a specific environment.

By default, GitHub suggests you define the name of your variable or secret in uppercase, using underscores to separate words. However, settings in `appsettings.json` have a very different naming convention. A mapping between the two is assumed by this action.

To make the variable substitution work correctly, you must define all the variables and secrets in the GitHub project using the same names as the keys in the `appsettings.json` files, and replacing any `:`, `-` and `.` characters with an underscore `_` character. Where all the characters of the variable/secret name are in uppercase (no mixed-casing).

For example, in the `appsettings.json` files, we might have the following settings:
```json
{
  "ApplicationServices": {
    "Persistence": {
      "Kurrent": {
        "ConnectionString": "Server=.;Database=Kurrent-1;Trusted_Connection=True;"
      },
      "Kurrent-2": {
        "ConnectionString": "Server=.;Database=Kurrent-2;Trusted_Connection=True;"
      },
      "Kurrent-2_1": {
        "ConnectionString": "Server=.;Database=Kurrent-2.1;Trusted_Connection=True;"
      }
    }
  }
}
```

In the GitHub project, you would define the following variables or secrets:
- `APPLICATIONSERVICES_PERSISTENCE_KURRENT_CONNECTIONSTRING`
- `APPLICATIONSERVICES_PERSISTENCE_KURRENT_2_CONNECTIONSTRING`
- `APPLICATIONSERVICES_PERSISTENCE_KURRENT_2_1_CONNECTIONSTRING`

## Inputs

### `files`

**Required**. A comma-delimited list of glob patterns that identify all the `appsettings.json`/`.env` files to be processed, relative to the root of the repository. Default `**/appsettings.json`.

### `secrets`

**Required**. This must have this specific value in the YML file: `${{ toJSON(secrets)}}`.

> Note: Due to limitations imposed by GitHub (which are very sensible), it is not otherwise possible for this action to read the actual secrets from the GitHub repository. 

### `variables`

**Required**. This must have this specific value in the YML file: `${{ toJSON(vars)}}`.


### `warnOnAdditionalVars`

**Optional**. If set to `true`, the action will create a build warning if there are additional variables or secrets in the GitHub repository that are not substituted into any `appsettings.json`/`.env` files. Default `false`.

### `ignoreAdditionalVars`

**Optional**. A regular expression that matches any GitHub variables/secrets that should be ignored if `warnOnAdditionalVars` is `true`.

## Outputs

All variables, in all files found by the `files` input, will be substituted with the values of the variables/secrets found in the GitHub repository, for the specified environment.

Additional build warnings will be raised in these cases:
* In `appsettings.json` files, when a declared `Deploy -> Required -> Keys` key in the `appsettings.json` file is present in specific `appsettings.json` file.
* In all files, when a variable is substituted and the variable is both defined as a secret and a variable in the GitHub repository. In this case, the secret value will be preferred.

> These warnings cannot be suppressed.

## Example usage

For .NET host projects:

```yaml
jobs:
  build:
    runs-on: windows-latest
    environment: 'YourDeploymentEnvironment'
    steps:
      - name: Variable Substitution
        uses: ./src/Tools.GitHubActions/VariableSubstitution
        with:
          files: '**/appsettings.json'
          variables: ${{ toJSON(vars)}}
          secrets: ${{ toJSON(secrets)}}
          warnOnAdditionalVars: true
          ignoreAdditionalVars: '^DEPLOY_'
```

For JavaScript/NodeJS projects:

```yaml
jobs:
  build:
    runs-on: windows-latest
    environment: 'YourDeploymentEnvironment'
    steps:
      - name: Variable Substitution
        uses: ./src/Tools.GitHubActions/VariableSubstitution
        with:
          files: '**/WebsiteHost/**/.env.deploy'
          variables: ${{ toJSON(vars)}}
          secrets: ${{ toJSON(secrets)}}
          warnOnAdditionalVars: true
          ignoreAdditionalVars: '^DEPLOY_'
```

> Note: That you should specify the name of the deployment environment that you have set up in your GitHub project, on the job itself. When you do, all secrets and variables from that environment, plus those form the GitHub repository (plus those from your GitHub organization).