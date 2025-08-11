# Create a Pattern Toolkit for creating new Subdomains

These are the instructions for creating a toolkit that will create the solution structure and initial classes for creating a SaaStack subdomain, that effectively performs the same manual steps in [Create a Subdomain Module](https://github.com/jezzsantos/saastack/blob/main/docs/how-to-guides/010-subdomain-module.md).

Essentially, the toolkit built here reliably automating those steps.

## Installation

In a Terminal:

* Install the tool called "automate": `dotnet tool install --global automate`. Latest version of the tool is [found here](https://github.com/jezzsantos/automate)
* Make sure to change to the  `src` directory of your codebase. 
  * i.e. the same folder as your Visual Studio solution file `src/SaaStack.sln`

Either:

1. Create the new pattern & toolkit  by following the steps below, or,
2. [Install and Use the toolkit](#create-a-new-subdomain)  and start using it to create new subdomains in your codebase.

# Create the Pattern & Toolkit

Commands for creating the new pattern, and the toolkit.

CLI reference and documentation is to be [found here](https://jezzsantos.github.io/automate/) 

## Create the Pattern

We start by establishing a new pattern, with a name, and an initial schema.

### The Subdomain

Our pattern will be modelling a single Subdomain, (core or generic).

Subdomains can be either "Core" or "Generic" (from a DDD perspective), and they can be tenanted or untenanted.

In a terminal, in the `src` directory of the repository: 

```cmd
automate create pattern SaaStackSubdomain --displayedas "A Subdomain" --describedas "A core subdomain of the code" 
automate edit add-attribute SubdomainName --isrequired
automate edit add-attribute "IsTenanted" --isrequired --isoftype "bool" --defaultvalueis "true"
automate edit add-attribute "SubdomainType" --isrequired --isoftype "string" --isoneof "Core;Generic" --defaultvalueis "Core"
```

#### Code Templates

Now, we need to add the following code templates for all the projects and code files that represent a whole subdomain in the codebase.

> We are going to reuse actual code from the samples already in the codebase. These will act as initial versions, and then later, we are going to customize each file with some special syntax, so that these files become templates that can be used to generate real files with the right values according to the attributes of a new subdomain.
>
> You can of course, start with blanks files and go from there, but this approach ensures we are harvesting actual code in the codebase as our templates. 

First, we start with the Infrastructure Layer projects, then the Application Layer projects, and finally the Domain Layer projects.

```cmd
automate edit add-codetemplate "CarsInfrastructure/CarsInfrastructure.csproj" --name InfrastructureProject
automate edit add-codetemplate "CarsInfrastructure/Resources.resx" --name InfrastructureResources
automate edit add-codetemplate "CarsInfrastructure/Resources.Designer.cs" --name InfrastructureResourcesDesigner
automate edit add-codetemplate "CarsInfrastructure/CarsModule.cs" --name SubModule
automate edit add-codetemplate "CarsInfrastructure/Api/Cars/CarsApi.cs" --name SubdomainApi
automate edit add-codetemplate "CarsInfrastructure/ApplicationServices/CarsInProcessServiceClient.cs" --name ServiceClient
automate edit add-codetemplate "Application.Services.Shared/ICarsService.cs" --name ApplicationService
automate edit add-codetemplate "CarsInfrastructure/Persistence/ReadModels/CarProjection.cs" --name Projection
automate edit add-codetemplate "CarsInfrastructure/Persistence/CarRepository.cs" --name Repository
automate edit add-codetemplate "CarsInfrastructure.UnitTests/CarsInfrastructure.UnitTests.csproj" --name InfrastructureUnitTestProject
automate edit add-codetemplate "CarsInfrastructure.IntegrationTests/CarsInfrastructure.IntegrationTests.csproj" --name InfrastructureIntegrationTestProject
automate edit add-codetemplate "CarsInfrastructure.IntegrationTests/appsettings.Testing.json" --name InfrastructureIntegrationTestAppSettings
automate edit add-codetemplate "CarsInfrastructure.IntegrationTests/CarsApiSpec.cs" --name InfrastructureIntegrationTestApiSpec


automate edit add-codetemplate "CarsApplication/CarsApplication.csproj" --name ApplicationProject
automate edit add-codetemplate "CarsApplication/CarsApplication.cs" --name ApplicationClass
automate edit add-codetemplate "CarsApplication/ICarsApplication.cs" --name ApplicationInterface
automate edit add-codetemplate "CarsApplication/Persistence/ICarRepository.cs" --name ApplicationRepository
automate edit add-codetemplate "CarsApplication/Persistence/ReadModels/Car.cs" --name ApplicationReadModel
automate edit add-codetemplate "CarsApplication.UnitTests/CarsApplication.UnitTests.csproj" --name ApplicationUnitTestProject
automate edit add-codetemplate "CarsApplication.UnitTests/CarsApplicationSpec.cs" --name ApplicationUnitTestClassSpec


automate edit add-codetemplate "Application.Resources.Shared/Car.cs" --name ApplicationResourceDto


automate edit add-codetemplate "CarsDomain/CarsDomain.csproj" --name DomainProject
automate edit add-codetemplate "CarsDomain/Resources.resx" --name DomainResources
automate edit add-codetemplate "CarsDomain/Resources.Designer.cs" --name DomainResourcesDesigner
automate edit add-codetemplate "CarsDomain/CarRoot.cs" --name DomainAggregate
automate edit add-codetemplate "CarsDomain/Events.cs" --name DomainEvents
automate edit add-codetemplate "CarsDomain/Validations.cs" --name DomainValidations
automate edit add-codetemplate "CarsDomain.UnitTests/CarsDomain.UnitTests.csproj" --name DomainUnitTestProject
automate edit add-codetemplate "CarsDomain.UnitTests/CarRootSpec.cs" --name DomainUnitTestAggregateSpec


automate edit add-codetemplate "Domain.Events.Shared/Cars/Created.cs" --name DomainCreationEvent
```

At this point, after running all those commands, you can see how the pattern in the toolkit is taking shape, by running this command:

```cmd
automate view pattern --all
```

> This should show you all the "Code Templates" you have configured above, and the schema of the pattern as well.

#### Automation

Now, we have to create the "commands" that will use all these code templates in the toolkit, that will eventually generate the code into a new codebase.

* There is usually at least one command per code-template above
  * This command tells the toolkit how to treat the code-template when you use it for the first time, and any time after that.
  * Commands can generate files in any location in your solution, and that location can be controlled by the attributes of the pattern.

> Don't worry just yet about the actual code we generate, we will be modifying the actual code after this step.

```cmd
automate edit add-codetemplate-command "InfrastructureProject" --isoneoff --targetpath "~/{{SubdomainName | string.pascalplural}}Infrastructure/{{SubdomainName | string.pascalplural}}Infrastructure.csproj"
automate edit add-codetemplate-command "InfrastructureResources" --isoneoff --targetpath "~/{{SubdomainName | string.pascalplural}}Infrastructure/Resources.resx"
automate edit add-codetemplate-command "InfrastructureResourcesDesigner" --isoneoff --targetpath "~/{{SubdomainName | string.pascalplural}}Infrastructure/Resources.Designer.cs"
automate edit add-codetemplate-command "SubModule" --isoneoff --targetpath "~/{{SubdomainName | string.pascalplural}}Infrastructure/{{SubdomainName | string.pascalplural}}Module.cs"
automate edit add-codetemplate-command "SubdomainApi" --isoneoff --targetpath "~/{{SubdomainName | string.pascalplural}}Infrastructure/Api/{{SubdomainName | string.pascalplural}}/{{SubdomainName | string.pascalplural}}Api.cs"
automate edit add-codetemplate-command "ServiceClient" --isoneoff --targetpath "~/{{SubdomainName | string.pascalplural}}Infrastructure/ApplicationServices/{{SubdomainName | string.pascalplural}}InProcessServiceClient.cs"
automate edit add-codetemplate-command "ApplicationService" --isoneoff --targetpath "~/Application.Services.Shared/I{{SubdomainName | string.pascalplural}}Service.cs"
automate edit add-codetemplate-command "Projection" --isoneoff --targetpath "~/{{SubdomainName | string.pascalplural}}Infrastructure/Persistence/ReadModels/{{SubdomainName | string.pascalsingular}}Projection.cs"
automate edit add-codetemplate-command "Repository" --isoneoff --targetpath "~/{{SubdomainName | string.pascalplural}}Infrastructure/Persistence/{{SubdomainName | string.pascalsingular}}Repository.cs"
automate edit add-codetemplate-command "InfrastructureUnitTestProject" --isoneoff --targetpath "~/{{SubdomainName | string.pascalplural}}Infrastructure.UnitTests/{{SubdomainName | string.pascalplural}}Infrastructure.UnitTests.csproj"
automate edit add-codetemplate-command "InfrastructureIntegrationTestProject" --isoneoff --targetpath "~/{{SubdomainName | string.pascalplural}}Infrastructure.IntegrationTests/{{SubdomainName | string.pascalplural}}Infrastructure.IntegrationTests.csproj"
automate edit add-codetemplate-command "InfrastructureIntegrationTestAppSettings" --isoneoff --targetpath "~/{{SubdomainName | string.pascalplural}}Infrastructure.IntegrationTests/appsettings.Testing.json"
automate edit add-codetemplate-command "InfrastructureIntegrationTestApiSpec" --isoneoff --targetpath "~/{{SubdomainName | string.pascalplural}}Infrastructure.IntegrationTests/{{SubdomainName | string.pascalplural}}ApiSpec.cs"


automate edit add-codetemplate-command "ApplicationProject" --isoneoff --targetpath "~/{{SubdomainName | string.pascalplural}}Application/{{SubdomainName | string.pascalplural}}Application.csproj"
automate edit add-codetemplate-command "ApplicationClass" --isoneoff --targetpath "~/{{SubdomainName | string.pascalplural}}Application/{{SubdomainName | string.pascalplural}}Application.cs"
automate edit add-codetemplate-command "ApplicationInterface" --isoneoff --targetpath "~/{{SubdomainName | string.pascalplural}}Application/I{{SubdomainName | string.pascalplural}}Application.cs"
automate edit add-codetemplate-command "ApplicationRepository" --isoneoff --targetpath "~/{{SubdomainName | string.pascalplural}}Application/Persistence/I{{SubdomainName | string.pascalsingular}}Repository.cs"
automate edit add-codetemplate-command "ApplicationReadModel" --isoneoff --targetpath "~/{{SubdomainName | string.pascalplural}}Application/Persistence/ReadModels/{{SubdomainName | string.pascalsingular}}.cs"
automate edit add-codetemplate-command "ApplicationUnitTestProject" --isoneoff --targetpath "~/{{SubdomainName | string.pascalplural}}Application.UnitTests/{{SubdomainName | string.pascalplural}}Application.UnitTests.csproj"
automate edit add-codetemplate-command "ApplicationUnitTestClassSpec" --isoneoff --targetpath "~/{{SubdomainName | string.pascalplural}}Application.UnitTests/{{SubdomainName | string.pascalplural}}ApplicationSpec.cs"


automate edit add-codetemplate-command "ApplicationResourceDto" --isoneoff --targetpath "~/Application.Resources.Shared/{{SubdomainName | string.pascalsingular}}.cs"


automate edit add-codetemplate-command "DomainProject" --isoneoff --targetpath "~/{{SubdomainName | string.pascalplural}}Domain/{{SubdomainName | string.pascalplural}}Domain.csproj"
automate edit add-codetemplate-command "DomainResources" --isoneoff --targetpath "~/{{SubdomainName | string.pascalplural}}Domain/Resources.resx"
automate edit add-codetemplate-command "DomainResourcesDesigner" --isoneoff --targetpath "~/{{SubdomainName | string.pascalplural}}Domain/Resources.Designer.cs"
automate edit add-codetemplate-command "DomainAggregate" --isoneoff --targetpath "~/{{SubdomainName | string.pascalplural}}Domain/{{SubdomainName | string.pascalsingular}}Root.cs"
automate edit add-codetemplate-command "DomainEvents" --isoneoff --targetpath "~/{{SubdomainName | string.pascalplural}}Domain/Events.cs"
automate edit add-codetemplate-command "DomainValidations" --isoneoff --targetpath "~/{{SubdomainName | string.pascalplural}}Domain/Validations.cs"
automate edit add-codetemplate-command "DomainUnitTestProject" --isoneoff --targetpath "~/{{SubdomainName | string.pascalplural}}Domain.UnitTests/{{SubdomainName | string.pascalplural}}Domain.UnitTests.csproj"
automate edit add-codetemplate-command "DomainUnitTestAggregateSpec" --isoneoff --targetpath "~/{{SubdomainName | string.pascalplural}}Domain.UnitTests/{{SubdomainName | string.pascalsingular}}RootSpec.cs"


automate edit add-codetemplate-command "DomainCreationEvent" --isoneoff --targetpath "~/Domain.Events.Shared/{{SubdomainName | string.pascalplural}}/Created.cs"



automate edit add-cli-command "dotnet" --arguments "sln add {{SubdomainName | string.pascalplural}}Infrastructure/{{SubdomainName | string.pascalplural}}Infrastructure.csproj {{SubdomainName | string.pascalplural}}Application/{{SubdomainName | string.pascalplural}}Application.csproj {{SubdomainName | string.pascalplural}}Domain/{{SubdomainName | string.pascalplural}}Domain.csproj --solution-folder Subdomains\{{SubdomainType}}\{{SubdomainName | string.pascalplural}}"
automate edit add-cli-command "dotnet" --arguments "sln add {{SubdomainName | string.pascalplural}}Infrastructure.IntegrationTests/{{SubdomainName | string.pascalplural}}Infrastructure.IntegrationTests.csproj {{SubdomainName | string.pascalplural}}Infrastructure.UnitTests/{{SubdomainName | string.pascalplural}}Infrastructure.UnitTests.csproj {{SubdomainName | string.pascalplural}}Application.UnitTests/{{SubdomainName | string.pascalplural}}Application.UnitTests.csproj  {{SubdomainName | string.pascalplural}}Domain.UnitTests/{{SubdomainName | string.pascalplural}}Domain.UnitTests.csproj --solution-folder Subdomains\{{SubdomainType}}\{{SubdomainName | string.pascalplural}}\Tests"

automate edit add-command-launchpoint "*" --name "Generate"
```

At this point, you have now wired up all the code templates to commands that generate code from them.

> There are 2 CLI commands, that basically add your new projects to the solution, in a certain location. Lastly, there is a launch point called "Generate" created to create the whole subdomain.

Now, if you run this command:

```cmd
automate view pattern --all
```

You will see the pattern all wired up.

### ServiceOperations

Each Subdomain, can have one or more APIs. We assume that there is just one API for now.

Each API, can have one or more "Service Operations".

> A Service operation is essentially an HTTP endpoint (or a minimal API).

An endpoint is defined by a request and a response, and has a route, and an HTTP method and some other attributes like authentication scheme and authorization rules, etc. Most service operations also require a request validator.

Run these commands to define a "Service Operation", as a child of the Subdomain. 

```cmd
automate edit add-collection "ServiceOperation" --displayedas "Operations" --describedas "The service operations of an API"
automate edit add-attribute Name --isrequired --aschildof "{SaaStackSubdomain.ServiceOperation}"
automate edit add-attribute Route --isrequired --aschildof "{SaaStackSubdomain.ServiceOperation}"
automate edit add-attribute Kind --isrequired --isoneof "POST;PUTPATCH;GET;SEARCH;DELETE" --defaultvalueis POST --aschildof "{SaaStackSubdomain.ServiceOperation}"
automate edit add-attribute IsAuthorized --isrequired --isoftype bool --defaultvalueis true --aschildof "{SaaStackSubdomain.ServiceOperation}"
automate edit add-attribute "IsTestingOnly" --isrequired --isoftype "bool" --defaultvalueis "false" --aschildof "{SaaStackSubdomain.ServiceOperation}"
```

#### Code Templates

A service operation is defined across several files.

```cmd
automate edit add-codetemplate "CarsInfrastructure/Api/Cars/RegisterCarRequestValidator.cs" --name Validator --aschildof "{SaaStackSubdomain.ServiceOperation}"
automate edit add-codetemplate "CarsInfrastructure.UnitTests/Api/Cars/RegisterCarRequestValidatorSpec.cs" --name ValidatorSpec --aschildof "{SaaStackSubdomain.ServiceOperation}"


automate edit add-codetemplate "Infrastructure.Web.Api.Operations.Shared/Cars/RegisterCarRequest.cs" --name Request --aschildof "{SaaStackSubdomain.ServiceOperation}"
automate edit add-codetemplate "Infrastructure.Web.Api.Operations.Shared/Cars/GetCarResponse.cs" --name Response --aschildof "{SaaStackSubdomain.ServiceOperation}"
```

#### Automation

```cmd
automate edit add-codetemplate-command "Validator" --isoneoff --aschildof "{SaaStackSubdomain.ServiceOperation}" --targetpath "~/{{Parent.SubdomainName | string.pascalplural}}Infrastructure/Api/{{Parent.SubdomainName | string.pascalplural}}/{{Name}}{{if (Kind==""SEARCH"")}}{{Parent.SubdomainName | string.pascalplural}}{{else}}{{Parent.SubdomainName | string.pascalsingular}}{{end}}RequestValidator.cs"
automate edit add-codetemplate-command "ValidatorSpec" --isoneoff --aschildof "{SaaStackSubdomain.ServiceOperation}" --targetpath "~/{{Parent.SubdomainName | string.pascalplural}}Infrastructure.UnitTests/Api/{{Parent.SubdomainName | string.pascalplural}}/{{Name}}{{if (Kind==""SEARCH"")}}{{Parent.SubdomainName | string.pascalplural}}{{else}}{{Parent.SubdomainName | string.pascalsingular}}{{end}}RequestValidatorSpec.cs"


automate edit add-codetemplate-command "Request" --isoneoff --aschildof "{SaaStackSubdomain.ServiceOperation}" --targetpath "~/Infrastructure.Web.Api.OPerations.Shared/{{Parent.SubdomainName | string.pascalplural}}/{{Name}}{{if (Kind==""SEARCH"")}}{{Parent.SubdomainName | string.pascalplural}}{{else}}{{Parent.SubdomainName | string.pascalsingular}}{{end}}Request.cs"
automate edit add-codetemplate-command "Response" --isoneoff --aschildof "{SaaStackSubdomain.ServiceOperation}" --targetpath "~/Infrastructure.Web.Api.OPerations.Shared/{{Parent.SubdomainName | string.pascalplural}}/{{Name}}{{if (Kind==""SEARCH"")}}{{Parent.SubdomainName | string.pascalplural}}{{else}}{{Parent.SubdomainName | string.pascalsingular}}{{end}}Response.cs"


automate edit update-command-launchpoint "Generate" --add  "*" --from "{SaaStackSubdomain.ServiceOperation}"
```

At this point, you have now wired up all the code templates to commands that generate code from them for any Service Operation.

Now, if you run this command:

```cmd
automate view pattern --all
```

You will see the whole pattern all wired up.

There are now two Launch Points, both called "Generate". One on the Subdomain, and one for every Service Operation. 

## Edit the code

Now is the time we can start editing the code templates, and tailoring the code to the specific Subdomain and Service Operation.

There are a bunch of code templates on the Subdomain (root) element, and some on the `ServiceOperation` child element.

Run this command to view them all:

```cmd
automate view pattern --all
```

Now, for each of the "Code Template" nodes you see in under the root element (`SaaStackSubdomain`), you can run a command like this to edit the content of the template, for example:

```cmd
automate edit codetemplate "InfrastructureProject" --with "%localappdata%\Programs\Microsoft VS Code\code.exe"
```

For the code templates, under the `ServiceOperation` child element:

```cmd
automate edit codetemplate "Validator" --with "%localappdata%\Programs\Microsoft VS Code\code.exe" --aschildof "{SaaStackSubdomain.ServiceOperation}"
```



> Templatizing these files is beyond the scope of this document. You can see examples of the [Scriban](https://github.com/scriban/scriban/blob/master/doc/language.md) syntaxes [used here](https://jezzsantos.github.io/automate/reference/#templating-expressions).

However, the content for the `InfrastructureProject` template would be this:

```xml
<Project Sdk="Microsoft.NET.Sdk">

    <PropertyGroup>
        <TargetFramework>net8.0</TargetFramework>
    </PropertyGroup>

    <ItemGroup>
        <ProjectReference Include="..\Application.Services.Shared\Application.Services.Shared.csproj" />
        <ProjectReference Include="..\{{SubdomainName | string.pascalplural}}Application\{{SubdomainName | string.pascalplural}}Application.csproj" />
        <ProjectReference Include="..\Infrastructure.Persistence.Common\Infrastructure.Persistence.Common.csproj" />
        <ProjectReference Include="..\Infrastructure.Web.Api.Operations.Shared\Infrastructure.Web.Api.Operations.Shared.csproj" />
        <ProjectReference Include="..\Infrastructure.Web.Hosting.Common\Infrastructure.Web.Hosting.Common.csproj" />
    </ItemGroup>

    <ItemGroup>
        <AssemblyAttribute Include="System.Runtime.CompilerServices.InternalsVisibleToAttribute">
            <_Parameter1>$(AssemblyName).UnitTests</_Parameter1>
        </AssemblyAttribute>
    </ItemGroup>

    <!-- Runs the source generator (in memory) on build -->
    <ItemGroup>
        <ProjectReference Include="..\Tools.Generators.Web.Api\Tools.Generators.Web.Api.csproj" OutputItemType="Analyzer" ReferenceOutputAssembly="false" />
    </ItemGroup>

    <ItemGroup>
        <EmbeddedResource Update="Resources.resx">
            <Generator>ResXFileCodeGenerator</Generator>
            <LastGenOutput>Resources.Designer.cs</LastGenOutput>
        </EmbeddedResource>
    </ItemGroup>
    <ItemGroup>
        <Compile Update="Resources.Designer.cs">
            <DesignTime>True</DesignTime>
            <AutoGen>True</AutoGen>
            <DependentUpon>Resources.resx</DependentUpon>
        </Compile>
    </ItemGroup>

</Project>
```

> You can also find all the template files on your disk in the directory `src/autoate/patterns/{Id}/CodeTemplates/*.*`

When finished editing all the templates, just save the files.

## Build Toolkit

Now that we have created the toolkit, it's time to publish it for use by your team.

Execute this command:

```cmd
automate build toolkit 
```

>  This command creates a new `*.toolkit` file on your desktop (representing the latest version of your toolkit)

---

# Create a new Subdomain

With an automate toolkit built (or obtained from someone else) you can now use it to create some code for you.

## Install the Toolkit

In a Terminal: 

* Make sure you change to the `src` directory of the codebase.
* Install the latest toolkit: `automate install toolkit "%userprofile%\Desktop\SaaStackSubdomain_1.0.0.toolkit"`

> The `*.toolkit` file should have been created on your desktop from the previous steps in this guide. If it does not exist on your desktop already simply type: `automate build toolkit` to re-build it again.
>
> OR, you can verify whether this toolkit ("SaaStackSubdomain") is already installed, with this command: `automate list toolkits`

## Use the Toolkit

Now, use the toolkit to build a new API.

This is all you need to do to specify API endpoints for a new Subdomain. (e.g., `CreateThing`, `GetThing`, `SearchAllThings`, and `DeleteThing`):

```cmd
automate run toolkit SaaStackSubdomain --name Things
automate configure on "{SaaStackSubdomain}" --and-set "SubdomainName=Thing"
```

> Notice how we specify the singular name of the `Thing` we want to model, not the plural name.

Now define each kind of endpoint you like:

A POST API:

```cmd
automate configure add-one-to "{SaaStackSubdomain.ServiceOperation}" --and-set "Name=Create" --and-set "Route=/things" --and-set "Kind=POST"
```

A GET API:

```cmd
automate configure add-one-to "{SaaStackSubdomain.ServiceOperation}" --and-set "Name=Get" --and-set "Route=/things/{Id}" --and-set "Kind=GET"
```

A SEARCH API:

```cmd
automate configure add-one-to "{SaaStackSubdomain.ServiceOperation}" --and-set "Name=SearchAll" --and-set "Route=/things" --and-set "Kind=SEARCH"
```

An PUT/PATCH API:

```cmd
automate configure add-one-to "{SaaStackSubdomain.ServiceOperation}" --and-set "Name=Change" --and-set "Route=/things/{Id}" --and-set "Kind=PUTPATCH"
```

A DELETE API:

```cmd
automate configure add-one-to "{SaaStackSubdomain.ServiceOperation}" --and-set "Name=Cancel" --and-set "Route=/things/{Id}" --and-set "Kind=DELETE"
```

At this point you can see what you have configured, by running this command:

```cmd
automate view draft 
```

## Generate all the code

Now that you have added your new service operations, it is time to generate some code for them all.

Run this command, and watch the output:

```cmd
automate execute command "Generate"
```
