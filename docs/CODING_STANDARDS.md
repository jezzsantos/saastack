# Coding Standards

The team contributing to this product wish to standardise certain practices, styles, principles and approaches. The document aims to capture most of them.

> At any time, the team is open to revise anything in this document, but because consistency is the key driver behind this document, when things change, they SHOULD change right across this whole repo. The last outcome anyone wants is inconsistencies that are hard to understand and then have clarity about for the future.

## 🏗️ **Architecture & Design Principles**

### Domain-Driven Design (DDD)
- **Use Domain-Driven Design over Data Modeling** - Focus on modeling behaviors rather than data structures
- **Define discrete boundaries** of behavior and encapsulate it using "aggregates", as the smallest atomic unit of state change
- **One root aggregate per subdomain** (ideally) to maintain clear boundaries. There can be exceptions to this rule
- **Aggregates always generate domain events** for atomic units of change, and those events are published using pub-sub mechanisms
- **Use `Result<Error>` return values** for raising errors for control flow, instead of throwing exceptions. Exceptions SHOULD be used in exceptional cases only.
- **Aggregates generate their own unique identifiers**
- **Validate all data entering the domain** (no matter what the source of the data) using ValueObjects or Aggregates, that always return `Result<Error>`
- **Ensure aggregates are never in invalid state** - verify invariants on every state change
- **ValueObjects are immutable** and equal based on their internal state
- **Entities/Aggregates are mutable** and equal by unique identifier only.
- **Subdomains define their own Bounded Contexts initially** and bounded contexts will evolve from there
- **Use Domain Services** to process data and other datasets when needed.
- **Tell the aggregates what to do**, rather than interrogating them and processing in the outside (TellDontAsk & Law of Demeter)
- **Encapsulate business rules in the aggregates**, not in the transaction scripts that orchestrate them. 
- **Unit tests all Aggregates, all ValueObjects and all Domain Services**

### Hexagonal/Clean Architecture
- **Dependencies point inward only** - Domain has no dependencies on Application or Infrastructure layers, Application has no dependency on Infrastructure.
- **Application Layer defines external interfaces** for all subdomains. These are the subdomain contracts
- **Avoid Anemic Domain Models in Application Layer**, and use ValueObjects and Aggregates to process rules, data and state machines.
- **User Transaction Scripts** that co-ordinate and orchestrate the aggregates to do the work.
- **Use the Repository pattern** to work with data from common data sources
- **Use Application Services** to work with data from other sources of data (including other subdomains)
- **Use CQRS pattern** - commands delegate to aggregates, queries delegate to read models. Both via repositories
- **Application class always consume DTO and always return DTOs**.
- Application classes are invoked by infrastructure interfaces. e.g., APIs, Data Sources, Cross-domain invocations, Pub/sub etc.
- **Unit test all application classes**. No need to unit test Repository classes and Projection Classes - unless they implement complex logic, which they SHOULD NOT do normally.

### Modularity & Subdomains
- **Design for modularity** to enable scale-out and splitting deployment units, as product grows, and ease of testability
- **Segregate aggregate state** - no dependencies between aggregates.
- **No JOINS in relational database tables of aggregates** to enable scale-out and splitting deployment units, as product grows
- **Group use cases around common concepts** to define subdomains
- **Use real-world terminology** for subdomain names (ubiquitous language)
- **Generic vs Core subdomains:**
  - Generic: Common to all products (e.g., Identity, Users, Organizations, etc.)
  - Core: Unique to your product (e.g., Cars, Bookings in car-sharing example)

## 🌐 **REST API Design**

### RESTful Principles
- **Model real-world processes** as much as possible, minimize RPC usage
- **Follow Level 3 Richardson Maturity Model** for REST maturity
- **Be consistent** across the entire codebase above all else
- **REST over CRUD** - model actual business processes, not database operations
- **Resources are nouns** involved in state changes of real-world processes
- **Actions are verbs** operating on those processes

### API Implementation
- **Use ASP.NET Minimal APIs** over MVC Controllers
- **Follow REPR design pattern** - Request/Response pairs organized in one layer
- **Use source generators** to convert declarative API classes into boiler plate Minimal APIs
- **Implement pluggable module pattern** for organizing APIs by subdomain
- **Apply cross-cutting concerns** (validation, auth, rate-limiting) at module/endpoint level, in the request pipeline
- **All API declarations are async** by default
- **Integration test all APIs**

