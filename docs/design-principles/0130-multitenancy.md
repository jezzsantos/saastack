# Multi-tenancy

Multi-tenancy is the ability of a single deployed version of software to handle multiple tenants/organizations/companies/groups at the same time and segregate their private data so that you don't have to deploy multiple versions of the software, one for each tenant. This is a key attribute of SaaS-based products, as opposed to other kinds of products.

Some SaaS-based products are "single"-tenanted, where:

* Multiple end-users of a single business entity (i.e., a single company) share one logical and/or physical *instance* of software running that operates on one shared data set (between the end-users). e.g., a desktop application connected to a central server.

Most SaaS products are "multi"-tenanted, where:

* Multiple organizations (tenants) with multiple end-users share one logical or physical instance of software running but have separate data sets. e.g., an online document store like Google Docs.

> Note: There is wide variance on what "logical" and "physical" *instance* means in practice, for each piece of software.

### A short history

Multi-tenancy is largely about data segregation today, thanks to the cloud and the rise of SaaS. We no longer distribute most software products on physical media like CDs or DVDs. Instead, we predominantly build web apps or internet-connected mobile/desktop apps or devices.

Before the cloud was widely adopted, companies and consumers did not trust software vendors to host and run their software for them, let alone store any of their data. Companies and consumers installed the software products in their own proprietary data-centers or on their own personal internet-connected machines. This process is known as "On-premise" installs of software products. The software itself was distributed by vendors on CDs and DVDs. The license keys came stamped on the box.

This model satisfied the company's/consumer's need for control and ownership of their data, but it made upgrading the software very difficult and expensive for the software vendors. Each physical installation of the software in a company's/consumer's data center represented a single tenant of the software. Data for the application was stored locally on the machine where the software was installed, on in local networked infrastructure in the office. All the tenants (in all the data centers where the software was installed) represented a giant multi-tenanted product, but none of them were networked or shared the same physical environment nor the same data.

Later, some software products allowed you to "save your data" centrally to the vendor's data centers, and this began the birth of multi-tenanted backend systems. That evolved quickly to the whole application being run centrally, storing its data centrally too.

SaaS became more popular in the mid-late 2000s, when companies began to trust the software vendors more with their software installations and data. Typically, a SaaS business builds a software product and controls the software version and its distribution, and also stores their customer data centrally in their own data centers. Then we have the large cloud providers who provide the hosting environments and infrastructure to run the software and store data for those SaaS companies.

Some SaaS companies permitted the large cloud providers to host both their software and their customer's data, while others hosted their products on their own cloud-based infrastructure. Some SaaS companies had to segregate a specific customer's data from other customers' data due to sovereignty issues. Thus, we entered an era of multi-tenanted architectures that needed to segregate their data physically between each customer using the software on their "platform".

The word "platform" became the term to loosely describe a multi-tenanted system, where each tenant could manage their own virtual installation of the software. In most cases, they branded it as their own software.

In SaaS, there is usually only one physical "version" of the software running, and the version is shared by all companies/consumers using the product at any one time, no matter where in the world they are consuming it from. Sharing and scaling one version of the software globally is easily achievable as long as the product is web-based. The real problems in SaaS happen when trying to segregate and manage customer data securely and efficiently.

### Multi-tenancy in SaaS today

#### Single-tenanted

Refers to a business model where one or more users share the same data and services.

This means that some data is private to each user, while some data can be public and shared by all users. In such a scenario, each user has control over their private data (e.g., on their own device/disk), while the product manages all public data.

All data and third-party services that the product uses are physically deployed in a single set of shared infrastructure (e.g., a single database)

This means that the software accesses this single instance of data and services on behalf of each user.

#### Multi-Tenanted

Refers to a business model where one or more users share the same data and services within their "tenant" using the software, but users from different tenants do not share data with each other. In such a scenario, data and services between tenants must be at least "logically" separated, if not "physically" separated, to avoid sharing/disclosure of information between tenants.

This means that if data is stored in the same physical database, then that data is partitioned by identifiers. Or it could mean that data is physically separated into physically separate databases, with one physical instance for each tenant. This strategy limits the probably that data is leaked between tenants.

