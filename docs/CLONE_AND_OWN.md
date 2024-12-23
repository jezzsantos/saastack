# Clone and Own

Make it yours

## Copy or Fork

You have a choice; you can either fork this repo on GitHub, or you can copy this template and start your product from scratch.

> Forking an open-source project is one way to get the code, but it's designed primarily for cases where you expect to modify the code and contribute it back to the original project, which you may or may not want to do at some point in time later.

For most cases, we recommend the straightforward approach of copying the template and getting started. This is a simple and effective way to begin your new product.

## Make it Yours

After copying the code to your local machine, there are a number of changes you will want to make to it so it becomes your own.

In general, these are, at least, the kinds of things you will want to change.

1. You will want to change the codebase's name from `SaaStack` (and all the related assets) to the name of your product or business.
2. You will want to delete the open-source project artifacts, like the `CODE_OF_CONDUCT.md`, and `LICENSE` and other files that will no longer apply to your product.
3. You will choose either AWS or Azure as your cloud provider and delete the parts of the template related to the other option.
4. You will want to sort out your build pipeline for the codebase.
5. You may want to customize the `README.md` further for your product team.

## 1. Rename files and references to SaaStack

You are likely to have a specific name of your company/venture/project. You might also have a different name for your product (or project).

Your project might be hosted in GitHub, or it might be hosted in a private company repository, like: GitLab/AzureDevOps, etc

Either way, you are going to need to make some changes to all places in the code where you see any of the following terms/words/references.

### Names

The terms `saastack` and `SaaStack` (case-sensitive) are used in many places throughout the various files of this template, and they can mean different things in different places.
To make this code entirely your own, you will need to use a good "Find and Replace Tool" to find all locations of these words and then make suitable replacements.

> Make sure to replace both forms of this word (case-sensitive)

Pay special attention to each of the locations; they don't all have the same meaning.

### Author Details

In the `src/GlobalAssemblyInfo.cs` file, you will find basic information about the codebase that you will need to make your own.

Make sure you populate this correctly with your company/venture details.

For example,

```c#
// Shared assembly information 
[assembly: AssemblyProduct("SaaStack")]
[assembly: AssemblyDescription("A comprehensive codebase template for real-world, fully featured SaaS web products. On the .NET platform")]
[assembly: AssemblyConfiguration("")]
[assembly: AssemblyCompany("jezzsantos")]
[assembly: AssemblyCopyright("Copyright 2023, Jezz Santos")]
[assembly: AssemblyTrademark("")]
[assembly: AssemblyCulture("")]
[assembly: ComVisible(false)]

// Shared release information
[assembly: AssemblyVersion("1.0.0.0")]
[assembly: AssemblyFileVersion("1.0.0.0")]
[assembly: AssemblyInformationalVersion("1.0.0")]
```

### Repository Details

In the `src/Directory.Build.props` file; you will find publishing information for any Nuget packages that you might create from this codebase.

For example,

```c#
    <PropertyGroup>
        <RepositoryUrl>https://github.com/jezzsantos/saastack</RepositoryUrl>
        <PackageProjectUrl>https://github.com/jezzsantos/saastack</PackageProjectUrl>
        <RepositoryType>GitHub</RepositoryType>
        <PackageReleaseNotes>https://github.com/jezzsantos/saastack/blob/main/README.md</PackageReleaseNotes>
        <RequireLicenseAcceptance>false</RequireLicenseAcceptance>
    </PropertyGroup>
```

Make sure you correctly replace this information with your new project and its details.

## 2. Delete open source project artifacts

You may need to delete a bunch of the OSS project files (included in this project), that really only make sense if you are publishing your product/project code in another OSS project. Otherwise, you might just be best to remove them.

> By all means, you should take what you want from them, for your own needs.

OSS Files to delete:

* `README.md` (leave the `README_DERIVATIVE.md` for later)
* `CONTRIBUTING.md` and `CODE_OF_CONDUCT.md` (replace/modify, you are publishing as another OSS project)
* `LICENSE` (replace, if you are publishing as another OSS project)

## 3. Choose your cloud provider

You are either planning to deploy your code to Azure or AWS (in some rarer cases, perhaps use both).

SaaStack has some projects and some code specific to these cloud providers. You will need to decide what to keep and what to delete, as keeping this code is unnecessary if you are not using it.

Consider the following projects/components:

* AWS:
   * `AWSLambdas.Api.WorkerHost` project
   * `Infrastructure.Persistence.AWS` project
   * `appsettings.AWS.json` (in the projects: `ApiHost`, `WebsiteHost` and `TestingStubApiHost`)
   * `AWS` folder in the `Infrastructure.Persistence.Shared.IntegrationTests` project
   * `AWSLocalStackEmulator.cs` in the `IntegrationTesting.Persistence.Common` project
   * `AWSCloudWatchCrashReporter` and `AWSCloudWatchMetricReporter` in the `Infrastructure.Common` project
   * Any `AWS.*` nuget packages used in any project of the solution
* Azure:
   * `AzureFunctions.Api.WorkerHost` project
   * `Infrastructure.Persistence.Azure` project
   * `appsettings.Azure.json` (in the projects: `ApiHost`, `WebsiteHost,` and `TestingStubApiHost`)
   * `Azure` folder in the `Infrastructure.Persistence.Shared.IntegrationTests` project
   * `AzuriteStorageEmulator.cs` in the `IntegrationTesting.Persistence.Common` project
   * `ApplicationInsightsCrashReporter` and `ApplicationInsightsMetricReporter` in the `Infrastructure.Common` project
   * The `tools/azurite` folder
   * Any `Microsoft.Azure` or `Azure.*` nuget packages used in any project of the solution.

Finally, scan the code for the occurrence of the symbols `HOSTEDONAZURE` and `HOSTEDONAZURE` and delete the relevant parts of the code.

#### README_DERIVATIVE.md

The `README_DERIVATIVE.md` file will now need to be renamed to `README.md`  to replace the original one for the SaaStack project.

Scan the sections of this file, and remove the sections referring to Azure or AWS that you don't want.

## 4. Build your pipeline

The last step to make this codebase your own is to set up your own build pipeline.

Take a look at the `.github/workflows/build.yml` and use that to perform the very same tasks in your build pipeline.

## 5. Customize other things

### Example Subdomains

We have deliberately included 2 standalone subdomains to be used as examples for your dev team to learn and adopt the common patterns in this codebase. They are the `Cars` and `Bookings` subdomain. They demonstrate many of the patterns you are adopting.
Since both `Cars` and `Bookings` are both modules, they are independent of all the other subdomains in the code (by design of course).

We recommend that you do not delete them from your codebase for at least the first few months, and instead leave them there and guide your team to use them to learn the patterns (i.e., shamelessly copy and paste from them) until your team is proficient at applying all the same patterns. Then delete them from your codebase.

> The is very little penalty or cost of keeping them around as useful examples

### Documentation

The documentation at `docs` is intended for your team to continue with and adapt and modify and evolve as they go.

> You can, of course, choose to delete it or include it in your codebase (as there will always be a copy online at the SaaStack project). But your team will not be moving it forward in that project, nor will they adopt the ADRs in it.

### Coding Standards

There are a bunch of coding standards and rules in the `SaaStack.sln.DotSettings` file that you may want to adopt or change for your team.

Rider settings are the best place to manage those.

You can change them all before you publish your codebase, or tackle them as you go for your team.

> They are pretty close to defaults for most teams.

## 6. Cleanup

Delete this file `CLONE_AND_Own.md`
