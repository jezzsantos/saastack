# API Framework

* status: proposed

* date: 2023-09-13
* deciders: jezzsantos

# Context and Problem Statement

When it comes to writing REST web APIs in the template, we want to establish scalable patterns that focus on the API's requests and responses and the cross-cutting concerns involved:

* Routes and verbs
* Authenticated or not
* Authorized or not, by roles with (RBAC)
* Authorized or not, by feature set
* Request Validation
* Rate limiting
* Response Caching
* Response types (e.g. JSON or Stream)
* Mapping etc
* Exception Handling
* etc...

We know that in this architecture, the web API will mostly be delegating to an in-proc Application layer, and so we know that there will not be much code in this layer except to identify the Application Layer call.

We also know that the requests and responses need to be easily referenced by clients and tests.

Thus, we need a simple, structured, and consistent way to define request and response types and decorate them with various attributes.

## Considered Options

The traditional .NET options include:

1. ASP.NET Minimal APIs ([MediatR](https://github.com/jbogard/MediatR)'ed)
2. ASP.NET Minimal APIs (out of the box)
3. ASP.NET Controllers
4. Web frameworks like: ServiceStack

## Decision Outcome

`MediatR'ed Minimal APIs`

- ASP.NET controllers were never a great abstraction to represent REST APIs. They were a poor adaptation of the ASP.NET MVC implementation re-purposed for sending JSON, and long overdue for a redesign. Until recently (< .net 6.0) MVC Controllers were the only choice from the ASP.NET team.
- ASP.NET Minimal APIs (out of the box > .net 6.0) are simple and easy to define for demonstrating example code, but for use in larger systems are very awkward to organize, maintain, test, and reuse, as they are today.
- ServiceStack is an ideal web framework for structuring and handling complex web APIs. It has resolved many of the design challenges that ASP.NET controllers has suffered from. It implements the [REPR design pattern](https://deviq.com/design-patterns/repr-design-pattern) very well indeed, and is a delight to use, in many aspects. However, the framework today has a huge surface area, which we are not interested in leveraging most of. It is not so well known to the wider developer community, in part because it is also licensed per developer for a significant annual fee. This last point disqualifies it for use in this template.
- ASP.NET Minimal API's that are MediatR'ed and that can remove some of the redundancy and tedium of the current MediatR patterns (i.e. one ctor per handler, and duplicate ctor for api collections) can bring a more usable, structured, and testable way to define minimal APIs that is close to the same feel of defining and using ServiceStack API's, that does not change them functionally, but does offer more maintainable ways to define and reuse them across a whole system
