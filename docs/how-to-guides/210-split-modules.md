# Split your Subdomains into multiple API Hosts

## Why?

You are starting to reach the point where you have too many subdomains and APIs in one host.

You might be experiencing performance issues with certain API endpoints, or you might want to split your teams and focus.

## What is the mechanism?

The whole codebase has been designed to be modular and to be easily split into multiple deployable hosts each with one or more modules running within it.

The mechanism is simple, you can create a new host project and simply move the modules you want to that host.

> You can even copy modules into several host projects if you want. But we recommend keeping a single module in a single host project to avoid confusion.

## Where to start?

Start by creating another API Host project in the solution (like `ApiHost1` project) , in the "Hosts" solution folder, and using the project template (you already installed) from `src/Tools.Templates/HostProject`.

> If you use another project template, you will need to update the *.csproj file with some special directives, that you can copy from the existing `ApiHost1` project.

### IP Address

Your new host project will be configured to run at `https://localhost:5002`.

> If this is your third or more host project, you need to update this port to be different than all your other API Host projects.

Review and change this port number in the `Properties/launchsettings.json` file of your new project, for all profiles.

### Host Type

The first thing to do is open the `Program.cs` file and double check the `WebHostOptions.BackEndHostApi` setting, is right for you for this host.

> If you are simply splitting you API into different modules, then `WebHostOptions.BackEndHostApi` is the correct choice, as long as you are not splitting out the Ancillary submodule. Notice that the `Progra,.cs` of the `ApiHost1` project is `WebHostOptions.BackEndAncillaryApiHost`.

### Submodules

Next thing to do is to plug in your subdomain modules into the `HostedModules.cs` file.

Extract out of the `ApiHost1` project and move them into you r new host project. 

> Ensure that you maintain the same ordering, as some of these modules have dependencies on others.

#### Cross-Domain Service Clients

Next thing to do is to determine which of the subdomains in your new host project communicate directly with the subdomains hosted in other host projects. 

> When you split and deploy these different modules in different hosts in the cloud, it more than likely that they are no longer hosted in the same process. In Azure, that might look like another App Service, in AWS that will look like separate Lambdas.

The glue that joins the different subdomains together, and that allow them to communicate directly are called "Service Clients".

You are interested in the service clients that communicate between the modules in your new host and the modules in the other hosts. 

These "Service Clients" were used to being run IN-Process with each other. But now you have separately deployable units, they now run over HTTP.

What that means is that you will need to write code for a flavor of the same Service Client that marshals calls over HTTP, instead of marshalling calls between application layers within the same process.

For example, if we moved the `CarsModule` into another host project, we would have to have other hosts communicate with the Cars subdomain through the `CarsHttpServiceClient`, rather than using the `CarsInProcessServiceClient`.



Once, you have created those Service Client classes, in the module that consumes the relevant Application Service, simply use dependency injection to inject the `HttpServiceClient` variant, instead of the `InProcessServiceClient` variant.

For example, inject an instance of the `CarsHttpServiceClient` instead of injecting the `CarsInProcessServiceClient` in the host that consumes the Cars subdomain.

### Configuration

1. Ensure the value of `ApplicationServices:EventNotifications:SubscriptionName` in your `appsettings.json` file match the name of your new host project.  For example:

   ```json
    "ApplicationServices": {
       "EventNotifications": {
         "SubscriptionName": "ApiHost2"
       },
   ```

2. Edit the `appsettings.json` (and `appsettings.Azure.json` or `appsettings.AWS.json`) and ensure all the settings are present for all the services, and technology adapters required by the modules in your host project, to operate correctly.

   1. Be sure to set the relevant `Deploy:Required:Keys` key with the relevant values.

### Event Notifications

Most of the subdomain modules that you move into your new host project will contain consumers that subscribe to the Eventing mechanisms that are used to communicate domain events across the system. 

> This Eventing mechanism subscribes to the Message Bus to receive domain_events, that are handled in the Application Layer of each subdomain. 

To ensure this mechanism is operational for your new host project, you need to make sure of these things:

1. Your selected `WebHostOptions` (in `Program.cs`) has `UsesEventing=true`. 

   1. During startup of your new host, the `IDomainEventingSubscriberService` will discover all `IDomainEventNotificationConsumer` implementations in all of the assemblies of all of your modules, and then those consumers will be automatically registered with the Message Bus dynamically.

2. You need to manually add the generated Azure Functions/AWS Lambdas triggers that have been generated for your new host project.

   1. These generated functions/lambdas are responsible for connecting to the Message Bus and relaying the notifications to your `EventingNotificationsModule` API that should be one of the modules in your new host project.

   2. Edit the *.csproj file of either the `AzureFunctions.Api.WorkerHost` project (or the `AWSLambdas.Api.WorkerHost` project. Which ever one you are using) and add the following XML to alongside the existing XML for the existing ApiHost1 workers:

      ```xml
      <Compile Include="..\ApiHost2\Generated\Tools.Generators.Workers\Tools.Generators.Workers.WorkerGenerator\ApiHost2_DeliverDomainEvents.g.cs">
          <Link>Functions/ApiHost2_DeliverDomainEvents.g.cs</Link>
      </Compile>
      ```
      

3. You need to add configuration to the `appsettings.json` file of the `AzureFunctions.Api.WorkerHost` project (or the `AWSLambdas.Api.WorkerHost` project. 

   1. Edit `appsettings.json`, and add a new key to the `Hosts` section, and then add the name of the host to the key `Hosts:EventNotificationApi` in a semi-colon list. For example:

   ```json
     "Hosts": {
       "EventNotificationApi": {
         "SubscribedHosts": "ApiHost1;ApiHost2"
       },
       "ApiHost2": {
         "BaseUrl": "https://localhost:5002",
         "HMACAuthNSecret": "asecret"
       }
   ```


### CI/CD

1. Edit the relevant `deploy-azure` (or `deploy-aws.yml`) scripts, and add your new host to the deploy steps.