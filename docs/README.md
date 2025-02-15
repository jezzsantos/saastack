# Documentation

[TOC]

## Getting Started

This repository is a template. The main idea is that you copy (or fork) the whole repo, modify it to be yours, and use it to start building out your new product.

Follow the [Clone and Own](CLONE_AND_OWN.md) guidelines when you are ready to get started, and make this your own.

## All Use cases

Take a look at all the [public API use cases](design-principles/0000-all-use-cases.md) for this software, there are already over 75 public APIs defined, that you don't have to build over again.

## Architecture Design Records

Browse our [Architectural Design Records](decisions/README.md) that describe the key assumptions and decisions made behind some of the bigger decisions made in this code template.

## Design Principles

Learn about the [Design Principles and Implementation details](design-principles/README.md) of the code template. What we aimed to achieve, and how we go about doing it.

## How-To Guides

Read the [HowTo Guides](how-to-guides/README.md) to help you get started and perform the most common tasks in working with and customizing this codebase template to suit your product needs.

## Tooling

As well as a code template, there is custom tooling (tailored to this codebase) to guide you to being more productive with using this template.

We make extensive use Roslyn Analyzers, Code Fixes and Source Generators and Architecture tests to help you and your team be highly productive in following the established patterns this codebase. And more importantly detect and fix when those principles are violated.

For more details, see the [Developer Tooling](design-principles/0140-developer-tooling.md) documentation.

For example, we make it trivial to define robust REST APIs, and under the covers, the tooling converts those API definitions into minimal APIs for you. But you never have to write all that minimal API boilerplate stuff, or worry about how it is organized in code. This is all hidden away from you, not requiring any input from you.

Furthermore, we have many Roslyn analyzers that continuously check your code to make sure that you and your team do not inadvertently violate certain architectural rules and constraints that are in place to manage the complexity of the source code over time, as your product develops. It is like having continuous code review, and your own plugins that understand your code.

For example, clean architecture mandates that all dependencies point inwards from Infrastructure to Application to Domain layers for very good reasons. But developers, in the heat of the moment, can easily violate that rule by simply adding a `using` statement the other way. Your IDE will make this easy for you, and it doesn't actually care, because it does not care about architectural rules. Your code review process may miss that subtle violation. However, our Roslyn rules certainly won't miss that violation, and they will guide you into fixing it.

Lastly, if you are using JetBrains Rider, we have baked in a set of common coding standards that are enforced across the codebase for you.
We also provide you with a number of project templates for adding the various projects for new subdomains.
We also give you several macros in the text editor (a.k.a. Live Templates) for creating certain kinds of classes, like DDD ValueObjects and DDD AggregateRoots, and xUnit test classes.

You can see all of these things in the Platform/Tools projects.

## Deployment

The codebase is ready for deployment immediately, from your GitHub repository.
Deployment can be performed by any tool set to any environment, any way you like, we just made it easy for GitHub actions.

For more details, see the [Deployment](DEPLOYMENT.md) documentation.