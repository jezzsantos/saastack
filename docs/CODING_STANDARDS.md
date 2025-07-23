# Coding Standards

The team contributing to this product wish to standardise certain practices, styles, principles and approaches. The document aims to capture most of them.

> At any time, the team is open to revise anything in this document, but because consistency is the key driver behind this document, when things change, they change right across this whole repo. The last outcome anyone wants is inconsistencies that are hard to understand and draw bounds around.

## 🏗️ **Architecture & Design Principles**

### Domain-Driven Design (DDD)
- **Use Domain-Driven Design over Data Modeling** - Focus on modeling behaviors rather than data structures
- **Define discrete boundaries** using aggregates as the smallest atomic unit of state change
- **One root aggregate per subdomain** (ideally) to maintain clear boundaries
- **Aggregates generate domain events** for atomic units of change using pub-sub mechanisms
- **Use `Result<Error>` return values** instead of throwing exceptions for control flow
- **Aggregates generate their own unique identifiers**
- **Validate all data entering the domain** using class factories that return `Result<Error>`
- **Ensure aggregates are never in invalid state** - verify invariants on every state change
- **ValueObjects are immutable** and equal based on internal state
- **Entities/Aggregates are mutable** and equal by unique identifier

### Hexagonal/Clean Architecture
- **Dependencies point inward only** - Domain has no dependencies on Application or Infrastructure layers
- **Application Layer defines external interfaces** for subdomains
- **Avoid Transaction Scripts** and anemic domain models in Application Layer
- **Use CQRS pattern** - commands delegate to aggregates, queries go to read models
- **Application Layer responsibilities:**
  - Stateless/stateful contract decisions
  - Data routing (where to pull/push data and when)
  - Which aggregate use case to invoke
  - Converting data to ValueObjects for Domain Layer
  - Converting domain states to shared DTOs/Resources

### Modularity & Subdomains
- **Design for modularity** to enable scale-out as product grows
- **Segregate aggregate state** - no dependencies between aggregates
- **Group use cases around common concepts** to define subdomains
- **Use real-world terminology** for subdomain names (ubiquitous language)
- **Generic vs Core subdomains:**
  - Generic: Common to all products (Identity, Users, Organizations)
  - Core: Unique to your product (Cars, Bookings in car-sharing example)

## 🌐 **REST API Design**

### RESTful Principles
- **Model real-world processes** as much as possible, minimize RPC usage
- **Follow Level 3 Richardson Maturity Model** for REST maturity
- **Be consistent** across the entire codebase above all else
- **REST over CRUD** - model actual business processes, not database operations
- **Resources are nouns** involved in state changes of real-world processes
- **Actions are verbs** operating on those processes

### API Implementation
- **Use ASP.NET Minimal APIs** over Controllers
- **Follow REPR design pattern** - Request/Response pairs organized in one layer
- **Use source generators** to convert declarative API classes to Minimal APIs
- **Implement pluggable module pattern** for organizing APIs by subdomain
- **Apply cross-cutting concerns** (validation, auth, rate-limiting) at module/endpoint level
- **All API declarations are async** by default
- **Make APIs unit testable**

### API Structure Example
```csharp
public sealed class CarsApi : IWebApiService
{
    [AuthorizeForAnyRole(OrganizationRoles.Manager)]
    public async Task<ApiGetResult<Car, GetCarResponse>> Get(GetCarRequest request, CancellationToken cancellationToken)
    {
        // Implementation
    }
}
```

## 🔧 **Control Flow & Error Handling**

### Exception vs Result Patterns
- **Use exceptions for exceptional cases** - when assumptions about calling context are invalidated
- **Use `Result<TValue, TError>` for expected errors** - validation failures, business rule violations
- **Don't catch exceptions you cannot handle** - let them propagate to terminate program
- **Catch-and-wrap exceptions** only to add diagnostic context, then re-throw
- **Follow Microsoft guidance**: "DO NOT return error codes" for exceptional cases

