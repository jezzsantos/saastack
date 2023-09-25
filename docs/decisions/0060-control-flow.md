# Control Flow

* status: accepted
* date: 2023-09-17
* deciders: jezzsantos

# Context and Problem Statement

[Control Flow](https://en.wikipedia.org/wiki/Control_flow) is about managing the flow of control in a program.

Essentially, the question is: how do we control the flow of a program?

Today, (and yesterday) there are two commonly proposed ways to handle control flow, and there is a long history of debate on the topic. To add to that, different programming languages (and their communities) have been optimized for, and have been conditioned to, one way or another. It is not that those communities cannot change, but these communities are large and changes (across the board) take a long while.

So, while there is an academic argument to be made on the universally "right way" to control flow in programs, there are far more important real-world conditions that need more consideration when choosing any way or convention.

> As is common in the programming community worldwide, what is "right" depends on your context, and the conventions being used.

Historically, it is worth knowing a little bit about the background of this topic. Some famous papers have been written (pre-internet) on the subject, most notably:

* [Go-to statement considered harmful](https://www.cs.utexas.edu/users/EWD/ewd02xx/EWD215.PDF) ( Edsger Dijkstra, 1968)
* [Don't Use Exceptions for Flow Control](https://wiki.c2.com/?DontUseExceptionsForFlowControl) (Ward Cunningham, C2 wiki)

It is important to note that the `GOTO` statement (in any language), in and of itself, is not a universal problem with the language - and should not be removed from the language (C# still supports `GOTO`!). The problems come when humans try to understand (and work with) the code that uses `GOTO` statements.

> Computers, after all, have no problem with their use, it is of course, completely unambiguous to a computer.

In reality, the use of `GOTO` statements in code makes it quite hard to design any code that has no idea of the context of where the caller `CAMEFROM` to arrive at any particular line of code - especially at runtime.

### Common control methods

Throwing exceptions (up the stack of a program) is one way to control the flow of a program when bad stuff happens. It essentially short-circuits the program from continuing and by design, terminates the program execution (if unhandled). It is a practice that is common (but not exclusive) in object-oriented languages like Java and C#, C++ etc.

> Throwing exceptions can also be argued to be like using a `GOTO` statement, or some even like to ascribe it to a [`COMEFROM`](https://en.wikipedia.org/wiki/COMEFROM) statement for a laugh.

A (the most widely accepted) alternative approach to throwing exceptions to control the flow of a program, (more common in functional programming languages, since throwing exceptions have side-effects) is to handle returned 'result codes'. Mechanisms to implement this pattern can go by many names - each with its nuances. e.g. [monads](https://en.wikipedia.org/wiki/Monad_(functional_programming)), '[oneof](https://github.com/mcintyre321/OneOf/)', [option](https://en.wikipedia.org/wiki/Option_type), [discriminating unions](https://en.wikipedia.org/wiki/Tagged_union), etc.), which all essentially return to the caller, either an expected result (successful) or an error of some kind (fault/failure) - where they are mutually exclusive. This means that the caller is essentially in control of what to do next, depending on the success or error of any call to another component. This is necessary to eliminate side effects very well.

### Other realities

Every programming language and every runtime platform in commercial use today has its conventions, resources, communities, and eco-systems. These "conventions" are critical for "developer mobility", which is an(oft-underestimated) commercial reality in the commercial software world - often ignored by those who work outside that reality for their own convenience.

> Developer Mobility is the ability of a programmer in one context to easily move to other contexts and provide value there without having to re-skill themselves from scratch. It applies not only to new people entering the industry but also applies to those moving between jobs/contexts in the industry, no matter how skilled or experienced they are. Even though every job/context is unique in some aspects, limiting the cognitive load on moving between contexts is a commercial reality for all businesses. It is why, when you go to find a new job, and you are skilled in one set of technologies/platforms that you know inside out, then you are less likely to aim for or be accepted for a highly qualified position in another set of technologies/platform, and expected to be as effective with it. Conventions help make these transitions easier for everyone.

Throwing and catching exceptions is the convention in the .Net object-oriented set of languages. This mechanism is encoded in all Microsoft libraries and frameworks (available at [nuget.org](www.nuget.org)), whereas using result types is less widely used. Things may change in the future, as more functional concepts are making their way into mainstream.

### General guidance

Thus, despite the academic argument on the "best way", and despite the ongoing arguments between functional and object-oriented programming communities on the "best way", we have to consider the conventions in use in this context and the ecosystems surrounding it.

> Creating new conventions is fine in a codebase if you are setting the conventions across the board. Otherwise, you are fighting the already-established conventions, and it can be confusing for newcomers (to this context) to understand why there are new or opposing conventions.

In the .NET world, Microsoft says this, in their [framework design guidelines](https://learn.microsoft.com/en-us/dotnet/standard/design-guidelines/exception-throwing): "*DO NOT return error codes.*"

The general guidance that most programmers will have heard is: "*Stop throwing exceptions in many places where you shouldn't be*".

This is good advice, but only if you understand what it really means.

There is a lot of confusion and disagreement on "*What is an error?*", "*What is an exception?*", and "*What is an exceptional case?*"

In truth, the answer is inconvenient. It depends on the context, case by case.

> Generalizing all contexts into a single hard and fast rule to be applied in any context is appealing for simplicity and efficiency (and we know engineers love that) but generalization is often where we inadvertently create bigger problems, where other conventions suddenly seem like perfect solutions to those problems.

### What is an error?

If we say that there are places (in your code) where you expect a call to another component that is designed to return an error, then that isn't an exceptional case, that is an expected error.

* In these cases, we would want to make a decision about handling the "error" condition.
* In these cases, using some kind of "result type" is appropriate

But this is not an "exceptional case", it is an "expected case".

> For example, in the kind of system we are building in this codebase, we deliberately apply a validation step to all inbound HTTP requests to ensure that the remote client doesn't provide invalid data to operate on. In this case, the validation step is expected to either succeed or fail. When it succeeds, then the code continues on to another layer, perhaps a processing layer, where other rules are enforced. When it fails, the code processing should not continue, and an error should be reported back to the remote client (along with some helpful details about why). This is by design. Handing a failed validation should absolutely be designed for and dealt with in the code. This is not an exceptional case, and throwing an exception to report this error would also not be appropriate.

### What is an exceptional case?

We could go further and say, that an exception is "*when an assumption about the calling context (being made by the caller) turns out to be invalidated by the component being called*".

Then, in these cases, the component would have to throw an exception, and that should invalidate the flow of control since the setup of the call was "wrong" in some way.

In these exceptional cases, an exception should be thrown, and the program should terminate, because the state of the program is now invalid, and it is no longer consistent. Nothing can be done about it (except perhaps record in detail what happened for later diagnostics).

> This is exactly what the designers of exception handling had in mind when they designed the exception mechanism in the first place.

The caller (receiver of the exception) still has a choice to decide whether it wants to handle the exception or not. This is where we need to apply another piece of guidance: "*Don't catch an exception that you cannot handle*".

* This means that, if you can't do anything about the exception (i.e. compensate for it), then don't `catch` the exception, just let it propagate and do its intended thing - terminate the program.

* However, in general, there is one last thing you can choose to do, even if you cannot deal with the exception, and that is simply to `catch-and-wrap` the exception and provide more context (to it) about where and when your program encountered it, for diagnostic purposes - but always `re-throw` it as intended.

> For example, in the kind of system we are building in this codebase, we are likely to be sending data to remote 3rd party systems, via integrations. We [already know](https://en.wikipedia.org/wiki/Fallacies_of_distributed_computing) that the internet is not reliable, and any call to a remote service could fail for any number of reasons that are out of our control (e.g. service unavailable, or timeout, etc.). But we also know that our requests to those remote services can also be wrong sometimes, from those systems' point of view (e.g. not authorized at this time, bad request, can't do it now the world has moved on, etc.).
>
> We would, therefore have to design a system where we could "retry" a failed remote call (say, due to networking problems), but only up to a point. We don't want to retry forever, and we certainly don't want to keep our remote clients waiting forever while we wait for other services to respond in time.
>
> But what happens when we reluctantly reach that point, where we cannot reach the 3rd party service and complete the request? If reaching that service is critical to maintaining the consistency of our system, then that's probably an exceptional case, unless we designed specifically for it.
>
> If we didn't expect that process to ever fail, and we assumed it would always be completed (even eventually), then failure to complete, is probably an exceptional case.
>
> However, if we designed a process that was resilient to that potential outcome (that we expected could happen), then we would be reporting errors, in some resilient way, not throwing exceptions and bringing the whole process down.
>
> Sometimes, it is easier/less work/more straightforward/less risk/less cost/etc. to throw an exception on failure, and bring the whole process down, to avoid inconsistencies.
>
> Sometimes, we have to work (orders of magnitude) harder with more effort/cost/complexity/risk to design for an expected error and to have other compensating processes to take care of those outcomes.
>
> It depends.

## Considered Options

The options are:

1. Throw Exceptions for exceptional cases, use Results for error cases
2. Result types to return all errors (and exceptions)

## Decision Outcome

`Throw Exceptions`

- Throwing exceptions for exceptional cases is the convention in the .Net world.
   - Yes, they are less performant than handling result types (depending on your context), but that cost in performance was by design in the first place, to achieve the accepted diagnostic outcomes.
   - They at least guarantee that the system will not continue in an invalid state that may result in inconsistent states.

- Using result types for every layer in a large system is a pattern that must be enforced and used "all the way down" the call stack, to ensure correct usage and completeness.
   - It is a staple of functional programming and, yes, can make the code more functional - if that is a design goal
   - It can be easily [demonstrated as powerful in example code](https://m.youtube.com/watch?v=a1ye9eGTB98&pp=ygUXTmljayBjaGFwc2FzIGV4Y2VwdGlvbnM%3D) (as seen here, even by influential sources)
   - However:
      - Result "faults" can be easily ignored by the calling code - if the programmer is not permitted/disciplined enough to handle it properly (or is proficient in its application). The risk is that faults can thus allow inconsistent states of the system to be reached.
      - Matching both cases (fault and success) doubles the number of code paths in every layer above the call, and that doubles the number of tests that need to be written to cover them, too.
      - It makes all the code far more cluttered, and harder to maintain than code where thrown exceptions are used to bypass code.

## More Information

Much has been written about this topic over the decades.

At the moment in the .Net community, there is a resurgence of functional programming influence, and .Net is starting to support the use by the community of more and more of these concepts. Eventually, these concepts may permeate the entire community, and when that happens there will be much more tooling support and the conventions will change across the eco-system. Being early to any new convention risks a reduction in the adoption of this codebase, for those unfamiliar with this convention.

Many .Net developers are curious about these functional concepts (more than others because they are not staple in .Net) to the point that they may want to try them out, to learn more about them themselves, irrespective of whether that is appropriate or sustainable in their current context or not. We've seen this all before and will continue to see this behavior over the years, whenever there is an introduction of new tools/technologies. It is not to be avoided, but it still requires tempering in this specific context.
