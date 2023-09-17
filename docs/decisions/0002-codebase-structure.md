# Codebase Structure

* status: accepted
* date: 2023-09-17
* deciders: jezzsantos

# Context and Problem Statement

As we are defining an opinionated codebase as a template for SaaS businesses to adopt and start with, we necessarily need to define the: naming, structure, and look and feel conventions of the solution, projects, files, and folders of it.

We need to do this in a way that both the buyers and the users of the template will quickly understand and agree with. They will likely be unfamiliar with many of the architectural styles encoded in it, and they will unlikely be spending too much time trying to study the details of the actual code, before making a decision to adopt or not.

There are many ways to organize a codebase and many ways to view it. For example, if you open the `SaaStack.sln` file in Visual Studio or in JetBrains Rider, you see a far more structured view of solution folders, than you would if you open the `src` directory structure, in any tool. But given that we have decided that [JetBrains Rider will be the recommended editor](0005-dev-editor-dotnet.md), we will optimize for a solution structure over a file structure.

There are really two main common ways that developers organize their codebases.

By far the most common way, is to model the technical aspects of the codebase. Another is to model the domain aspects of the codebase.

### Technical aspect modeling

Modeling technical aspects looks like grouping your files into top-level folders like `Controllers` and `Services` `Filters` and `Endpoints` where the developer groups by the "technical" category of the code. We expect to see a rather flat structure overall, rather than deeply nested structures.

File names, class names, and namespaces often include the name of the programming concept in the architecture/framework. Like: `CustomerController`  and `EmailPort` and `SendGridAdapter`.

The taxonomy used for many of these codebases optimizes for keeping the same types of "things" in the same folders, but also across a global scope. Vertical slice segregation is not strongly represented but implied.

Knowing how all the code components are *implicitly* related is often done with file name prefixes like `Customer`, or by defining subfolders like `Customer` under the `Controllers` top-level folder. This can result in fatigue, as the 30 things that make up a `Customer` slice are scattered throughout 30 subfolders of the respective technical parts they have.

It is very common that many other codebase templates (similar to this one) focus more on structuring the codebase on the technical aspects of it rather than what the things actually do.

For example, [DDD Forum](https://github.com/stemmlerjs/ddd-forum) you can see this taxonomy throughout the structure (even within the modules themselves)

> We believe the reason for this strong tendency towards taxonomizing on technical aspects of codebases comes from familiarity and following the numerous code samples provided by software vendors, like Microsoft, and from the myriad of simple sample projects that are used to teach these technical aspects to developers. After adopting these samples, is how most developers learn about new technologies.

### Functional aspect modeling

> Functional here, means the function of something, not a reference to functional programming.

Functional modeling focuses on communicating something about what the code elements do (at a higher abstraction level), rather than reflecting the specifics of the frameworks it works with.

Since [DDD is the chosen modeling tool](0040-modeling.md) for this codebase, we would expect to see all the subdomains of the codebase manifest immediately at the top level of the codebase when we open it up. These subdomains would then contain all the pieces they need to work, in the sub-structures within a subdomain.

Filenames should just reflect the name of the concept being modeled, rather than prefixed with the class of object it is. Namespaces and project names have already aggregated the kinds of "things" that are in that container to begin with.

### Hybrids

But let's face it: all codebases define some kind of framework that they operate with or within.

Especially when it comes to reusable components in the codebase (for example, the infamous 'utils'), it makes more sense to taxonomize them by their technical aspect than any functional aspect they may or may not have.

We would expect to see for most codebases, a hybrid approach for structuring its pieces, depending on what they are and how they are used in that codebase.

## Considered Options

The options are:

1. Hybrid Structures
2. By Vertical Slice
3. By Technical Aspect

## Decision Outcome

`Hybrid Structuring`

- We believe that most of the focus in the early days of building a new product will be on the use cases of subdomains. Thus we should organize around subdomains.
- In a code template like this, there will be stable 'Core' subdomains that will only need infrequent attention, once configured (e.g. `UserAccounts`, `Organisations`, `Ancillary,` etc.) then there will be the specific subdomains to this specific product that are being explored most of the time. Segregating those subdomains will be useful.
- Common components (that are scoped to be reused only by components in each horizontal layer) are best contained in projects related to that layer. In the absence of specificity of a subdomain, those common/shared components are better organized by a horizontal layer. We would expect to see a hierarchy of Common/Shared components, where there is more of a focus on technical aspects, as these components apply to all components in the layer of all subdomains.
- For better abstraction, there is no need to directly reflect the names of components for specific frameworks in the namespaces and type names of things. This is an undesirable form of coupling to 3rd party frameworks and libraries.  