The software accesses the data for each user on behalf of the tenant they belong to. In some systems, users are stored centrally, while tenants are stored separately, allowing users to have access to multiple tenants. This depends on the type of product being built.

In essence, a 'Tenant' is a loose boundary around some data and services that is shared by one or more specific users and not others.

A tenant can be scoped in a given digital product as any of these common concepts: a Country, a Company, a Business, an Organisation, a Workspace, a Project, a Party, a Team, and any other kind of group or grouping.

> Depending on the specific product, all of these concepts can be manifested as separate tenants.

## Design Principles

* Multi-tenancy is an integral part of any B2B or B2C SaaS web business model.
* Build once, deploy once, maintain one version of the software, and roll upgrades continuously, since we fully control the distribution of the software.
* Never share any data between tenants, unless explicitly authorized by each tenant. Privacy is paramount.
* Each tenant will likely want to customize the software running with their tenancy. Sometimes, this is just branding (fonts, colors, logos etc.), other times, it is the rules and configuration of the software.
* Tenants will likely want to give access to their tenancy to people inside and outside their company. Some individuals (i.e., consultants) will have access to more than one tenancy at any time, regardless of whether they work for the company in the tenancy or for companies/independent bodies outside that company.
* Physical partitioning of data is an important solution in a global SaaS solution.

## Implementation

### Identity Model

In SaaStack, we have a choice of how to model multitenancy out of the box. The choices we have *essentially* comes down to whether we want to define identities **exclusively** within a tenant, or define identities **centrally** outside all tenancies (and shared with all tenancies across the platform).

In B2B and B2C SaaS products, it is common to model identity either way. SaaStack has to make a choice in order to be a functional template however.

> Note: This decision is not baked in concrete, and can be changed if necessary, but it cannot be both at the same time.

This choice not only affects the rules around the relationships between users (`EndUser`), memberships and tenants (`Organization`), and where their data physically resides, it also defines the API contract used by those subdomains for all the use cases for managing identities.

The choice SaaStack has made is to define all identities centrally, outside any given tenancy.

The implication of this decision is that:

* An `EndUser` in the system is unique across all tenancies by their username (their email address). That implies that a human using the system is the same human (by email) address, no matter what tenancy they are working in at any one time. (e.g., an independent consultant collaborating with more than one company). That human can of course use several email addresses, should it be necessary (due to rules/constraints) to access any specific tenancy only with a certain email domain. Which is a common requirement in many enterprise B2B products.
* Any `EndUser` can have any number of `Memberships` to any number of tenancies (`Organisations`), which is common for many B2B and B2C products. However, they must always have at least one (see later).
* The authentication (login) process, would be branded with the branding of the the SaaS product itself, since at that point in time, it is not clear which tenancy the user wishes to access. Which is common for a lot of B2C and B2B products. Think www.slack.com, or www.miro.com or www.google.com, etc, where you login to the unbranded product, before accessing the branded tenant.
* This also means that any `Enduser` will need to have a default "home" tenancy at all times, so that they can always login to the platform and either make a choice about what context they are working in at that time, or be sent to the default tenancy they normally work in, or last worked in, etc..
* This also means that when they register on the central platform they are automatically assigned to their own "personal" tenant (`Organization`), from which they can use the product (to some degree - depending on their subscription at that time). They must always have that personal `Organization`, for the times when they lose access to any other tenancy (i.e., a consultant ends their engagement with a company, or as an employee change jobs at any organization).

> Of course, it should remain possible to change that default tenant/organization at any time, and certain actions, like getting invited to another tenancy can automatically change that default also.

This all implies the following domain model that both describes multi-tenancy and identity, would look like this:

![Multitenancy](../images/Multitenancy.png)

As you can see, `EndUser` is the at the heart of the model, and almost everything else is dependent upon it. The membership model decides access to one or more `Organizations`, and each organization references a `Subscription` determining their access to the rest of the product.

### Managing Multi-tenancy

Lets break down the implementation of multi-tenancy in SaaStack into these critical areas:

