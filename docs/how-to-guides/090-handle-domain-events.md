# Handle Domain Events

## Why?

You want to respond to domain/integration events that have been raised in another subdomain, and perform your own processing.

e.g. When a user deletes a specific image, and that image is being used as the avatar for a user, then the `UserProfile` subdomain will want to remove the avatar image, otherwise the downloading of the avatar image will eventually return a `HTTP 400 - NotFound`.

## What is the mechanism?

Every subdomain that changes state, creates domain events as a handy side-effect.

Assuming that the subdomain of interest persists its state in some `IApplicationRepository`, it will also emit domain events whenever the state is persisted.

Those domain events can be subscribed to, either in-process (as "domain events"), or out-of-process (as "integration events").

Any other subdomain can register a "notification consumer: to capture those events and process them, any way they see fit.

See [Eventing](/design-principles/0170-eventing.md) for more details on how that Notifications mechanism works

## Where to start?

Start by identifying the subdomain that emits the events of interest. This is your publisher/notifier/producer of the events.

This is where you will wire up the pub/sub mechanism (if it not already wired-up).

### Configure the Notifier

Start on the notifier side, in the subdomain where the events of interest are generated.

In `SubdomainInfrastructure` project, in the `Notifications` folder, create a new class derived from `IEventNotificationRegistration`.

> Note: there may already be a registered `IEventNotificationRegistration` in this subdomain. There should only be one. In which case, you can skip this whole step and move to configuring the Consumer.

