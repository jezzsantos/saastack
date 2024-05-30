# Developer Tooling

This page summarizes the developer tooling that is included in SaaStack to help developers be successful with the framework and patterns included within.

## Overview

Sample code templates, of the type that software vendors like Microsoft produces are great for learning new concepts. So are many of the training videos out there on youtube, trying to explain a cool single concept. Beyond that, these examples are very problematic to copy and commercialize as they establish no architectural styles or enduring structure upon which to build real systems.

> There is very good reason for this for vendors. Vendors like Microsoft simply cannot afford to commit to supporting any one architectural style, over others, because it will not be fit for purpose for the whole market they sell to. They are after selling to the largest market, as opposed to serving any niche market. General purpose tools and advice for the largest possible market influence. Leave the niche specific tools for the relatively smaller players in the partner networks. Its all upside for everyone.

It requires decades of experience of building software from first principles (and many mistakes) to establish long lived patterns that teams can be effective with in new products. Unfortunately this skill is far rarer than the industry would like to believe (in the growing pool of software development talent out there), and as a result many software teams are starting up (in a myriad of different kinds of contexts) without this experience on board when they begin. And with that inexperience, they fall back unto naive templates and code samples, which ultimately lead to unstructured codebases with very naïve patterns in them, with lack of abstraction and basic design patterns for managing the long term future of the product. Particularly if "data-modeling" and relational databases are the predominant design pattern. These kinds of codebases very quickly evolve into big balls of mud, with a giant monolithic database at their center, and ultimately this leads to one of two outcomes: A successful business cannot scale, due to lack of funding, and rewrites or rage quits ensue.

The second problem that product teams have is that the tools they use. Their favorite tools tend to be general purpose tools, that they know so well from previous employ. For example, all IDEs like Visual Studio, VS Code and JetBrains Rider are, by design, general purpose and highly extensible tools aimed at every kind of software development.

The problem with any general purpose tool, is that a tool designed to do anything (be default), is not very good at any doing one specific thing.

> However, most of these tools can be extended with custom tooling, which is a saving grace.

As a general principle, the best tools to use for resolving a specific problem with a specific solution, are specialized tools that are designed specifically to understands that problem and the solution, its variance and the constraints that you specifically face. So that it becomes easier to do just the things you need to do, and do they very well.

In the world of software, such tools as called Domain Specific Workbenches or Languages (DSLs). The real world problem with DSL's is, well, creating them in the first place to model the problem and the solution well enough, at the right level, and then keeping them up to date as things change - as they should, all the time. All those challenges require not just a tool that can do that, but also a person with enough skill to model the problem and solution domain. Lastly, the skill to keep it up to date, as things change.

Many attempts at creating the kinds of tools that actually forge these DSL tools, in a way that is easy enough for anyone to use, often fall flat on their face when it comes to modeling technology problems, such as laying down architectural styles of the type we need in product codebases. It is technically possible, and the author has built many of those tools in the past (like [NuPattern](https://github.com/NuPattern) and [automate](https://github.com/jezzsantos/automate)) but it turns out that a "good enough" place to start (for most developers) is with the code patterns in a specific codebase itself.

But having these patterns is not enough, especially when you are trying to lay them down, teach them, stick to them, and enforce them for consistency across a codebase and a team. All it takes is one developer on the team who doesn't understand them (well enough) to workaround around them (the way they know), and that can cascade quickly into other workarounds being formed and the entire system decaying into disorder again. Even when the whole team is onboard, it only takes someone new to the team (who wasn't there at the start or who does not have the experience to understand the context of why these things exist the way they do) before they are compromised.

General purpose tools and tooling (like IDEs from the major vendors) do nothing to help you protect any of that. In fact, general-purpose tooling actively fights against your intentions to protect that. Since they all aim to make violating your constraints as easy as possible for the developer since they are simply ignorant of your constraints. You have no choice but to build specialized tools to do the protection and enforcement for you.

As such, in SaaStack, we have not only laid out durable architectural styles and patterns, but we have also provided the custom tooling that enforces the constraints that underpin and govern the pattern's existence.

There is no single set of tooling that does that in SaaStack in its entirety; instead, there is a carefully selected combination of well-known tools (that are already available in most general-purpose IDEs) that work together to define and enforce these patterns and architectural constraints.

For example,

* In a Clean/Onion/Hexagonal Architecture (which SaaStack advocates), we have several well-known horizontal layers. These layers have a specific direction of dependencies.
* In DDD (which SaaStack backends also advocate), subdomains mark the boundaries between aggregates and their data, and coupling between them is forbidden. Not just at the code level but also in the data repository.
* And finally, in an Event Driven Architecture (which SaaStack backends advocate), domain events should be emitted from the Aggregates and brokered to other components and MUST cross boundaries (both vertical Subdomain boundaries and horizontal Layer boundaries), and so they must be behavior-less data containers (a.k.a DTOs) that are [JSON/XML] serializable.
* With all these constraints, in the "heat of the moment" when time is tight, and the pressure is on, despite best intentions, developers can easily violate any one or all these rules/constraints by simply selecting the wrong type from an intelli-sense picker (or from predictive AI like GitHub Copilot) and never know they have violated any constraint at all. Often, not until months later when this discovery now costs too much to remediate - slowly cascading to disorder again.

What is really needed is custom tooling that will call these things out when they happen and force the developer to change their mistake to conform to the established patterns there and then as they change the software.

Furthermore, as time passes, the codebase itself should change and evolve as the product grows. This MUST happen regardless. So, the tooling that supports the codebase MUST also evolve and move forward with the codebase. Thus, both the codebase itself and the tooling need to co-exist in the same codebase and should evolve together so they are always in sync with each other. This is a key attribute of this class of tooling.

Now, let's get into all the tooling that exists in SaaStack that pulls this all together into a cohesive toolset.

## Code Formatting

> WARNING: This feature is only available in JetBrains Rider

SaaStack utilizes a comprehensive set of code formatting rules that JetBrains Rider (ReSharper) defines. These rules govern everything from how files are laid out to how variables are named and how whitespace, line breaks, and brackets are formatted.

> We don't currently have much support for EditorConfig, a smaller subset of those rules.

These settings are all saved in the codebase for the whole team to use (team-settings). This is important for consistency across the codebase and the team.

In general, SaaStack is compliant with many of the defaults in JetBrains Rider, with a few minor exceptions, and these notable exceptions:

* Formatting Rules:
  * Member variables are always prefixed with an underscore (i.e., `private string _name;`)
  * `if` statements are always enclosed in braces.
  * Various other common formatting rules to increase readability.
* File Ordering: We have changed the order of fields, constants, constructors, factories, methods, and other known kinds of methods to make it easier to find various common things, for specific file types.

## Project Templates

> WARNING: This feature may only available in JetBrains Rider and Visual Studio

TBD - how to install them, when to use them. Reference the how-to-guides

## File Templates

> WARNING: This feature is only available in JetBrains Rider

TBD

## Live Templates

> WARNING: This feature is only available in JetBrains Rider

TBD

## Code Generators

> a.k.a Roslyn Source Generators

TBD

## Code Analysis

> a.k.a Roslyn Analyzers

TBD

Others:

Specific class properties:

* Events properties

* ValueObject properties

* Resource properties

## Architectural Unit Tests

TBD

## Swagger UI

> a.k.a OpenAPI documentation

We provide Swagger UI tooling for both the frontend and backend APIs.

For Backend API: you can find Swagger UI a the root URL of the site `/`

From Frontend API: you can find the swagger UI at : `/swagger`