### API Structure Example
```csharp
public sealed class CarsApi : IWebApiService
{
    public async Task<ApiGetResult<Car, GetCarResponse>> Get(GetCarRequest request, CancellationToken cancellationToken)
    {
        // Implementation that delegates to the Application
    }
}
```

## 🔧 **Control Flow & Error Handling**

### Exception vs Result Patterns
- **Use exceptions for exceptional cases** - when assumptions about calling context are invalidated
- **Use `Result<TValue, Error>` for expected errors** - validation failures, business rule violations
- **Always handle the returned error code**, and pass up the stack to caller.
- **Don't catch exceptions you cannot handle** - let them propagate to terminate the current thread
- **Catch-and-wrap exceptions** only to add diagnostic context, then re-throw

## 🔒 **Validation & Data Handling**

### Validation Strategy
- **Use FluentValidation** for API request validation
- **Validate at domain boundaries** - all data entering domain must be validated again
- **Create subdomain-specific validations** in `Validations` classes in domain layer for reuse in other layers
- **Use `Matches()` with domain validations** instead of `Must()` assertions
- **Centralize validation logic** for reusability across components and layers

### Nullability Management
- **Enable Nullable Context** (`#nullable enable`) for compiler assistance
- **Use nullables `T?`, both reference types and value types** in infrastructure layers, to comply with dotnet runtime and with external libraries. 
- **Use `Optional<T>`** within domain layer
- **Map between both nullables and optionals** when converting between domain and application data

## 🧪 **Testing Guidelines**

### Test Naming Conventions
- **Success cases**: `WhenMethodNameAndCondition_ThenSucceeds()`
- **Failure cases**:  `WhenMethodNameAndCondition_ThenReturnsError()` OR `WhenMethodNameAndCondition_ThenThrows()`
- **Use descriptive test names** that clearly indicate the scenario and expected outcome

### Test Organization
- **Use `[Trait("Category", "Unit")]`** for test categorization
- **Unit tests and mocks** for individual classes and combinations
- **Integration tests and stubs** for API interactions, and other infrastructure (e.g., Queues, Buses etc.).
- **External integration tests and real online services** for technology adapters
- Arrange tests to test all errors and edge cases first, then happy paths last.
- Prefer a TDD approach for unit testing, to help design your classes better. 

## 📝 **Code Organization & Structure**

### Project Structure
- **Separate concerns into layers**: Domain, Application, Infrastructure
- **Divide subdomains into Vertical slices**
- **Use dependency injection** for loose coupling in modules only
- **Keep infrastructure and 3rd party dependencies at arm's length** using Ports and Adapters pattern
- **Minimize the use of 3rd party libraries and frameworks**

### Naming & Conventions
- **Use ubiquitous language** from the domain
- **Be consistent** with naming across the codebase
- **Follow C# naming conventions** and Microsoft design guidelines
- **Use meaningful names** that express intent clearly. No Hungarian notation

### File Organization
- **Group related functionality** in the same namespace/folder

- **Separate interfaces from implementations**

- Define types, interfaces and constants in assemblies prefixed as `Namespace.Interfaces`

- Define implementations in assemblies suffixed with `Namespace.Common`

- **Use consistent folder structure** across all subdomains


### Reuse

- Reuse responsibly to minimize blast-radius.

- Create reusable code only after you one or two other use cases for it.

- Aim towards D.R.Y, but recognize that disparate use cases require duplicity.

- If you encapsulate reusable methods as helpers into separate classes, prefer extension methods for discoverability and fluency

- Define those classes as close to all the consumers of those methods, defined in the limited scope of a "common" parent assembly.

- At first, define these methods within the subdomain that consumes them, then graduate to `*.Shared` subdomain assemblies, then to the framework common assemblies (e.g., `Infrastructure.Web.Api.Common`), and then ultimately to the `Common` assembly. In that order as required.  

- DO NOT define these helpers methods in the most accessible classes in the codebase by default. i.e., the `Common` assembly.
  - Caution: reusable code in the `Common` assembly has the widest reuse scope in the codebase (accessible to all horizontal layers and all vertical slices), thus it should be characterized by having the simplest and most generalized behavior (and implementation).
  - Make sure it is thoroughly unit tested, since it has the highest chance of being reused in the most disparate use cases, and when it breaks will have the largest blast radius in the codebase.


## 🔄 **Dependency Management**

