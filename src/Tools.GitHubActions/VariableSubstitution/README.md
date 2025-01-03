# AppSettings Variable Substitution

This action performs variable substitution of .NET projects (that use `appsettings.json` files) where you also store their variables and secrets in a GitHub repository. It also provides a mechanism to verify that all the required settings (defined in `appsettings.json` files) are defined in the GitHub repository before deployment.

> Unlike the well-known GitHub action [`microsoft/variable-substitution`](https://github.com/microsoft/variable-substitution), this action verifies the settings before substitution, and does not require the developer to modify the `build.yml` file when they add or remove settings in their code.
> This action will fail the build when there is a mismatch between what the `appsettings.json` files say, and what the GitHub repository has defined in its secrets and environment variables.
> This capability dramatically decreases the chances of deploying mis-configured software, as the code changes between deployments. In this way, this action is a safeguard for deployment.

## How It Works

This action scans for a set of `appsettings.json` files across the current repository (using a glob pattern filter).

For each settings file found, it will:
1. Verify that all settings that are referenced in the `Deploy -> Required -> Keys` sections, have corresponding variables defined in the GitHub secrets or in GitHub environment variables of the GitHub repository. This ensures that new settings that are designated as "required", have been correctly set up in the GitHub repository  before deployment of the software. It is up to the developer to define what is "required" and what is not "required". 
2. Substitutes the values of all settings of all `appsettings.json` files, with the values of all secrets and environment variables stored in the GitHub project (for the specified environment of the build). These files are then rewritten back to the build artifacts, for deployment.

## Set up

This action relies on the source code (i.e., `appsettings.json` files) declaring what variables/secrets must be present in the GitHub repository, before deployment. This is achieved with the definition of a `Deploy -> Required -> Keys` section of the `appsettings.json` file for each deployable host.

This action also relies on the variables/secrets being defined in the GitHub project for a specific environment, using a naming convention that can automatically match the variables in the `appsettings.json` files.

### Source Code

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

### GitHub Project

You define your variables and secrets in the GitHub project, either in the `Settings` -> `Secrets and variables` section, or in the `Settings` -> `Environments` -> `Secrets` section of a specific environment.

By default, GitHub suggests you define the name of your variable or secret in uppercase, using underscores to separate words. However, settings in `appsettings.json` have a very different naming convention. A mapping between the two is assumed by this action.

To make the variable substitution work correctly, you must define all the variables and secrets in the GitHub project using the same names as the keys in the `appsettings.json` files, and replacing any `:`, `-` and `.` characters with an underscore `_` character. Where all the characters of the variable/secret name are in uppercase (no mixed-casing).

For example, in the `appsettings.json` file we might have the following settings:
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

**Required**. A comma-delimited list of glob patterns that identify all the `appsettings.json` files to be processed, relative to the root of the repository. Default `**/appsettings.json`.

### `secrets`

**Required**. This must have this specific value in the YML file: `${{ toJSON(secrets)}}`.

> Note: Due to limitations imposed by GitHub (which are very sensible), it is not otherwise possible for this action to read the actual secrets from the GitHub repository. 

### `variables`

**Required**. This must have this specific value in the YML file: `${{ toJSON(vars)}}`.

## Outputs

None.

All variables, in all files found by the `files` input, will be substituted with the values of the variables/secrets found in the GitHub repository, for the specified environment.

## Example usage

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
```

> Note: That you should specify the name of the deployment environment that you have set up in your GitHub project, on the job itself. When you do, all secrets and variables from that environment, plus those form the GitHub repository (plus those from your GitHub organization).
