# Framework or Template

* status: accepted
* date: 2023-08-15
* deciders: jezzsantos

# Context and Problem Statement

The ultimate goal of SaaStack is to save start-ups time & money by providing a ready-to-go codebase that they can focus on what makes the business unique rather than spending time and money on the things that are common with all SaaS software products.

There have been millions of attempts by developers (all over the world, and every day) at trying to save others time and money by providing them with tools (that they have built) and now want others to use and appreciate. In that respect, SaaStack is really no different.

Common ways we experience developers sharing their code, patterns, and tools:

- They build and distribute a library (in binary form, like a nuget/npm package)
- Build and distribute a framework (in binary form, like a nuget/npm package)
- Build and make accessible source code to their libraries, frameworks, code snippets, etc to be built and maintained by someone else (in source code form, like a GitHub project)

Library or Framework? The size and perceived complexity of the SaaStack codebase will probably be seen to be of the type "framework" (as opposed to being a "library") since it describes certain structured solutions and abstractions at many layers of the code, taking care of many concerns. It also describes where to put your code and the interfaces available to it, which is a common characteristic of frameworks. It would appear just like any other framework that developers experience, despite the fact that it does not come in binary form.

There are well-known issues with adopting other people's frameworks:

- **Learning curve**: Frameworks often have a steep learning curve, meaning that you need to invest a lot of time and effort to understand them, what the pieces are and where they go.
- **Overhead**: Code frameworks are generalized solutions and often introduce some overhead, such as additional abstractions, dependencies, and configuration, that can affect the conciseness and sometimes the performance of your code in your specific context. They often include extensibility points and features that you may never use.

There are also particularly difficult issues to deal with if they are packaged, versioned, and controlled by those other people:

- **Customization**: Frameworks may limit your ability to customize your application to your specific needs and preferences.
- **Standards**: You often have to follow the conventions and rules that the framework defines and enforces, which may be different than what you are comfortable with, and will need to be acceptable to you.
- **Lock-in
  **: Using a framework can also lead to "lock-in" where you are tied to a specific set of technologies and unable to switch to alternatives easily. This can make it harder to adapt to the changing needs and environment of your specific context. You also become dependent on the framework developers for support and maintenance, which can pose risks if they stop updating or fixing the framework, or ignore your requests to alter it for your specific context.

Lock-in is the perhaps biggest issue encountered, and it will almost always be encountered at some point in time in the future. It can become a real show-stopper. When it happens, the framework will certainly get in your way, and if it cannot be adapted the way you need it to adapt, then you end up fighting against it. At this point, it is now working against you (besides the fact that it has helped you for years before that point), and all you can do is find ways to work around it.

> Why is that? It is because all frameworks present a "domain-specific language" (DSL) to address a specific problem space, and when that DSL no longer addresses your particular problem space, the framework will need to be changed. If it doesn't support the relevant extensibility points to let you do that, you hit a brick wall.

The number one problem for consumers of frameworks when this happens is that they are not in control of changing the framework to adapt it to suit their specific needs at that moment in time. That's because, in most of these cases, the framework (and its abstractions, its features, and its extensibility) is controlled by someone else. As a consumer of it, your only choices are to either stop using it or coerce the owner of it into making the change the way you want it made. But, in order to do that, they need to do it in a way that works for all the consumers they have for it. Not only that, but you will want it resolved now, and waiting for them (and their release cycles) may not be an option for you.

This one fact (about being controlled by someone else) is often overlooked as being something that can be resolved in another way.

What if you controlled the framework's abstractions, features, and extensibility? When you hit a problem with them, you just changed them then and there? Then, the only remaining challenge is to understand the existing abstractions well enough to be able to adapt them the way you want to and to do that safely (i.e. without breaking any existing abstractions and their existing consumers). If they are simple abstractions and familiar to you, then this may not be a big problem. If those abstractions and design constraints were suitably explained (by documentation) and also backed up with regression tests, then this makes it far, far easier to move forward with changing it yourself.

Today, we have well-understood and advanced techniques to do this well (i.e. practicing YAGNI, SOLID, KISS, DRY principles, with extensive test automation). You are already employing these things on every software project already - they are not new. You probably already continuously refactor your abstractions, enhance your features, and make things as extensible as you need to when you build new systems. It is just part of the development of all software these days.

So, how different is it doing that for the framework that you are already working within? The truth is that every codebase is, in fact, already its own domain-specific language with its own abstractions, whether you built it to be that way or not. A codebase framework is simply a more advanced version of what you will build in a few years down the track.

Acceptance. Developers (as a mass audience) can be a prickly bunch, and building tools for them is challenging because those tools need to be adaptable enough to be easily applied to different contexts.

> Hence, only highly generalized (and un-opinionated) tools are provided by large software vendors (e.g., Microsoft). Domain-specific tools that may have more value to you, are only made available in specific contexts.

We want to mitigate or completely avoid the known existing problems with adapting \[binary distributed\] frameworks that are owned by other people.

We also want to avoid esoteric and arcane abstractions that are unfamiliar to you and hard to understand easily enough to change and adapt when the time comes. Thus, we need to stick with well-known patterns that are commonly seen by developers who are traversing different contexts in this space.

We believe that many of the big problems that are commonly experienced with adopting other people's frameworks will be mitigated in SaaStack with careful design, following commonly accepted conventions, and by using the [principle of least astonishment](https://en.wikipedia.org/wiki/Principle_of_least_astonishment) in its abstractions and tooling.

Finally, codebase templates have one other untapped advantage that is not possible in \[binary distributed\] frameworks. That is an opportunity to include within the code framework itself several contextual examples of using it. That would otherwise be described in words in accompanying documentation.

## Considered Options

The options are:

1. Codebase Template
2. 3rd party Framework (binary distributed)
3. 3rd party Framework (source code distributed)

## Decision Outcome

`Codebase Template`

- A codebase template comes with its own example code already in the codebase. Developers can see how to solve the problems they have from code that is already working in situ the code that they are already working in.
- A codebase template is not owned by any other party. It is controlled by the people consuming it. If any part of it needs changing or adding to, the people who require the change can simply make the change, then and there, exactly the way they want it. It is backed up with regression tests to ensure that the change can be made quickly and safely.
- Changes or fixes to the codebase template (after a derivative project has cloned it) will be very challenging to integrate forward to derivatives (as the derivatives can significantly diverge). However, derivatives can still take advantage of those improvements by cherry-picking them from the original codebase template. There can be no dependency in the reverse direction, from derivative back to original.