### Dependency Injection
- **Use built-in .NET DI container**
- **Register services with appropriate lifetimes**:
  - `AddSingleton` for stateless services
  - `AddPerHttpRequest` for request-scoped services
- **Inject interfaces, not concrete types**
- **Keep dependencies minimal** and focused
- Some services are required to be registered twice in the container (A Scoped one per request, and a singleton for the "Platform"), and we use named instances for this purpose, when consumers require to be explicit.

### Package Management
- **Always use package managers** (npm, nuget, etc.) instead of manual file editing
- **Use appropriate package manager commands** for each technology
- **Let package managers handle version resolution** and dependency conflicts
- **Only edit package files directly** for complex configuration that can't be done via commands
- Regularly, move dependencies forward (e.g., once every 2 months), running all tests to check for compatibility

## 📊 **Monitoring & Observability**

### Recording & Logging
- **Use the IRecorder pattern** for capturing usage activity, diagnostics, and audit events
- **Implement structured logging** for better search-ability
- **Capture key legal and security domain events** for audit trails
- **Monitor key business metrics** through domain events

## 🚀 **Development Workflow**

### Getting Started
1. **Identify use cases for a subdomain**, and group use cases around common concepts, defined by a single aggregate
2. **Implement use cases as API first**
3. **Build REST APIs** and start outside-in to stay focused on YAGNI, and avoid overengineering tendencies
4. **Sketch out** the API request and responses, API definition, and Application interface first. Then write API integration tests to identify the inputs and outputs and expected behaviors first.
5. Then implement the Application layer, repository, cross-domain calls and aggregate next, unit testing as you proceed.
6. **Implement domain logic** with proper validation, and raise events for state changes
7. **Write unit tests** for all application methods and all domain code 

### Best Practices
- **Start with domain modeling** before thinking about persistence
- **Focus on behavior** rather than data structures
- **Keep aggregates small** and focused
- **Use domain events** for cross-subdomain communication
- **Design for testability** from the beginning
- **Be consistent** and follow all established patterns and conventions.
- There are numerous examples to follow to do most things. Don't' reinvent the wheel.

## Patterns and conventions

The patterns and conventions above are derived from the extensive documentation in the `/docs` folder and represent the established patterns and principles for developing within the SaaStack codebase. Following these guidelines will ensure consistency, maintainability, and alignment with the overall architecture vision.

### Why are they important?

Patterns and conventions are critical for:
- **Developer mobility** - enabling team members to work effectively across different parts of the codebase
- **Consistency** - ensuring predictable code structure and behavior, making code reviews a breeze
- **Maintainability** - making the codebase easier to understand, modify, and extend, without breaking other pieces
- **Quality** - reducing avoidable bugs through established, proven approaches
- **Onboarding** - helping new team members understand and contribute quickly

## 💻 **Code Replacements & C# Standards**

### C# Coding Standards

We follow Microsoft's C# coding conventions with specific enhancements for readability and consistency.

> These rules are enforced through Rider settings and code analysis rules.

### Preferred Expressions

The following table shows code patterns we want to see replaced throughout the codebase for better readability and consistency:

| Instead of this:                   | Use this:                             | Why?                                                                                                                                                                                            |
|------------------------------------|---------------------------------------|-------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|
| `DateTime.Now`                     | `DateTime.UtcNow`                     | You should never handle local dates and times in the API layer. All dates and times should always be in UTC. Only clients should convert to dates and times to local, based on client settings. |
| `!string.IsNullOrEmpty(variable)`  | `variable.HasValue()`                 | Easier to read and understand the real intent                                                                                                                                                   |
| `string.IsNullOrEmpty(variable)`   | `variable.HasNoValue()`               | Easier to read and understand the real intent                                                                                                                                                   |
| `variable != null`                 | `variable.Exists()`                   | Easier to understand the real intent                                                                                                                                                            |
| `variable == null`                 | `variable.NotExists()`                | Easier to understand the real intent                                                                                                                                                            |
| `variable == null`                 | `variable.IsNull()`                   | Uncommon, for completeness in these rare cases.                                                                                                                                                 |
| `variable != null`                 | `variable.IsNotNull`                  | Uncommon, for completeness in these rare cases.                                                                                                                                                 |
| `string.Format(message, args)`     | `message.Format(args)`                |                                                                                                                                                                                                 |
| `variable.Equals(value, options)`  | `variable.EqualsIgnoreCase(value)`    | More explicit about the comparison type                                                                                                                                                         |
| `!variable.Equals(value, options)` | `variable.NotEqualsIgnoreCase(value)` | More explicit about the comparison type                                                                                                                                                         |
| `collection.Any()`                 | `collection.HasAny()`                 | More readable and expresses intent clearly                                                                                                                                                      |
| `!collection.Any()`                | `collection.HasNone()`                | More readable and expresses intent clearly                                                                                                                                                      |