For example, [OrganizationNotifier.cs](https://github.com/jezzsantos/saastack/blob/main/src/OrganizationsInfrastructure/Notifications/OrganizationNotifier.cs)

```c#
public class OrganizationNotifier : IEventNotificationRegistration
{
    public OrganizationNotifier(IEnumerable<IDomainEventNotificationConsumer> consumers)
    {
        DomainEventConsumers = consumers.ToList();
    }

    public List<IDomainEventNotificationConsumer> DomainEventConsumers { get; }

    public IIntegrationEventNotificationTranslator IntegrationEventTranslator =>
        new NoOpIntegrationEventNotificationTranslator<OrganizationRoot>();
}
```

In the `SubdomainModule.cs` class of the `SubdomainInfrastructure` project, locate the `ISubdomainModule.RegisterServices` method.

Locate the specific eventing registration entry that handles the domain events of interest:

For example,

```c#
                services.RegisterUnTenantedEventing<OrganizationRoot, OrganizationProjection>(
                    c => new OrganizationProjection(c.GetRequiredService<IRecorder>(),
                        c.GetRequiredService<IDomainFactory>(),
                        c.GetRequiredServiceForPlatform<IDataStore>()));

```

Add your new `IEventNotificationRegistration` entry as the last parameter of this method call.

For example, in [OrganizationsModule.cs](https://github.com/jezzsantos/saastack/blob/main/src/OrganizationsInfrastructure/OrganizationsModule.cs)

```c#
                services.RegisterUnTenantedEventing<OrganizationRoot, OrganizationProjection, OrganizationNotifier>(
                    c => new OrganizationProjection(c.GetRequiredService<IRecorder>(),
                        c.GetRequiredService<IDomainFactory>(),
                        c.GetRequiredServiceForPlatform<IDataStore>()),
                    c => new OrganizationNotifier(c.GetRequiredService<IEnumerable<IDomainEventNotificationConsumer>>()));

```

> Note: also add the `TNotifier` type parameter of the method signature.

## Configure the Consumer

Now that we have set up the notifier to publish any produced domain events, we now need to set up a consumer in the subdomain where you want to receive the events.

In `SubdomainInfrastructure` project, in the `Notifications` folder, create a new class derived from `IDomainEventNotificationConsumer`.

> Note: there may already be a registered `IDomainEventNotificationConsumer` in this subdomain for the source of domain events you want to handle. There can be an existing one, or many. In which case, you can skip this whole step and move to handling the events.

Register a new `IDomainEventNotificationConsumer` and define the domain events that you wish to handle.

For example, [OrganizationNotificationConsumer.cs](https://github.com/jezzsantos/saastack/blob/main/src/EndUsersInfrastructure/Notifications/OrganizationNotificationConsumer.cs) that handles

```c#
public class OrganizationNotificationConsumer : IDomainEventNotificationConsumer
{
    private readonly ICallerContextFactory _callerContextFactory;
    private readonly IEndUsersApplication _endUsersApplication;
    private readonly IInvitationsApplication _invitationsApplication;

    public EndUserNotificationConsumer(ICallerContextFactory callerContextFactory,
        IEndUsersApplication endUsersApplication, IInvitationsApplication invitationsApplication)
    {
        _callerContextFactory = callerContextFactory;
        _endUsersApplication = endUsersApplication;
        _invitationsApplication = invitationsApplication;
    }

    public async Task<Result<Error>> NotifyAsync(IDomainEvent domainEvent, CancellationToken cancellationToken)
    {
        switch (domainEvent)
        {
            ... events to handle

            default:
                return Result.Ok;
        }
    }
}
```

> Note: that you can in fact handle domain events from multiple other subdomains in the same consumer, or you can create and register separate `IDomainEventNotificationConsumer` classes, and for clarity, segregate the different domain event sources.
>
> Note: the injection of the `ICallerContextFactory` and the `IEndUsersApplication` application port.

Next, register this `IDomainEventNotificationConsumer` in the DI container.

In the `SubdomainModule.cs` class of the `SubdomainInfrastructure` project, locate the `ISubdomainModule.RegisterServices` method.

Register your `IDomainEventNotificationConsumer`.

> Note: In all cases, this registration must be registered using the `services.AddPerHttpRequest()` as we need access to the `ICallerContextFactory` in those methods, which is only available as a `Scoped` dependency in the DI container.

```c#
            services
                    .AddPerHttpRequest<IDomainEventNotificationConsumer>(c =>
                        new OrganizationNotificationConsumer(
                            c.GetRequiredService<ICallerContextFactory>(),
                            c.GetRequiredService<IEndUsersApplication>(),
                            c.GetRequiredService<IInvitationsApplication>()));
```

### Handle the Events

The final step is to handle the domain events, as they are raised.

In your `IDomainEventNotificationConsumer`, you simply now add a `swtch` `case` statement for the event of interest.

For example, [OrganizationNotificationConsumer.cs](https://github.com/jezzsantos/saastack/blob/main/src/EndUsersInfrastructure/Notifications/OrganizationNotificationConsumercs)

```c#
        switch (domainEvent)
        {
            case Created created:
                return await _endUsersApplication.HandleOrganizationCreatedAsync(_callerContextFactory.Create(),
                    created, cancellationToken);

            case MemberInvited added:
                return await _invitationsApplication.HandleOrganizationMemberInvitedAsync(
                    _callerContextFactory.Create(), added, cancellationToken);

            case MemberUnInvited removed:
                return await _invitationsApplication.HandleOrganizationMemberUnInvitedAsync(
                    _callerContextFactory.Create(), removed, cancellationToken);

                ... other handlers
                    
            default:
                return Result.Ok;
        }
```

#### Create the Handler

Normally, handling a domain event in this way is conceptually equivalent to handling an inbound API call. Thus this handler of this call is logically the Application Layer, defined by the application interface.

> Note: The convention, that you will see implemented in this template, we create a separate port/contract for event handlers that are distinct from those being used by an inbound API endpoint. However, we also want to be able to use the same patterns and services of the existing application layer.
>
> Thus, for convenience, we use `partial interfaces` and `partial classes` to segregate these two mechanisms for the programmer, but still be able to reuse all the same code structures.

First, if not already done, mark your Application `interface` declaration as a `partial interface` declaration.

Then, create another file containing another part of the `partial interface` for declaring your event handlers.

For example, [IEndUsersApplication_DomainEventHandlers.cs](https://github.com/jezzsantos/saastack/blob/main/src/EndUsersApplication/IEndUsersApplication_DomainEventHandlers.cs)

```c#
partial interface IEndUsersApplication
{
    Task<Result<Error>> HandleOrganizationCreatedAsync(ICallerContext caller, Created domainEvent,
        CancellationToken cancellationToken);

    Task<Result<Error>> HandleOrganizationDeletedAsync(ICallerContext caller, Global.StreamDeleted domainEvent,
        CancellationToken cancellationToken);

    Task<Result<Error>> HandleOrganizationRoleAssignedAsync(ICallerContext caller, RoleAssigned domainEvent,
        CancellationToken cancellationToken);

    Task<Result<Error>> HandleOrganizationRoleUnassignedAsync(ICallerContext caller, RoleUnassigned domainEvent,
        CancellationToken cancellationToken);
}
```

Finally, you implement this handlers in an adjacent `class` to your Application class.

Then, create another file containing another part of the `partial class` for implementing your event handlers.

For example, [EndUsersApplication_DomainEventHandlers.cs](https://github.com/jezzsantos/saastack/blob/main/src/EndUsersApplication/EndUsersApplication_DomainEventHandlers.cs)

```c#
partial class EndUsersApplication
{
    public async Task<Result<Error>> HandleOrganizationCreatedAsync(ICallerContext caller, Created domainEvent,
        CancellationToken cancellationToken)
    {
        ...
    }

    public async Task<Result<Error>> HandleOrganizationDeletedAsync(ICallerContext caller,
        Global.StreamDeleted domainEvent,
        CancellationToken cancellationToken)
    {
        ...
    }

    public async Task<Result<Error>> HandleOrganizationRoleAssignedAsync(ICallerContext caller,
        RoleAssigned domainEvent,
        CancellationToken cancellationToken)
    {
        ...
    }

    public async Task<Result<Error>> HandleOrganizationRoleUnassignedAsync(ICallerContext caller,
        RoleUnassigned domainEvent,
        CancellationToken cancellationToken)
    {
        ...
    }
```

In these handler methods, you would typically do these things:

1. Unpack the `domainEvent` parameter into the parts you need data, and convert them into ValueObjects.
2. Delegate the call to a new `private` method in this class that handles the event and returns a `Result<Error>`

> Note: In some cases, you might call existing methods on your Application class directly, in which case you would be passing DTO's as opposed to ValueObjects in their parameters.

For example,

```  c#
    public async Task<Result<Error>> HandleOrganizationCreatedAsync(ICallerContext caller, Created domainEvent,
        CancellationToken cancellationToken)
    {
        var ownership = domainEvent.Ownership.ToEnumOrDefault(OrganizationOwnership.Shared);
        var membership = await CreateMembershipAsync(caller, domainEvent.CreatedById.ToId(), domainEvent.RootId.ToId(),
            ownership, cancellationToken);
        if (membership.IsFailure)
        {
            return membership.Error;
        }

        return Result.Ok;
    }
```

with a `private` function, like this:

```c#
    private async Task<Result<Membership, Error>> CreateMembershipAsync(ICallerContext caller,
        Identifier createdById, Identifier organizationId, OrganizationOwnership ownership,
        CancellationToken cancellationToken)
    {
        ... retrieve the aggrgate or readmodels
        ... change state of aggregate
        ... save its state
    }
```

> Follow existing implementations for consistency. 