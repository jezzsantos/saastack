# OpenApi Docs

* status: accepted
* date: 2024-05-02
* deciders: jezzsantos

# Context and Problem Statement

Adding OpenAPI documentation to the project will help developers better understand the API and how to use it.

In .net8.0 there is partial support for OpenAPI by ASP.NET Core, but it is not as feature rich as the Swashbuckle or NSwag libraries.
That wil ultimately change in .net9.0 as Swashbuckle has gone out of support. As per [this announcement](https://github.com/dotnet/aspnetcore/issues/54599)

We need the following capabilities.

1. Provide a developer fo the API some documentation about the API.
2. Enables the developer to try out the API with a built-in tool
3. Allows the backend to publish to other tools that can derive code from the API documentation. For example, JS/TS classes to be consumed by the WebsiteHost

## Considered Options

The options are:

1. Swashbuckle
2. NSwag
3. AspNetCore OpenAPI

## Decision Outcome

`Swashbuckle`

- It is mostly supported in .net8.0.
- It does the basics of what we need now, with some custom extensions
- NSwag has serious issues with correctly generating "multipart/form-data" data requests
- We will switch to .net9.0 when released