### When to Use Each Pattern
- **Expected Errors** → `Result<T, Error>` pattern
  - Validation failures
  - Business rule violations
  - Designed failure scenarios
- **Exceptional Cases** → Exceptions
  - Invalid program state
  - Assumption violations
  - Infrastructure failures (when not designed for)

## 🔒 **Validation & Data Handling**

### Validation Strategy
- **Use FluentValidation** for request validation
- **Validate at domain boundaries** - all data entering domain must be validated
- **Create domain-specific validations** in `Validations` classes
- **Use `Matches()` with domain validations** instead of `Must()` assertions
- **Centralize validation logic** for reusability across components

### Nullability Management
- **Enable Nullable Context** (`#nullable enable`) for compiler assistance
- **Use `Optional<T>`** for values that may or may not be present
- **Combine both approaches** - Nullable Context + Optional<T> for comprehensive null safety
- **Avoid nulls in domain logic** - use Optional<T> or Result<T> patterns

## 🧪 **Testing Guidelines**

### Test Naming Conventions
- **Success cases**: `WhenCondition_ThenSucceeds()`
- **Failure cases**: `WhenCondition_ThenThrows()`
- **Use descriptive test names** that clearly indicate the scenario and expected outcome

### Test Organization
- **Unit tests** for individual components
- **Integration tests** for component interactions
- **Use `[Trait("Category", "Unit")]`** for test categorization
- **Mock external dependencies** in unit tests
- **Test both success and failure scenarios** comprehensively

## 📝 **Code Organization & Structure**

### Project Structure
- **Separate concerns into layers**: Domain, Application, Infrastructure
- **Use dependency injection** for loose coupling
- **Keep infrastructure at arm's length** using Ports and Adapters pattern
- **Organize by subdomain** rather than technical concerns

### Naming & Conventions
- **Use ubiquitous language** from the domain
- **Be consistent** with naming across the codebase
- **Follow C# naming conventions** and Microsoft design guidelines
- **Use meaningful names** that express intent clearly

### File Organization
- **Group related functionality** in the same namespace/folder
- **Separate interfaces from implementations**
- **Keep validators with their corresponding request types**
- **Use consistent folder structure** across subdomains

## 🔄 **Dependency Management**

### Dependency Injection
- **Use built-in .NET DI container**
- **Register services with appropriate lifetimes**:
  - `AddSingleton` for stateless services
  - `AddPerHttpRequest` for request-scoped services
- **Inject interfaces, not concrete types**
- **Keep dependencies minimal** and focused

### Package Management
- **Always use package managers** (npm, dotnet, etc.) instead of manual file editing
- **Use appropriate package manager commands** for each technology
- **Let package managers handle version resolution** and dependency conflicts
- **Only edit package files directly** for complex configuration that can't be done via commands

## 📊 **Monitoring & Observability**

### Recording & Logging
- **Use the Recorder pattern** for capturing usage activity, diagnostics, and audit events
- **Implement structured logging** for better search-ability
- **Capture domain events** for audit trails
- **Monitor key business metrics** through domain events

## 🚀 **Development Workflow**

### Getting Started
1. **Identify use cases** for your subdomain
2. **Group use cases** around common concepts
3. **Define aggregates** for each concept
4. **Implement domain logic** with proper validation
5. **Create application services** to orchestrate domain operations
6. **Build REST APIs** following the established patterns
7. **Write comprehensive tests** for all scenarios

### Best Practices
- **Start with domain modeling** before thinking about persistence
- **Focus on behavior** rather than data structures
- **Keep aggregates small** and focused
- **Use domain events** for cross-subdomain communication
- **Design for testability** from the beginning
- **Be consistent** with established patterns and conventions

## Patterns and conventions

The patterns and conventions above are derived from the extensive documentation in the `/docs` folder and represent the established patterns and principles for developing within the SaaStack codebase. Following these guidelines will ensure consistency, maintainability, and alignment with the overall architecture vision.

### Why are they important?

