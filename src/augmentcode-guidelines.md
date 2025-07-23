# SaaStack Development Guidelines for Augment Code

## Core Principles
- Always follow Domain-Driven Design (DDD) over data modeling
- Use Hexagonal/Clean Architecture with dependencies pointing inward only
- Design for modularity to enable scale-out
- Be consistent across the entire codebase above all else

## Domain Layer Rules
- Aggregates generate their own unique identifiers
- Validate all data entering the domain using class factories that return Result<Error>
- Ensure aggregates are never in invalid state
- ValueObjects are immutable, Entities/Aggregates are mutable
- Use Result<Error> return values instead of throwing exceptions for control flow

## Application Layer Rules
- Avoid Transaction Scripts and anemic domain models
- Use CQRS pattern - commands delegate to aggregates, queries go to read models
- Convert data to ValueObjects for Domain Layer
- Convert domain states to shared DTOs/Resources

## REST API Standards
- Model real-world processes, minimize RPC usage
- Follow Level 3 Richardson Maturity Model
- REST over CRUD - model business processes, not database operations
- Resources are nouns, actions are verbs
- Use ASP.NET Minimal APIs over Controllers
- Follow REPR design pattern

## API Implementation Pattern
```csharp
public sealed class [Domain]Api : IWebApiService
{
    [AuthorizeForAnyRole(OrganizationRoles.Manager)]
    public async Task<ApiGetResult<T, TResponse>> Method(TRequest request, CancellationToken cancellationToken)
    {
        // Implementation
    }
}
```

## Error Handling Rules
- Use exceptions for exceptional cases (invalid program state, assumption violations)
- Use Result<TValue, TError> for expected errors (validation failures, business rule violations)
- Don't catch exceptions you cannot handle - let them propagate
- Catch-and-wrap exceptions only to add diagnostic context, then re-throw

## When to Use Each Pattern
- Expected Errors → Result<T, Error> pattern
- Exceptional Cases → Exceptions
## Validation Standards
- Use FluentValidation for request validation
- Validate at domain boundaries - all data entering domain must be validated
- Create domain-specific validations in Validations classes
- Use Matches() with domain validations instead of Must() assertions
- Centralize validation logic for reusability

## Validation Pattern Example
```csharp
RuleFor(req => req.Field)
    .NotEmpty()
    .Matches(Validations.Domain.FieldValidation)
    .WithMessage(Resources.Validator_InvalidField);
```

## Preferred Code Patterns
| Instead of | Use | Why |
|------------|-----|-----|
| `string.IsNullOrEmpty(value)` | `value.HasNoValue()` | More readable extension method |
| `!string.IsNullOrEmpty(value)` | `value.HasValue()` | More readable extension method |
| `collection.Any()` | `collection.HasAny()` | More expressive intent |
| `!collection.Any()` | `collection.HasNone()` | More expressive intent |
| `.Must(x => x == "value")` | `.Matches(Validations.Domain.Field)` | Centralized validation logic |
| `throw new Exception("error")` | `return Error.Validation("error")` | Use Result pattern for expected errors |
| `string? value = null` | `Optional<string> value = Optional<string>.None` | Explicit optional values |

## Test Naming Conventions
- Success cases: `WhenCondition_ThenSucceeds()`
- Failure cases for errors: `WhenCondition_ThenReturnsError()`
- Failure cases for exceptions: `WhenCondition_ThenThrows()`
- 
## Breaking Change Rules
NEVER change without explicit approval:
- Domain Events (structure, names, semantics)
- ReadModels (database schema, query contracts)
- Message Bus & Queue Contracts (formats, names, semantics)
- Public API Contracts (DTOs, endpoints, auth requirements)
- Aggregate Identifiers (formats, generation strategy)

## Safe Change Strategies
- Use versioning for potentially breaking changes
- Make additive changes rather than modifying existing
- Use deprecation before removal
- Provide migration scripts for data changes
- Use feature flags for rollout control

## Development Process
1. Identify use cases for your subdomain
2. Group use cases around common concepts
3. Define aggregates for each concept
4. Implement domain logic with proper validation
5. Create application services to orchestrate domain operations
6. Build REST APIs following established patterns
7. Write comprehensive tests for all scenarios

## Code Organization
- Separate concerns into layers: Domain, Application, Infrastructure
- Organize by subdomain rather than technical concerns
- Use ubiquitous language from the domain
- Keep validators with their corresponding request types

## Dependency Rules
- Minimize the number of dependencies from nuget or npm
- Always use package managers (dotnet, npm) instead of manual file editing
- Use built-in .NET DI container
- Register services with appropriate lifetimes:
    - AddSingleton for stateless services
    - AddPerHttpRequest for request-scoped services
- Inject interfaces, not concrete types
- Keep dependencies minimal and focused

## Testing Requirements
- Unit tests for individual components
- Integration tests for APIs
- Use [Trait("Category", "Unit")] for unit test categorization
- Mock external dependencies in unit tests
- Test both success and failure scenarios comprehensively
- Use FluentAssertions for readable test assertions