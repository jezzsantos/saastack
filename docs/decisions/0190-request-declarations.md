# Request Declarations

* status: accepted
* date: 12-09-2023
* deciders: jezzsantos

# Context and Problem Statement

We have chosen to simplify the declarative design of request and responses in C# using the [REPR design](https://deviq.com/design-patterns/repr-design-pattern) pattern as described [in this decision](0050-api-framework.md).

It turns out that current version of ASPNET (8.0) and the Open API library [Swashbuckle](https://github.com/domaindrivendev/Swashbuckle.AspNetCore), do not fully support the use of custom request types with properties that use the C# `required` keyword.

There are good technical reasons for this.

* The C# `required` keyword on a property of a custom type, that has no data in an incoming JSON request, will cause the `JsonSerializer` being used in ASPNET to throw deserialization exceptions. We would prefer that the `JsonSerializer` ignore the `required` keyword and force the property to have a `null` value. Since we will detect that in request validators.

* Swashbuckle does not yet infer the Open API "required" attribute on properties using the C# `required` keyword.

Both of these issues require us to use different declarative patterns in the C# classes that model incoming HTTP requests, that correctly conform to C# language. Albeit a compromise.

## Considered Options

The options are:

1. UseNullable - Define all reference type properties as nullable (e.g., `string?`), and use the `[RequiredAttribute]` to inform Open API attributes.
2. FixSerializer - Fix/Extend the `JsonSerializer` in ASPNET 8.0, and extend the Swashbuckle inference for Open API.
3. ChangePattern - change the declarative pattern to something else. For example, turn off `<Nullable>disabled</Nullable>` for all reference types in the assembly, where all requests happen to be defined.

## Decision Outcome

`UseNullable`

- Defining all "required" properties as nullable conforms to standard nullable reference type checking in C#.
- However, it does require compromises:
    - We are forced to have to teach, understand and accept the technical details underlying this constraint.
    - We have to explicitly override null checks in all API request deconstruction code (ugly).

- If we find a better solution in ASPNET 8.0 later we can easily revert all the code to change the pattern.

## More Information

[Issue in Swashbuckle](https://github.com/domaindrivendev/Swashbuckle.AspNetCore/issues/2764) for supporting the C# `required` keyword.