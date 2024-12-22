# HTTP Logging

* status: accepted
* date: 2024-12-22
* deciders: jezzsantos

# Context and Problem Statement

Logging HTTP request and responses seems like a completely reasonable thing to do to support most web products, when it comes to diagnosing production issues from production log files. Without basic data in the log traces (particularly, in lieu of stack traces), it becomes harder to piece together a cohesive picture of why certain things failed. Were they expected and programmed faults or were they unexpected errors?

For example, on Azure if you are using Application Insights monitoring you might be tracing through the messages and exceptions that have been caught and you might want to see what went wrong, including some fine details that help you create a more complete picture of a problem.

### Performance

With ASPNET (for many decades) it has been difficult to access the request and response streams that are part of the
`HttpRequest` and
`HttpResponse` objects in memory, and thus hard to see the contents or request or responses (that would otherwise be visible by the clients).

There are many very good reasons for creating this [difficulty] in terms of optimizing performance to keep web server secure, using low resources and operating fast. Streams can be large, particularly if dealing with large datasets or images or files. Copying them in memory can starve web servers of memory and reduce performance significantly during average or peek loads.

### Security/Privacy

On the security front, data in the requests and in responses of HTTP calls can contain a great many secrets, and confidential/personal data. This is a major privacy and compliance concern for your customers (see, GDPR wrt logging data). It is also a well know attack vector for hackers (see [OWASP cheat sheet](https://cheatsheetseries.owasp.org/cheatsheets/Logging_Cheat_Sheet.html#attacks-on-logs)).

For example, consider logging the request body of the following request:

```json
POST {{apihost1}}/passwords/register
Accept: application/json
Content-Type: application/json

{
  "EmailAddress": "auser@company.com",
  "Password": "myfavoritepassword",
  "FirstName" : "afirstname",
  "LastName" : "alastname",
  "TermsAndConditionsAccepted": true
}
```

This data would end up in log files and copied, possibly even backend up for many eyes to see!

It is generally a very poor practice to allow this kind of data end up in transactional log files that people in the company may have a chance to peruse. As well as that these files can become the target for malicious attacks by various parties inside and outside the company.

### Workarounds

For these very good reasons (and several others) Microsoft have deliberately NOT shipped easy means to capture the Request and Response bodies, and allow an engineer to easily write them to log files.

For example, if using Application Insights, you can easily extend the framework and write custom code that accesses the request and response bodies and write them into log traces but to do this, you have to compromise speed and security to do it. There are many very poor and naive examples of doing that, out there in the wild.

In later versions of .NET (> 6.0), Microsoft recommends instead using the supported middleware called
`HttpLoggingMiddleware`  which will output various parts of the HTTP requests and responses into the standard
`ILogger` interface. Details on configuration and use are [well documented, here](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/http-logging/?view=aspnetcore-8.0).

However, Microsoft also clearly warns readers against the dangers of this approach unless you accept the performance degradation AND you go out of your way to ensure that you do not leak confidential/personal information into your log files. To do this well and comprehensively is non-trivial and beyond most engineers capability to do ins a sustainable and maintainable way over long periods of time - even with years of experience in this area.

These caveats, may seem pretty easy to mitigate against for most engineers (who are hell bent on solving this problem, and making this happen), but the truth is, that this task and the risk associated to it, is extremely hard to mitigate over the lifecycle of any product as things change. At best you can only minimize it temporarily.

> Having actually tried to achieve this in several products, and given the extent of work required to mitigate all the risks exceptionally well, experience has taught us that this solution is best avoided altogether, and seek alternative solutions.

In principle, instead of desiring to see all requests and response contents, in reality there is a always only a subset of data that would need to be surfaced to suffice for most diagnostic efforts.

Here is the summary:

* All `HTTP 5XX` errors from an API (API issues), and all
  `HTTP 4XX` errors from an API (client problems) all come with a standard
  `ProblemDetails` structure that is [RFC7807](https://datatracker.ietf.org/doc/html/rfc7807) compliant.

All of these errors populate the body of the HTTP response, with a known error structure.

As long as we log those details, we should be able to enhance the logs to provide clues as to the cause of the problem being diagnosed.

This approach should cover much of the ground of response bodies that would be desirable in diagnostic scenarios.

Going much beyond this requirement, will require significant investment in producing more details, and will introduce performance issues and security issues long term.

We STRONGLY advise against tampering with the `HttpRequest` and
`HttpResponse` streams in ASPNET, as well as against exposing the contents of all requests and responses to log files (i.e., Bodies or Query Strings), given the potential to store and leak sensitive/private/confidential data.

## Considered Options

The options are:

1. Custom 4XX/5XX response handling - detect 4XX/5XX errors and output the response bodies of those only.
2. Custom Telemetry Enrichment/Sanitization Only - enrich diagnostic telemetry with the contents request and response bodies, for all requests
3. ASPNET HTTP Logging - enrich diagnostic logs with the contents request and response bodies, for all requests (that is echoed to telemetry)

## Decision Outcome

`CustomHandling`

- We don't need to process all requests/responses, only responses that fail with 4XX/5XX errors.
- We don't need to process any requests, and thus we don't need to sanitize confidential/personal data
- We assume that all RFC7807 responses will not contain any confidential/personal information, and thus no need to sanitize
- There is no significant performance degradation from processing (potentially large) streams of data
- The custom implementation is easy to understand and adapt.

## (Optional) More Information

Please see the `HttpRecordingFilter` for implementation details.