### Validation Patterns

| Instead of this:                      | Use this:                             | Why?                                                      |
|---------------------------------------|---------------------------------------|-----------------------------------------------------------|
| `.Must(x => x == "value")`            | `.Matches(Validations.Domain.Field)`  | Centralized validation logic, reusable across components  |
| Custom validation logic in validators | Domain-specific `Validations` classes | Consistency and reusability of validation rules           |
| Throwing exceptions for validation    | `Result<T, Error>` return types       | Expected errors should use Result pattern, not exceptions |

### Control Flow Patterns

| Instead of this:               | Use this:                                   | Why?                                          |
|--------------------------------|---------------------------------------------|-----------------------------------------------|
| `throw new Exception("error")` | `return Error.Validation("error")`          | Use Result pattern for expected errors        |
| `if (result == null) throw...` | `if (result.IsFailure) return result.Error` | Consistent error handling with Result pattern |
| Catching all exceptions        | Catch specific exceptions only              | Don't catch exceptions you cannot handle      |

### Nullability Patterns

| Instead of this:                    | Use this:                                        | Why?                                                           |
|-------------------------------------|--------------------------------------------------|----------------------------------------------------------------|
| `string? value = null`              | `Optional<string> value = Optional<string>.None` | Explicit optional values, better than nullable reference types |
| `if (value != null)`                | `if (value.HasValue)`                            | Works with Optional<T> pattern                                 |
| Returning `null` for missing values | `return Optional<T>.None`                        | Explicit handling of missing values                            |

### Test Naming Patterns

| Instead of this:           | Use this:                         | Why?                                                       |
|----------------------------|-----------------------------------|------------------------------------------------------------|
| `TestMethod_Fails()`       | `WhenCondition_ThenThrows()`      | Consistent naming that clearly indicates expected behavior |
| `TestMethod_Success()`     | `WhenCondition_ThenSucceeds()`    | Consistent naming that clearly indicates expected behavior |
| `TestMethod_ReturnsNull()` | `WhenCondition_ThenReturnsNone()` | Aligns with Optional<T> pattern usage                      |

### Test Data Patterns

Example data used in all testing is important to be consistent and avoid magic string values.

* Use constants from relevant `Constants.cs` files where available
* If using numbers in test data, try to use number that look random, for example `9` or `999` 
* If using strings, use descriptive lowercase alphanumeric strings starting with an article (e.g., "anidtoken", "aclientid", "aredirecturi") that describe the variable's purpose, and sound arbitrary. Sounding arbitrary mitigates again confusing the reader with potentially magic strings that may be in the code.
  * Again don't reinvent your own set of values, follow the numerous examples.
  * For real URL values, use "http://localhost", instead of others like "http://example.com"


## ⚠️ **Breaking Changes**

### Understanding Breaking vs Non-Breaking Changes

Breaking changes are modifications to the existing code that can cause existing functionality to fail or behave differently in already deployed production releases, where data is already stored in repositories.

Understanding what constitutes a breaking change is crucial for maintaining system stability.

> WARNING: Some of these breaking changes may corrupt data captured in existing deployed code, and may bring your customers down. So ensure you have good backups of all data repositories, in the case mistakes are made, particularly when under pressure.

### Things Designed to Change Safely (Non-Breaking)

The following components have been explicitly designed to evolve without breaking existing functionality:

1. Basically adding any new code element concept to the existing codebase
2. Changing existing functionality:

- **Application Services** - Internal implementation can change as long as interfaces remain stable
- **Infrastructure Adapters** - Can be swapped out using dependency injection
- **Domain Logic** - Internal aggregate behavior can evolve, and even expose different data, as long as the events themselves don't break.
- **Domain Events** - can be added as long as they don't assume a specific order with respect to existing events 
- **API Implementation** - Internal processing can change while maintaining request/response contracts
- **Validation Rules** - Can be enhanced (made less permissive) without breaking existing clients
- **Configuration Settings** - New settings can be added with sensible defaults