Patterns and conventions are critical for:
- **Developer mobility** - enabling team members to work effectively across different parts of the codebase
- **Consistency** - ensuring predictable code structure and behavior
- **Maintainability** - making the codebase easier to understand, modify, and extend
- **Quality** - reducing bugs through established, proven approaches
- **Onboarding** - helping new team members understand and contribute quickly

## 💻 **Code Replacements & C# Standards**

### C# Coding Standards

We follow Microsoft's C# coding conventions with specific enhancements for readability and consistency. These rules are enforced through Rider settings and code analysis.

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

Use constants from relevant `Constants.cs` files where available, otherwise use descriptive lowercase alphanumeric strings starting with an article (e.g., "anidtoken", "aclientid", "aredirecturi") that describe the variable's purpose.
For real URL values, use "http://localhost", instead of others like "http://example.com"


## ⚠️ **Breaking Changes**

### Understanding Breaking vs Non-Breaking Changes

Breaking changes are modifications that can cause existing functionality to fail or behave differently in deployed production releases. Understanding what constitutes a breaking change is crucial for maintaining system stability.

Some of these things may corrupt data captured while being deployed. So ensure you have good backups of all data repositories.

### Things Designed to Change Safely (Non-Breaking)

The following components have been explicitly designed to evolve without breaking existing functionality:

- **Application Services** - Internal implementation can change as long as interfaces remain stable
- **Infrastructure Adapters** - Can be swapped out using dependency injection
- **Domain Logic** - Internal aggregate behavior can evolve while maintaining public contracts
- **API Implementation** - Internal processing can change while maintaining request/response contracts
- **Validation Rules** - Can be enhanced (made more permissive) without breaking existing clients
- **Configuration Settings** - New settings can be added with sensible defaults


### Things You CANNOT Change (Breaking Changes)

The following changes will have **significant and dire consequences** on already deployed and running Production systems:

#### 1. **Domain Events**
- **Event structure/schema** - Adding required fields, removing required fields, changing field types or names. 
- **Event names** - Renaming event class names will break event handlers when the persisted event is loaded later

Some changes to events are very safe. Consider that the old event was already persisted with data values, and must remain in tack in memory when deserialized later, into your new event class

The event serializer (JSON) will ignore certain fields it cannot match between the old version and the new version. Thus make sure that `required` fields are always satisfied with data values.

If you need to make a breaking change to an event, use the builtin `IEventSourcedChangeEventMigrator` mechanism to map between old and new events classes. You would be expected to add a version number to the new class name to reflect that change. See the migration documentation.

#### 2. **ReadModels (for Event Sourcing)**
- **Database schema changes** - Removing/renaming tables, columns, changing data types
- **Query contracts** - Changing expected result structures
- **Indexes and constraints** - Removing indexes can cause performance degradation

Read Models should be treated as disposable, since the origin data for them is kept in a separate Event Store, and these tables can be rebuilt at any time (with same code) from same events.

You can also create more than one read model at any time, by adding it to the code. This is a better change strategy, that is not possible without read models. So a new version of the code can create a new read model (appropriately named), and that read model may need to be prepopulated with original data before the new code starts using it, while the old table remains in the database. The old table can be removed at a later date (once a rollback is confirmed not to be necessary).

#### 3. **Message Bus & Queue Contracts**
- **Message formats** - Changing message structure breaks consumers
- **Queue names** - Renaming queues breaks message routing
- **Message semantics** - Changing what messages represent

#### 4. **Public API Contracts**
- **Request/Response DTOs** - Removing fields, changing field types, making optional fields required
- **HTTP endpoints** - Changing URLs, HTTP methods, or response codes
- **Authentication/Authorization** - Changing security requirements

Certain kinds of changes will break 3rd party consumers of your API (if you have any that you support), even though it may not break your own clients.

You can handle these kinds of changes with standard API version strategies, and in developer documentation.

#### 5. **Aggregate Identifiers**
- **ID formats** - Changing identifier structure breaks references
- **ID generation strategy** - Can cause conflicts with existing data

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