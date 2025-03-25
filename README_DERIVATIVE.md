# <YourProjectName>

> This is a starter template for your projects' README.md file, which you should rename to `README.md` (replacing the one from this template)

# Install Environment

## Mandatory Tools

You will need the following development tools to build, run, and test this project:

* Windows or macOS.
* JetBrains Rider (recommended) or Visual Studio
   * Note: Using JetBrains Rider is recommended since the codebase includes more comprehensive customized tooling (coding standards, live templates, etc.)
   * Note: If using Visual Studio, you will need to install the additional component `.NET Compiler Platform SDK` in order to run the built-in Roslyn source generators.
   * Note: if using Visual Studio, the built-in Roslyn analyzers will not work (due to .netstandard2.0 [restrictions between Visual Studio and Roslyn](https://learn.microsoft.com/en-us/dotnet/csharp/roslyn-sdk/source-generators-overview))

* Install the .NET8.0 SDK (specifically version 8.0.6). Available for [Windows Download](https://dotnet.microsoft.com/en-us/download/dotnet/thank-you/sdk-8.0.302-windows-x64-installer)
* Install NodeJs (22.12.0 LTS or later), available for [Download](https://nodejs.org/en/download/)

> We have ensured that you won't need any other infrastructure running on your local machine (i.e., a Microsoft SQLServer database) unless you want to run infrastructure-specific integration tests. See [Optional Local Infrastructure](#Optional-Local-Infrastructure) below.

# Setup Environment

## Build The API

* Build the solution in Rider
* OR `dotnet build src/SaaStack.sln` in the terminal

## Memory Allocation

In Rider, 'Search Everywhere' for the action `change memory settings`, and set `8000M` of memory, then restart Rider.

## HTTPS Certificate

Open a terminal and run: `dotnet dev-certs https --trust`

## Install the Project Templates

In Rider, right-click on the solution node in the Explorer, and choose Add -> New Project.

In the bottom left corner, click the link "Manage Templates"

Click "Install Template..." and select the directory: `src/Tools.Templates/AnyProject`

Repeat for these directories:

* `src/Tools.Templates/HostProject`
* `src/Tools.Templates/InfrastructureProject`
* `src/Tools.Templates/IntegrationTestProject`
* `src/Tools.Templates/UnitTestProject`

Then cancel the dialog box.

## Azure Functions Host

Only if you are deploying your product to Azure.
(Delete this section otherwise)

For security, and to ensure the Azure Functions can run when running locally, you need to create your own version of `local.settings.json`.

In the `AzureFunctions.Api.WorkersHost` project:

1. Create a new file called `local.settings.json`
2. Copy the following JSON:
   ```json
   {
      "IsEncrypted": false,
      "Values": {
         "DebugMode": true,
         "AzureWebJobsStorage": "UseDevelopmentStorage=true",
         "ServiceBusConnection": "",
         "FUNCTIONS_WORKER_RUNTIME": "dotnet-isolated",
         "APPINSIGHTS_INSTRUMENTATIONKEY": "",
         "AZURE_TENANT_ID": "",
         "AZURE_CLIENT_ID": "",
         "AZURE_CLIENT_SECRET": "",
         "WEBSITE_SITE_NAME": "",
         "WEBSITE_OWNER_NAME": "",
         "WEBSITE_RESOURCE_GROUP": ""
      }
   }
   ```
3. Create a new file called `appsettings.local.json`
4. Leave the content blank for now

> DO NOT add these two files to source control!

## Optional Local Infrastructure

You only need the tools below if you want to either:
 - Work against alternative infrastructure during local development. For example `AzureSqlServerStore` instead of `LocalMachineJsonFileStore`.
 - Run specific `Integration.Persistence` tests for the persistence technology adapters you need to use in your codebase. 
   - We would normally run these storage integration tests in CI periodically.

If you are running the `Integration.Persistence` integration tests, Docker will be used to fire up the relevant infrastructure components automatically for you. 

### Docker

> You only need to perform this step once

You will need to install Docker on your local machine to run certain kinds of integration tests against certain local/cloud infrastructure components.

For example, all `Integration.Persistence` tests.

Install [Docker Desktop](https://docs.docker.com/desktop/)

> We will be using [TestContainers](https://dotnet.testcontainers.org/) to create containers specifically for the tests.

### MSSQL Service

The docker `tools/compose.yml` file defines a service for MSSQL.

You can use your IDE to launch it, or alternatively on the command line:

```shell
docker compose up -d mssql
```

### Azurite (Azure Storage Emulator)

The docker `tools/compose.yml` file defines a service for Azurite.

You can use your IDE to launch it, or alternatively on the command line:

```shell
docker compose up -d azurite
```

### LocalStack (AWS Emulator)

Only if you are deploying your product to AWS
(Delete this section otherwise)

The docker `tools/compose.yml` file defines a service for localstack.

You can use your IDE to launch it, or alternatively on the command line:

```shell
docker compose up -d localstack
```

### External Adapter Integration Testing

> You only need to perform this step once, prior to running any of the `Integration.External` tests against 3rd party adapters (e.g., Flagsmith, Twilio, etc.)

In the `Infrastructure.External.IntegrationTests` project, create a new file called `appsettings.Testing.local.json` and fill out the empty placeholders you see in `appsettings.TestingOnly.json` with values from service accounts that you have created for testing those 3rd party services.

> DO NOT add this file to source control!

## Build the Website

* `cd src/WebsiteHost/ClientApp`
* `npm install`
* `npm run build`

> Note: As a result of this build step you should see new bundle file (e.g. `0123456789abcdef.bundle.js`) appear in the `wwwroot` folder. This file should never be added to source control.

# Build & Deploy

When pushed, all branches will be built and tested with GitHub actions

# Local Development, Debugging and Manual Testing

## Committing and Pushing Code

1. Ensure that the solution contains `0` warnings and `0` compile errors.

   ![Warnings](docs/images/Rider-Warnings.png)

   > Note: Warnings are generated by the IDE, plugins and by Roslyn code analysis rules that run against the solution.

2. Run these tests:

   In Rider, run all C# tests with Category= `Unit`, `Unit.Tooling`, `Unit.Architecture`, `Integration.API` and `Integration.Website`

   > Note: Use `Group By > Category` in Rider's unit test explorer to view these three categories easily.

   OR, in a terminal:

   - `dotnet test --filter:"Category=Unit|Category=Unit.Tooling|Category=Unit.Architecture" src/SaaStack.sln`

   - `dotnet test --filter:"Category=Integration.API|Category=Integration.Website" src/SaaStack.sln`

3. Configure your "Commit " window to select the "Cleanup with 'SaaStack Full' profile".

   ![Automatic Code Cleanup](docs/images/Rider-Commit-Settings.png)

## Changing IDE Settings

This solution contains comprehensive code formatting, and error-checking settings in the team-shared settings file `src/SaaStack.sln.dotSettings` that contains numerous code style rules, 'Live Templates', 'File templates', and other important settings that we need to share across the team for consistency.

When changing any settings in Rider, make sure you save them for the whole team (team-shared), then add the `SaaStack.sln.dotSettings` file to your next commit to save those rules for the rest of your team.

![Settings](docs/images/Rider-Settings.png)

## Stubs and Fakes

We use various stubs/fakes (whichever definition you like) in concert with configuration settings (`appsettings.json`) and with `#if TESTINGONLY` sections to create different environments for testing and for production.

There are 3 environments you need to be aware of and how they differ in their dependencies and configuration:

1. Local (manual) testing (aka F5 debugging) - `Debug` or `Release`
2. Automated integration testing - `Debug` or `Release`
3. Production (and/or Staging etc.) - `ReleaseForDeploy`

> Note: In all cases, in all environments, there should NEVER be any production settings nor **secrets** in any configuration file (i.e.  `appsettings.json` ) anywhere in this codebase! These production settings and secrets should only be defined in the CD pipeline, and replaced when a production build is packaged and deployed.

### Production Builds

In production builds, we build and deploy the code in the `ReleaseForDeploy` build configuration.

> Note: In this build configuration, certain testing stubs, certain testing endpoints, and certain hardcoded testing values and functions are compiled out of the code (e.g. behind the `#if TESTINGONLY` conditional compilation variable).
>
> We absolutely need to do this because these specific testing code pieces should never exist in the production codebase and may expose security vulnerabilities and exposures we simply don't want in production environments.

The various 3rd party *adapters* we need in production (e.g., `SendGridHttpServiceClient` and the `AzureSqlServerStore`)
will be configured in the DI containers (of `Program.cs` of the `ApiHost1` project, and in the modules of each subdomain) to use code to talk to real 3rd party APIs and will be configured with specific production settings in the `appsettings.json` file (overwritten by your CD server).

These are the real 3rd party public API adapters, which, if used with production settings, in local CI environments, or in automated testing environments, may incur financial service charges, trigger rate-limiting quotas, and/or pollute or corrupt real customer data!

> Note: This should never happen by accident, but read and follow the next 2 sections to avoid the possibility of this happening. We have designed several safeguards in place that should make this impossible (albeit without working around it intentionally).

You will notice that in the production build (`ReleaseForDeploy`), we have configured the code:

* By injecting the `AzureSqlServerStore` as the primary `IDataStore`.
* By injecting various other dependencies according to the current value of the `$(HostingPlatform)` MS build property (e.g., `HOSTEDONAZURE`).

> Note: that many of the other technology adapters (e.g., `SendGridHttpServiceClient`) will not need to be explicitly configured in the DI container (for specific build flavors), that is because these adapters can be configured to point to local stubs instead of pointing to production environments.

### Automated Integration Testing

> On the CI server, integration testing is run in the `Release` build configuration, which permits the inclusion of `TESTINGONLY` code in the compilation necessary for integration testing.
> Integration testing on your local machine should be done in the `Debug` build configuration. The only difference between `Debug` and `Release` in practice is that there are some compiler optimizations configured in `Release`, which are closer to code in Production.

In automated integration testing (executed on both your local machine and on the CI build server), we run the APIs you are testing in their original production DI configuration defined in the `Program.cs` file (in the respective `ApiHost` project of the API code you are testing).

That DI configuration is modified slightly to swap out the 3rd party adapters for that `ApiHost` so that we can program the 3rd party adapters to behave the way the tests need them to (and to query them for certain interactions).

This is what we modify and how:

* We run this API production DI code in-process in a Kestrel web server in the same process as your Integration Tests (e.g., in the process of `xunit.console.exe`, not as a separate process).
* We replace the `appsettings.json` file with the one in your integration testing project

  > Which should never ever contain ANY production settings or secrets!

* We then manipulate the DI container (seen in the constructor of your integration testing project) and replace certain dependencies (e.g., the 3rd party adapters like: `IUserNotificationsService,` etc.) with their respective hardcoded Stubbed equivalents (found in the `Stubs` directory of your integration testing project).

> Note: These integration testing stubs will likely use no configuration settings from `appsettings.json` as their responses and behavior are hardcoded/canned in the hardcoded classes of the integration test project.

You may wish to modify these stubs to add the ability to query them to ensure they are called in the right ways in testing.

### Local Manual Testing

> Note: When you are manual testing (like using F5 debugging), make sure that you compile the code and run it in `Debug` (or `Release`) build configuration that will include all code marked up by `#if TESTINGONLY` compilation variable.

When you run any of the `ApiHost` projects in this solution in your local environment, you are starting that `ApiHost` project at a specific IP address, which starts a separate Kestrel server in *external* processes of its own (e.g., `ApiHost.exe` for the API).

In local testing, all external services (i.e., SendGrid, Unleash, etc.) should be directed (via config) to point to the local Stubs `TestingStubApiHost,` which will respond with specific fixed responses to any calls to these external services.

The goal of this testing strategy is to make it possible to run locally without connecting to any real live services over the internet.

> Note: When you are doing manual testing on your local machine, either through the browser with PostMan, or with any other tools, you are actually running the code in production configuration as far as the adapters that DI injects into your code. However, most of these adapters will be using the configuration found in `appsettings.json` of the `ApiHost` project.

There will be numerous statements in the code using `#if TESTINGONLY` to determine which concrete dependencies are actually used in the `Debug` (or `Release`) configuration.

You will notice that in local debugging, we have switched out the currently configured `IDataStore` for the `LocalMachineFileDataStore`, so that you can do all your local debugging without a SQL database being available on your local machine.

The `LocalMachineFileDataStore` is configured to place your files in `Environment.SpecialFolder.LocalApplicationData`, which resolves to these folders:

* On Windows: `%appdata%`
* On macOS: `/home/<you>/.local/share`

## Running The Code

1. Build the code:

   - Rebuild the solution
   - OR `dotnet build src/SaaStack.sln` in the terminal

2. Run the backend:

   * In Rider, run the `AllHosts` compound configuration (runs the `ApiHost1` server and the `TestingStubApiHost`)

3. Access the API on `https://localhost:5001`

#### Troubleshooting

Sometimes (especially on macOS), after manually testing, the processes do not shut down properly, leaving ports: `5001` and `5656` occupied. This then throws an exception when you try to run again later.

The message looks something like this:

```
System.IO.IOException: Failed to bind to address https://127.0.0.1:5656: address already in use.
```

To kill these processes:

* On Windows, `taskkill /f /im dotnet.exe`
* On macOS:
   * Find the processes: `lsof -Pni | grep "5001\|5101\|5656"`
   * Kill the processes: `kill -9 <processid>` where `<processid>` is the ID of the process in the list

   * Alternatively, in macOS:

      * Use `lsof -ti :[PORT]` and locate the PID of the process, e.g., `lsof -ti :5656`.
      * Open "Activity Monitor", locate the process with that PID, and stop that process.

## Testing The Code

### Everyday tests

Run all C# tests with Category= `Unit`, `Unit.Architecture`, `Integration.API` and `Integration.Website`

OR, in a terminal:

- `dotnet test --filter:"Category=Unit|Category=Unit.Tooling|Category=Unit.Architecture" src/SaaStack.sln`

- `dotnet test --filter:"Category=Integration.API|Category=Integration.Website" src\SaaStack.sln`

> Note: All tests will be run in parallel in `Rider` or in `dotnet test`.

### Persistence Integration tests

These tests ensure that 3rd party persistence technology adapters that are used in production environments work correctly.

Only run these kinds of tests when the code in the persistence technology adapters changes.

These tests should NOT be run frequently and can be scheduled to run as part of a nightly/weekly build.

> Warning: These tests connect to and test real 3rd party systems in the cloud (usually across HTTP or some other protocol). Some of these tests require that you have the respective technology installed on your local machine (e.g., SQL Server Database).
>
> Warning: They may incur charges, or they may trigger rate-limiting policies on the accounts they are run against.

`dotnet test --filter:"Category=Integration.Persistence" src/SaaStack.sln` (requires installing the server persistence components listed at the top of this page)
> Note: If any of the `Integration.Persistence` category of tests fail, it is likely due to the fact that you don't have that technology installed on your local machine, or that you are not running your IDE as Administrator, and therefore cannot start/stop those local services without elevated permissions.

> Note: AWS infrastructure adapters require LocalStack to be running (in Docker) on your computer in order to work. (Run `localstack start`).

### External Integration tests

These tests ensure that 3rd party technology adapters that are used in production environments work correctly.

Only run these kinds of tests when the code in the technology adapters changes. These tests should not be run frequently and can be scheduled to run as part of a nightly/weekly build.

> Warning: These tests connect to and test real 3rd party systems in the cloud (usually across HTTP).
>
> Warning: They may incur charges, or they may trigger rate-limiting policies on the accounts they are run against.

`dotnet test --filter:"Category=Integration.External" src/SaaStack.sln` (requires internet access to external services)
`dotnet test --filter:"Category=Integration.External" src\SaaStack.sln` (requires internet access to external services)

# Upgrading .NET Version

If you need to upgrade the version of .NET for this codebase, do the following:
1. Download the new version of the .NET SDK from this page: https://dotnet.microsoft.com/en-us/download/dotnet
2. Run the installer for your OS. This will add a new version to your local machine, but it will not change anything.
3. Make a note of the version of the runtime, and the SDK (they are different numbers)
4. In `Directory.Build.props`, change the value of the `<RoslynTargetFramework>` variable to the version of the runtime.
5. In `RuntimeConstants.cs` change the value of both `DotNet.RuntimeVersion`, `Dotnet.SdkVersion` and `DotNet.Version`.
6. (Optional) If necessary (i.e. moving from .NET 8 to .NET 9) then find and replace the `<TargetFramework>net8.0</TargetFramework>` attribute in every `*.csproj` file in the solution.
7. Rebuild the solution

# Versioning the Code

> Note: We use the 2 dot [Semantic Versioning](https://semver.org/spec/v2.0.0.html) scheme.

The latest changes for this new version are recorded in [CHANGELOG.md](CHANGELOG.md), and they follow a [keep a changelog](https://keepachangelog.com/en/1.0.0/) convention.

> All assemblies and all hosts will share the same version number.
>
> We will be using a tool called [release-it](https://github.com/release-it/release-it) to update the version and changelog when creating new releases.

1. First, make sure that all changes are documented in the various sections of the `[Unreleased]` section of the [CHANGELOG.md](CHANGELOG.md)

2. Copy the new version number to `src/GlobalAssemblyInfo.cs`

    > For example:
    * `[assembly: AssemblyVersion("2.0.0.0")]`
    * `[assembly: AssemblyFileVersion("2.0.0.0")]`
    * `[assembly: AssemblyInformationalVersion("2.0.0")]`

3. Commit, tag, and push the new version changes.

> Note: Each build in CI will automatically append the last build number to the SemVer and update the version in `GlobalAssemblyInfo.cs`