### Things You CANNOT Change easily (Breaking Changes)

The following changes will likely have **significant and dire consequences** on already deployed and running Production systems.

> However, some of these changes can be managed with careful data migrations, defaults and other extra work

#### Changing Domain Events (for Event-Sourcing)
- **Event structure/schema**
  - Renaming the C# class name
  - Adding the C# `required` keyword to existing properties
  - Changing an existing `non-required` field to being `required`
  - Changing field data types or names.

Essentially, any change to the class that causes possible combinations of existing data to fail JSON deserialization into your changed domain event class, will cause a breaking change. Likely to result in an `HTTP 500` error being returned from your API.

Some changes to existing events can be very safe to make. Consider that the old event was already persisted with old data values, and must remain in tack in memory when deserialized now into your new event class.

> Tip: You could write unit tests (using dynamics, or raw strings) to verify that data produced by older versions of your events are compatible with your new event class. 

The event JSON serializer will (by default) ignore certain fields it cannot match between the old version and the new version of your class. So you need to be aware that these values may be null when processed by new code. Hence, the strict use of C# nullability. 

##### Event Migration

There is a supported migration mechanism to help in the case of migrating events at runtime from older versions to newer versions.

If you need to make a breaking change to an event, use the built-in `IEventSourcedChangeEventMigrator` mechanism to map between old and new events classes.

If you use this method, then, basically:

1. Make changes to your domain event class (e.g., `Happened`). 
2. Rename your new class to append a version number to the name of the class. For example: `HappenedV2`
3. Configure the  registered `ChangeEventTypeMigrator`  to map your old class to your new class.
4. Write some basic unit tests to reproduce the old data being deserialized into the new class definition.

Follow the migration guidance in [Migrate Domain Event Versions](./how-to-guides/910-migrate-domain-events.md) for more details

#### Read Models (for Event-Sourcing)
- **Database schema changes** - Adding/removing/renaming tables, columns, changing data types, etc
- **Query contracts** - Changing expected result structures, as the API contract changes
- **Indexes and constraints** - Removing indexes can cause performance degradation

If you need to change a read model, you can create more than one read model at any time, by adding it to the code. This is a better change strategy, that is not possible without event-sourcing. So a new version of the code can create a new read model (appropriately named), and that read model may need to be prepopulated with original data before the new code starts using it, while the old table remains in the database. The old table can be removed at a later date (once a rollback is confirmed not to be necessary).

> Note: Read Models should always be treated as disposable, since the origin data for them is kept in a separate Event Store, and these tables can be rebuilt at any time (with same code) from same events.

#### Message Bus & Queue Contracts
- **Message formats** - Changing message structure breaks consumers. This is only applicable to 3rd party consumers and possibly integration events. If all those consumers are inside your system. You must alter code to deal with the changes in the old and new messages.
- **Queue names** - Renaming queues will break message handling. You must migrate existing messages on existing queues with existing code, or alter existing code to deal with both old and new message schemas.

#### Public API Contracts
- **Request/Response DTOs** - Removing fields, changing field types, making optional fields required. May break 3rd party consumers
- **HTTP endpoints** - Changing URLs, HTTP methods, or response codes. May break 3rd party consumers
- **Authentication/Authorization** - Changing security requirements. May break 3rd party consumers

> Note: Most  changes to API will break 3rd party consumers of your API (if you have any that you support), even though it may not break your own built clients.

You can handle these kinds of changes with standard API version strategies, and in developer documentation.

#### Aggregate Identifiers
- **ID formats** - Changing identifier structure should not break code, but may confuse those diagnosing the system, or the consumers of logs

### Safe Change Strategies

When you need to make potentially breaking changes:

1. **Versioning** - Create new versions alongside old ones
2. **Additive Changes** - Add new fields/endpoints rather than modifying existing ones
3. **Deprecation** - Mark old functionality as deprecated before removal
4. **Migration Scripts** - Provide automated migration for data changes
5. **Feature Flags** - Use feature flags to control rollout of changes
6. **Backward Compatibility** - Maintain support for old contracts during transition periods

### Change Review Process

Before making any change, ask:
1. **Will this break existing clients?** - If yes, consider versioning or additive approach
2. **Will this affect persisted data?** - If yes, plan migration strategy
3. **Will this change event contracts?** - If yes, consider event versioning
4. **Is this change reversible?** - If no, ensure thorough testing and gradual rollout