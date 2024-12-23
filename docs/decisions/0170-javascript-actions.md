# JavaScript Actions

* status: accepted
* date: 2024-10-05
* deciders: jezzsantos

# Context and Problem Statement

In the design and implementation of FrontEnd JavaScript clients, developers often spend a lot of time writing boilerplate code to perform the same tasks to improve the usability of the UI.

In many cases, many of the standard considerations of improved usability that should be common across all UIs are forgotten by the developer as they focus only on getting the UI and controls they care about - just working. Once released, and in the wild, the end user often experiences poor usability for very basic needs. This damages the perceived quality of the product. Here are some very common examples:

Example 1: Offline status

The device that the browser is running on (i.e., a desktop, a mobile phone, etc.) loses internet connectivity, and the UI does not prevent the user from submitting data/input they are working with (sometimes that can be a significant amount of data the user had to provide). The UI will fail in some way when a command that the end user makes requires an API call across the internet. The API call will eventually time out (offline), and then it's the question of how the developer designed the code to handle this error, if at all. Here are some possibilities:

* The developer didn't bother to design for this scenario at all. The API call is issued when the browser is 'offline', and returns to the JS with an 'offline' error, but the error was not handled, the UI likely did nothing, and the user is unaware that there was an error at all.
* The end user waits, and waits, confused that the app is unresponsive to them, and they likely retry the command (click the "submit" button) over and over again as if to "wake up" the unresponsive app to respond to them immediately.
* Obviously, this may work in some cases for the end user, but it is also likely that the command they issued will be tried and retried over and over again, and even if the browser re-establishes connection during this process, or not this may also cause subsequent errors, but they remain unaware of those also.
* If the connection is still offline, the user won't even be aware of that, unless they check their device.

A better way to handle this scenario is to let the end-user know they have lost connection as soon as it occurs or wait until any API call indicates 'offline' status and let the user know about it until the connection is restored again.

Then, to take it a step further, prevent the user from making the API call in the first place until the connection is restored. In other words, disable the controls that make API calls, until the connection is restored.

At the very least, handle the error properly, informing the user that they are offline and asking them to retry when 'online' status has been established.

Furthermore, when API calls are being made, and we have 'online' status, we also would want to do this:

1. Indicate the API is being made, by indicating busy status. For example, a spinner on the "Submit" button
2. Disable the "Submit" button until the API call returns (except for offline indicators)

In all web forms, we would expect all the conditions to be handled for the developer automatically, and ideally they never have to think about it. It would be built in for them.

Example 2: Form Validation

When an end user is working with a form and entering user input, we would expect that invalid input would be detected as soon as it is entered. We would expect the form to indicate that, and we would expect the form to prevent submission until all data is present and valid. Many developers forget to wire all that up correctly, and some allow forms to be submitted by calling APIs that return `HTTP 400 - BadRequest` error, and thus they allow the user fail the command.

The basic design principle should be that the client application should NEVER allow a `HTTP 400 - BadRequest` error to be returned from an API call (except in uncontrollable edge cases), because the client should work be designed very meticulously to guard against that happening to the end user.

A better experience for the end user is to disable the "submit" button that gathers the user's input and issues the API call until all the data (and context) is valid.

Example 3: Unexpected/Expected Errors

In general, developers, under pressure, usually only design for the happy path and often forget to design for exceptional cases. This is very common for developers who don't design their software with automated tests; thus they are not reminded to consider what could go wrong. Further, there is great effort when only manually testing UIs even for the happy paths, let alone all the error cases.

What is needed is to ensure that no matter what happens, when an API call fails to execute, perhaps it is a timeout (due to network connectivity), perhaps it is an unexpected defect in the API (i.e., an `HTTP 500 - InternalServerError`) or perhaps it is a legitimate `HTTP 405 - MethodNotAllowed` error from the API saying that something isn't in a state to be changed. No matter what, the UI is responsive to the error, and the end user is notified that there was an error and, ideally, what to do about it.

Last point to make about this. It is a well-known design principle in clients that errors that are returned from API calls and servers are not designed to be consumed by end users. They are designed to be consumed by the developers building the clients. Therefore, developers should never just pass on the error to the end-user that they get from the backends. Many, many developers forget this and do what's expedient, and the end user has a terrible time trying to figure out what they can do with these esoteric developer-oriented errors.

## Considered Options

The options are:

1. Provide a standardized, easy-to-apply, and consistent mechanism to handle all these common cases.
2. Leave it up to each engineer to know about and handle all these cases individually and in bespoke ways.

## Decision Outcome

`Standardized`

- We can enforce this as the basic building block of UI components, supported by a set of common components, and we can define common patterns of implementation to follow, and easy to extend.
- Handling offline capability is a general concern and can be made into a general mechanism across the codebase.
- Handling validation is very common, too, and can be baked into all interactive forms
- Handling XHR API calls can be easily standardized and made extensible for all use cases.
- The end users get a consistent and responsive set of experiences, particularly in exceptional cases.

## (Optional) More Information

We have decided to define the "[JavaScript Action](../design-principles/0200-javascript-actions.md)" to provide the standardized experience we are after.