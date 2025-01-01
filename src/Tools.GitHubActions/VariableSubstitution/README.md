# AppSettings Variable Substitution

This action performs variable substitution of .NET projects (that use `appsettings.json` files) where you also store their variables and secrets in a GitHub repository.

> Unlike the well-known GitHub action [`microsoft/variable-substitution`](https://github.com/microsoft/variable-substitution), this action verifies the settings before substitution, and does not require the developer to change the `build.yml` file when they add or remove settings in their code.
> This action will fail the build when there is a mismatch between what the code says, and what the GitHub repository says.
> This dramatically decreases the chances of deploying mis-configured software, as the code changes.   

## How It Works

This action scans for a set of `appsettings.json` files in the current repository (using a glob filter), and for each found file:
1. Verifies that all variables that are referenced in the `Required` sections, have corresponding variables in the  GitHub Secrets or in GitHub variables of the repository. This is to ensure that new settings that are designated as "required", have been correctly set up in the GitHub repository first, before deployment.
2. Substitutes the values of the variables and secrets stored in the GitHub project for the specified environment, into all the `appsettings.json` files.

## Set up

This action relies on the source code declaring what variables must be present in the GitHub repository, before deployment.
This action also relies on the variables/secrets defined in the GitHub project, using a naming convention that can automatically match the variables in the `appsettings.json` files.

### Source Code

This action depends on the definition of a set of "required" settings defined in one of your `appsettings.json` files.

For example,

```json
{
  "Deploy": {
    "Notes": "Lists the required configuration keys that must be overwritten (by the GitHub configuration action) when we deploy this host",
    "Required": [
      {
        "description": "General settings from appsettings.json",
        "keys": [
          "ApplicationServices:Persistence:Kurrent:ConnectionString",
          "Hosts:WebsiteHost:BaseUrl"
        ]
      },
      {
        "description": "Azure specific settings from appsettings.Azure.json",
        "keys": [
          "ApplicationInsights:ConnectionString",
          "ApplicationServices:Persistence:SqlServer:DbServerName",
          "ApplicationServices:Persistence:SqlServer:DbCredentials",
          "ApplicationServices:Persistence:SqlServer:DbName"
        ]
      },
      {
        "description": "AWS specific settings from appsettings.AWS.json",
        "disabled": true,
        "keys": [
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
> Note: These definitions are assumed exist in your `appsettings.json` files. However, they can also exist in separate files, just used for deployment use. e.g., `appsettings.Deploy.json`.

> Note: without these definitions, this action just performs variable substitution, without any verification.

### GitHub Project

You define your variables and secrets in the GitHub project, either in the `Settings` -> `Secrets and variables` section, or in the `Settings` -> `Environments` -> `Secrets` section.

By default, GitHub suggests you define the name of your variable or secret in uppercase, using underscores to separate words.

To make the variable substitution work correctly, you must define all the variables and secrets in the GitHub project using the same names as the keys in the `appsettings.json` files, and replacing any `:`, `-` characters with an underscore `_` character. Where all the characters of the variable/secret name are in uppercase or lowercase (no mixed-casing).

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

> or the lowercase versions of these same names.

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