* [Creation of tenants](#Tenant-Creation)
* [Configuration for each tenant](#Infrastructure-Configuration)
* [Detection of a tenant](#Tenant-Detection)
* [Storage of data](#Data-Storage)

#### Tenant Creation

In systems that host multiple tenants, the process of creating and provisioning new tenants typically occurs automatically.

Depending on the infrastructure required for each tenant, this process may be manual or automated, and the time it takes to complete may vary.

For example, provisioning a new database in a different geographic region could take longer than creating a new record in a shared database in a shared region.

To illustrate, in some products, buyers may register their account on a customer management site, and after a short delay, a new tenancy is set up on the main product website. As this process can be costly, it may require some account management by the SaaS business.

On the other hand, for another product, buyers can sign up on the main application website, and their account is created in the central storage tenant instantly without any additional account management or delays.

In SaaStack, a tenant is initially modeled as an `Organization`.

> The "Organization" subdomain can be renamed to be any of these concepts, to fit the specific business model of the SaaS business: `Group`, `Company`, `Workspace`, `Project`, `Part` or `Team`, etc.

When a new ~~tenant~~ `Organisation` is created (via the API), it is created in a centralized (and untenanted) part of the system, where all `EndUser` and `Memberships` are also created. This means that `Organizations` are global across the entire product.

The data/record about the `Organisation` is created instantly. At the same time, the record is populated with any settings pertinent to that tenant. (see Configuration section below).

If any infrastructure is required to be provisioned and configured, that process (automated or manual) can be triggered by registering a listener to events from the `Organization` events (via Notifications), and by responding to the `OrganizationsDomain.Events.Created` event.

> Further, if provisioning physical infrastructure is expected to take some time (i.e., greater 1sec) then code (API's plus domain code) can be added to the `Organization` subdomain to include a process in the lifecycle of an  `Organization` that checks on the completeness of the provisioning process before certain other actions can be performed with the organization.

#### Infrastructure Configuration

In multi-tenanted SaaS systems, we always share computing resources across multiple tenants, whether that is on a single server/process or across multiple scaled-out servers/processes.

However, sometimes we need (due to the our business model) to separate/segregate customer data to different locations or different ownership, e.g., different databases in different global data centers, or a dedicated databases per tenant.

To do that, each tenant requires its own unique configuration for connecting to those services (e.g., the technology-specific `IDataStore` adapter, like the `SqlServerDataStore`, or the `DynamoDbDataStore`).

> SaaStack uses a ports and adapters architecture where the adapters have all the knowledge (and read their own configuration) for connecting to remote 3rd party services. This is true for data storage adapters in the same way it is true for any 3rd party adapters to any external service.

Runtime configuration is considered in two parts:

1. `Platform` configuration settings, that are shared by all infrastructure across the platform. (usually stored in static files like `appsettings.json`)
2. `Tenancy` configuration settings, that are private and specific to a specific tenant.  (stored in a data store, in the `Organization` subdomain)

Therefore, for multi-tenancy to work at runtime, some adapters in the code must load configuration settings that are specific to each tenant the code accesses (on behalf of that tenant). To enable that to happen, each `Organization` has its own set of `TenantSettings` that it can use to configure these adapters at runtime.

This needs to happen dynamically at runtime to work effectively, and it works at runtime like this:

1. When an HTTP request comes into the Backend API, a new request "scope" is created for each request by the dependency injection container, so that we can resolve "scoped" dependencies (as well as "transient" dependencies). Note: "singleton" dependencies will have already been resolved at this point in time.
2. The request is processed by the middleware pipeline, and in the middleware pipeline we have the `MultiTenancyMiddleware` that parses the HTTP request and uses the `ITenantDetective` (see detection below) to discover the specific tenant of the inbound request. Once discovered, it sets the `ITenancyContext.Current` with the tenant ID and sets the `ITenancyContext.Settings` for that specific tenant. These settings are fetched from the stored `Organization` that the tenant identifies, which were created when the `Organization` was created.
3. Then, later down the request pipeline, a new instance of an "Application Layer" class (e.g., the `ICarsApplication`), and this application class is injected into the `CarsApi` class, where the request is processed by the remaining code. This application class instance will require a dependency on one or more technology adapters to 3rd party services (e.g., the `ICarRepository`) that will ultimately be dependent on an adapter for the `IDataStore` that can be satisfied by instantiating some technology adapter (e.g., `SqlServerDataStore` or `DynamoDbDataStore`). Into the constructor of this instance of technology adapter, an instance of the `IConfigurationSettings` will be injected, which will ultimately be dependent on the `ITenancyContext.Settings` which, in turn, will fetch specific settings for this specific tenant. (see the `AspNetConfigurationSettings`)
4. Finally, the specific technology adapter (e.g.  `SqlServerDataStore` or `DynamoDbDataStore`) will load its configuration settings, the `IConfigurationSettings` will attempt to retrieve them first, from the current `ITenancyContext.Settings` if they exist for the tenant, and if not, then retrieve them from the shared `Platform` settings (available to all tenants).

Ultimately, the actual settings that are used in any adapter in any Application or Domain Layer, is down to two things:

1. Whether the port is registered in the DI container as "scoped", "transient" or "singleton"
2. If "singleton", then only shared `Platform` settings are available to it.
3. If "scoped" AND if the actual HTTP request belongs to a specific tenant, AND if the specific `Organization.Settings` contains the required setting, then it will be used, otherwise the shared `Platform` setting will be used.
4. If "transient", it is similar to "scoped" above, except that the instance of the adapter is recreated each time it is resolved in the container, rather than being reused for the lifetime of the current HTTP request, which may cause other issues.

##### A note about dependency injection

For many "generic" subdomains like `EndUsers`, `Organizations`, `Identity` and `Ancillary` etc all their data will be centralized (by default), and none of that data is specific to any tenant. They store the data that all tenants require to operate, including the definition of the tenants themselves (`Organizations`). These dependencies have the lifetime of "singletons" (i.e. one instance shared with all consumers, and only ever instantiated once). They also only use `Platform` configuration settings.

However, most of the "core" subdomains like `Cars`, `Bookings` (and the others to be added) are likely to be tenant-specific. These dependencies have a lifetime of "scoped" (i.e. a different lifetime per HTTP request). They may require private tenant specific configuration settings to work, or may use shared `Platform` configuration settings.

> It is NOT really common to use "transient" lifetime for the adapter dependencies of subdomains. Even though, technically that would work - it would just be inefficient in memory, and for some technology adapters a waste of resources (i.e. caches). However, some stateless dependencies could be "transient".

This means that at runtime, there will always be some services that are tenant specific, and some that will be not-specific specific, and some (like `IDataStore`) that are registered in the container, for both, at the same time (one instance for use by tenanted dependents, one for use by non-tenanted dependents).

> Note: Great care must be taken when configuring the dependency injection for every domain in the software, so that infrastructure that should be tenant specific is not accidentally configured to use centralized infrastructure.
>
> Also, make sure you don't inject "transient" dependencies into "scoped" or "singleton" dependencies, and don't inject "scoped" dependencies into "singleton" dependencies". Neither of these things have the desired effect with lifetimes.

One last point about dependency injection and multi-tenancy that is quite important:

* The MediatR handlers that are used in the API layer to relay requests to the Application Layer are required to be registered in the container as either "transient" or "scoped". This is important, so that when ever a "scoped" dependency is resolved in the handler, it is resolved in the same "scope" of the HTTP request.

(see [Dependency Injection](0060-dependency-injection.md) for more details)

#### Tenant Detection

Every end user of the system needs to belong to at least one tenancy to use the product meaningfully in the "core" domains. Otherwise, they would have limited abilities restricted to only some of the "generic" subdomains.

> Note: There are exceptions to this rule for a limited set of special service accounts in any product, that does not belong to a specific tenant. For example, the anonymous account, and other background worker processes that operate on data for all tenants. (e.g. asynchronous queuing services, or clean-up services).

Once a user identifies with and interacts with the product/system, their tenancy needs to be known immediately, and this tenancy needs to be passed throughout the technical system so that each process in the system can know and honor their tenant.

For that to be possible, in the broadest range of options, the tenants and the users need to be held centrally to be looked up at runtime.

> Note: Some SaaS systems store users in each of the tenants, which is a viable solution only if the tenant can be pre-determined AND users are duplicated across the tenants that they are members of. In SaaStack the default is to keep all `EndUsers` centrally, so that a end user in the software equates to a single email address in the real world, and end users can be members in many tenancies, and can come and go frequently from those tenancies.

In some systems, the user's tenancy is pre-determined for them ahead of interacting with the system (each request contains that information); in some systems, the tenancy that they want to access needs to be manually selected in some way and then declared to the system when interacting with it.

> Note: In SaaStack, the default is to identify the tenancy in the request coming into the API, or once authenticated, use the ID of the default tenant (Organization) that is stored against that specific authenticated user. Anonymous users would then need to log in against a central login mechanism to determine the default tenant for that user, once authenticated.

There are several common ways to declare a tenancy in any HTTP request:

* In a call to a WebApp or API, using host names (or host headers).
   * e.g., if they access `https://tenant1.acme.com/login.html`
   * Their tenant is identified beforehand as `tenant1`. In this scheme, users may be able to access other tenants, but it would have to be through different URLs, or they access a central URL (like: `www.acme.com`) and make a selection of a tenant there.

* In a call to a WebApp or API, using URI paths .
   * e.g., if they access `https://www.acme.com/tenant1/login.html`
   * Their tenant is identified as: `tenant1`. Same caveats as above.

* In a call to a WebApp or API, using API keys in headers or the query string.
   * e.g., if they access `https://www.acme.com/login.html` and in the request is an `ApiKeyToken` header containing the value of `tenant1`
   * Their tenant is identified as: `tenant1`. Similar caveats as above.

* In a call to an API only, using specific claims in an *access_token*.
   * The caveat is that with an anonymous user, the tenant cannot be identified. Also, in systems where the memberships can change, the *access_token* will need to be forcibly refreshed - which leads to other downstream problems

* Finally, In a call to a WebApp only, if they access  `https://www.acme.com/login.html` and are prompted for their username, and since they are not authenticated yet, their username can be looked-up in the central directory of memberships, and their tenant or tenants can be listed for them to choose manually, or select their default tenancy (if they have one).

In every case, no matter how the tenant is communicated in each HTTP request, if the user is authenticated at that time, their membership to that tenant must be validated against the tenant.

Once the request is verified, the `TenantId` can be passed downstream to other processes, via dependency injected services.

> Typically, this can be achieved by including the `TenantId` in downstream HTTP requests, and in events or messages send across messaging infrastructure.

In SaaStack, we use three components to figure-out and expose the tenant for every incoming request.

1. We use the `MultiTenancyMiddleware` in the request pipeline to begin the process. This middleware needs to run before the authorization middleware runs since the authorization middleware requires knowledge of the current tenant to work properly in cases where tenancy roles are required.
2. The `MultiTenancyMiddleware` parses the request and uses the `ITenantDetective` to extract the ID of the tenant from the HTTP request. This
   *detective* is the only component that has knowledge of where the tenant is identified in the HTTP request. Once the ID is found in the request (or not), the middleware continues. If the ID of the tenant is not found, and if the user is authenticated, then the default organization of the user is used. Otherwise, the process fails. If the ID of the tenant is found by the detective, it is then validated, and if authenticated verified against the memberships of the user. Finally, the middleware sets the ID of the tenant and the settings for that tenant to the `ITenancyContext`, which is used further down the request pipeline.
3. The final component in this process is the `MultiTenancyFilter` which is active on any API endpoint that uses an `IWebRequest` that is also an `ITenantedRequest`. This filter gives us access to the "request delegate" of the minimal API declaration, which includes the original request as a typed parameter, which is then unpacked and fed downstream to the Application Layer. Those typed requests (`ITenantedRequest`) will have an optional `OrganizationId` in either their request body (in the case of POST, PUT, or PATCH requests) or in their query parameters (in the case of GET requests), that is passed from the request pipeline to the Application Layer. If that `OrganizationId` is not present in the body or the query parameters of the original request (by the API caller), AND if a tenant ID is derived by the middleware (e.g., from an authenticated user), then that derived tenant ID is now written into the `OrganizationId` field of the endpoint request delegate (`ITenantedRequest.OrganizationId`).

With the request pipeline processing above, it is possible to detect the tenant (or derive a sensible default for it) and pass that downstream to the Application Layer, as you might expect.

#### Data Storage

It is common for any given SaaS product to use multiple data stores and third-party services, as well as different technologies (e.g., a mix of relational databases and non-relational databases). Each store is typically dedicated to a specific kind of data and workload, such as caching data in in-memory databases, transactional application state in SQL or NoSQL databases, logging data in files or binary stores, reporting data in warehouses, and audit data in write-only storage, to name a few.

> Today, there are many options that are better optimized for the data type rather than storing all data in a single relational database, which used to be the only approach and is still common in many systems.

For instance, in SaaStack today, by default, we already use data sources like:

1. Azure Storage Account Queues or SQS queues for delivering emails to an email provider (like SendGrid, MailGun or PostMark)
2. Azure Storage Account Queues or SQS queues for relaying audits to permanent storage in a queryable database like SQLServer.
3. Azure Storage Account Blobs or S3 buckets for storing uploaded files and pictures.
4. Memcached or Redis Server for caching data in memory.
5. Azure SQL Server, DynamoDB, or EventStore DB for storing subdomain state.

As SaaS products evolve, we would expect then to use more third-party services, such as Chargebee and/or Stripe for billing account management, Auth0 or Okta or Cognito for identity services, and Twilio for text messaging, among others.

With all these different services and all these different workloads in the product, we need a strategy for separating the data for each of these services per tenant, either logically or physically, and drive that with configuration to access their respective remote infrastructure.

##### Logical Data Partitioning

Logical data partitioning is a technique used to store data from multiple tenants in a single physical repository or service.

This approach is supported by most data stores and third-party online services in one form or another. However, not all online services implement it very well.

The key concept behind logical data partitioning is to utilize a single physical service (and usually account subscription) and specify a "partition" for each tenant in the data.

For example, in a SQL database, you can add an additional foreign key column to each table that contains tenanted data called `TenantId` and ensure that this column participates in each and every SQL operation, such as SELECT, INSERT, UPDATE, and DELETE.

> Whereas, shared data across all tenants will not require this constraint.
>
> There is a real world danger with this kind of partitioning, that developers can easily make a mistake and forget to include the `TenantId` in the query or update statement, and they can inadvertently expose data from one tenant to others, causing a data breach event.

For example, in Azure Table Storage, you might define the `PartitionKey` as the `TenantId`, and create a combined `PartitionKey` with the `TenantId` and the `EntityId`, or create a combined `RowKey` using the `TenantId` for each table.

For example, in a call to a third-party service that is already multi-tenanted, like www.Chargebee.com, and requires unique API keys per tenant, you might include an identifier in the metadata of the API call to identify the tenant.

For example, in a call to a third-party service that is not natively multi-tenanted, like www.sendgrid.com and uses a "shared" API key, regardless of the tenant in the application, you might include an identifier in the metadata of the API call to identify the tenant to help with reading the data back for a specific tenant in the application.

Logical data partitioning is most often controlled by the product software itself, and it relies on the product team to ensure that all software applies, honors and abides by this partitioning. However, it does not guarantee that information disclosure across partitions is not an accidental possibility.

> Past defects in the software and mistakes and shortcuts in DevOps practices have resulted in numerous accidental cross-tenancy information disclosures and privacy data breaches with severe consequences.

Data partitioning is on the other hand cheaper to build and maintain since it uses shared physical infrastructure components (i.e. one database). However, as SaaS products scale out and move globally, other factors become more significant to the business.

Not all single data repositories and third-party services can address those needs effectively when relying on logical data partitioning.

##### Infrastructure Partitioning

Infrastructure partitioning is similar to logical partitioning except that it uses separate (but similar) physical infrastructure to contain the data of each tenant. That could be a separate server, database, storage account, which is addressed differently than other partitions, sometimes in different physical locations or data centers.

It is becoming increasingly financially viable for small companies to deploy this capability, thanks to the large cloud providers, who make it more economical and easier to perform.

Each tenant is assigned its own dedicated physical infrastructure for exclusive use. This infrastructure can be set up and taken down as tenants onboard and exit the SaaS platform.

In some cases, physical ownership of the actual infrastructure and rights are put in place to protect data even after the subscription ends.

In some cases, tenants do BYO of their own infrastructure to be used by the SaaS product.

In some cases, tenants may also manage their own dedicated subscriptions to third-party services like stripe.com or chargebee.com.

Infrastructure partitioning may be mandatory for SaaS products with specific compliance needs, such as HIPAA, PCI, or government, where shared data stores are not permitted or where they have expensive compliance and access requirements.

Physical partitioning has many other benefits, including a reduced risk of accidental information disclosure since access to these dedicated resources is more difficult to execute accidentally in code or during production support, maintenance, and administrative processes.

Dedicated partitioned infrastructure can also be deployed closer to the tenant's region or onto their premises, which is impossible with logical data partitioned infrastructure.

Network latency can also be reduced by deploying dedicated infrastructure closer to the tenant, whereas data partitioned infrastructure is often shared from one or more physical locations that may not be physically close to the product consumers.

Finally, managing the cost of physical resources per tenant can be more carefully controlled and optimized by the buyer, thanks to mature cloud provider tools and services.

At some point, many SaaS products will need to explore infrastructure partitioning with certain customers, usually the larger or more strategic customers who are likely to have special needs.

### Provisioning

For SaaS products that wish to provision physical infrastructure to implement "Infrastructure Partitioning" for one or more tenants here are some options.

Consider the following workflow:

1. A new customer signs up for the platform. They register a new user, and that will create a new `Personal` organization for them to use the product. This organization will have a billing subscription that gives them some [limited] access level to the product at this time (i.e., a trial).
2. At that time, or at some future time (like when they upgrade to a paid plan) a new event (e.g., `EndUsersDomain.Events.Registered`) can be subscribed to by adding a new `IEventNotificationRegistration` in one of the subdomains.
3. This event is then raised at runtime, which triggers an application (in some subdomain) to make some API call to some cloud-based process to provision some specific infrastructure (e.g., via queue message or direct via an API call to an Azure function or AWS Lambda - there are many integration options). Let's assume that this triggers Azure to create a new SQL database in a regional data center physically closer to where this specific customer is signing up from.
4. Let's assume that this cloud provisioning process takes some time to complete (perhaps several minutes), and meanwhile, the customer is starting using the product and try it out for themselves (using their `Personal` organisation, which we assume is using shared platform infrastructure at this time.
5. When the provisioning process is completed (a few minutes later), a new message [containing some data about the provisioning process] is created and dropped on the `provisioning` queue (in Azure or AWS).
6. The `DeliverProvisioning` task is triggered, and the message is picked up off the queue, and delivered to the `Ancillary` API by the Azure function or AWS Lambda.
7. The `Ancillary` API then handles the message and forwards it to the `Organization` subdomain to update the settings of the `Personal` organization that the customer is using.
8. As soon as that happens, if we assume that the message contained a connection string to another SQL database, then the very next HTTP request made by the customer will start to persist data to a newly provisioned database.
9. From that point forward the newly provisioned database stores all tenanted core subdomain data in the newly provisioned database.

The `provisioning` queue is already in place, and so is all handling of messages for that queue, all the way to updating the settings of a spefici `Organization`.

All that is needed now is:

1. A scripted provisioning process to be defined in some cloud provider. That could be via executing a script that automates the provisioning, or could be an API call direct to the cloud provider with some script already defined in the cloud provider.
2. Some way to trigger the provisioning process itself, based upon some event in the software. Be that a new customer signup or some action they take. That could be `IEventNotificationRegistration` or another mechanism.
3. A way for the provisioning script to construct and deposit a message (in the form of a `ProvisioningMessage`) and deposit it on the `provisioning` queue.
