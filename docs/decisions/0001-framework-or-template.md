# Framework or Template

* status: accepted
* date: 2023-08-15
* deciders: jezzsantos

# Context and Problem Statement

The ultimate goal of SaaStack is to save start-ups time & money by providing a ready-to-go codebase that they can focus on what makes the business unique rather than spending time and money on the things that are common with all SaaS software products.

There have been millions of attempts (by developers all over the world, and every day) at trying to save others time and money by providing them with tools that they have built and now want others to use and appreciate. In that respect, SaaStack is really no different.

Common ways we experience developers sharing their code, patterns, and tools:

- They build and distribute a library (in binary form, like a nuget/npm package)
- Build and distribute a framework (in binary form, like a nuget/npm package)
- Build and make accessible source code to their libraries, frameworks, code snippets, etc to be built and maintained by someone else (in source code form, like a GitHub project)

Now the size and complexity of SaaStack will probably be seen to be of the type "framework" (as opposed to being a "library") since it provides structured solutions and abstractions at many layers of the code, taking care of many concerns. This is just like how many developers experience other frameworks in binary form.

There are well-known issues with adopting other people's frameworks. There are too many issues to go into detail here, but [this article](https://medium.com/@itmarketplace.net/the-problem-with-frameworks-1fafa148dbad) highlights some of the essence of them.

The primary issue that becomes a real show-stopper for just about anyone is that at some future point in time, the framework will get in your way, and if it cannot be adapted the way you need it to adapt, then you end up fighting against it. At this point, it is now working against you (besides the fact that it has helped you for years before that point), and all you can do is work around it.

Why is that? All frameworks present a "domain-specific language" to address a specific problem space, and when that DSL no longer addresses your particular problem space, the framework needs to be changed. if it doesn't support the relevant extensibility points to let you do that, you hit a brick wall.

The number one problem for consumers, when this happens is that they are not in control of changing the framework to adapt it to suit their specific needs at that moment in time. That's because, in most of these cases, the framework (and its abstractions, its features, and its extensibility) is controlled by someone else. As a consumer of it, your only choices are to either stop using it or coerce the owner of it into making the change the way you want it made. But, in order to do that, they need to do it in a way that works for all the consumers they have for it. Otherwise, you are stuck. Not only that, but you need it resolved now, and waiting for them (and their release cycles) may not be an option for you.

This one fact (about being controlled by someone else) is often overlooked as being something that can be resolved in other ways. What if you controlled the framework's abstractions, features, and extensibility? Then, the only remaining challenge left is to understand the existing abstractions well enough to adapt them the way you want to and to do that safely (i.e. without breaking any existing abstractions and their existing consumers).

Today, we have great techniques to do this (i.e. YAGNI, SOLID, KISS, DRY, and test automation). You are already employing them on every software project already. You already continuously refactor your abstractions, enhance your features, and make things as extensible as you need to. It is just part of the development of all software these days. So, how is doing that for the framework that you are already working within? The truth is that every codebase is, in fact, already its own domain-specific language with its own abstractions, whether you built it to be that way or not.

Developers (as an audience) can be a prickly bunch, and building tools for them is challenging because those tools need to be adaptable enough to be easily applied to different contexts.

> Hence, only very generalized tools are provided by the large vendors. Domain-specific tools are only created in specific contexts.

We want to avoid the known existing problems with adapting \[binary distributed\] frameworks that are owned by other people.

We also want to avoid esoteric abstractions that are unfamiliar and hard to understand easily enough to change and adapt when the time comes. Thus we need to stick with well-known patterns that are commonly seen by developers traversing different contexts.

Finally, codebase templates have one other untapped advantage that is not possible in \[binary distributed\] frameworks. That is an opportunity to include within the code framework itself several contextual examples of using it. That would otherwise be described in words in accompanying documentation.

## Considered Options

The options are:

1. Codebase Template
2. 3rd party Framework (binary distributed)
3. 3rd party Framework (source code distributed)

## Decision Outcome

`Codebase Template`

- A codebase template comes with its own example code already in the codebase. Developers can see how to solve the problems they have from code already working in the code that they are working in also.
- A codebase template is not owned by any other party. It is controlled by the people consuming it. If any part of it needs changing or adding to, the people who require the change can simply make the change, then and there, exactly the way they want it. It is backed up with regression tests to ensure that the change can be made quickly and safely.
- Changes or fixes to the codebase template (after a derivative project has cloned it) will be very challenging to integrate forward to derivatives (as the derivatives can significantly diverge). However, derivatives can still take advantage of those improvements by cherry-picking them from the original codebase template. There can be no dependency in the reverse direction, from derivative back to original.
