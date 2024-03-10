# Introduction

Welcome to SaaStack, and thank you for being curious about how we go about doing things around here.

Since doing this kind of work requires a lot of attention and time, we welcome all submissions and feedback, and will do our utmost to involve your valuable contribution to the future of this technology.

The project leaders of this repository are very experienced developers and have been professionals for many decades. You should be in good hands.

Our goal here is to enjoy the creation of useful and effective software that benefits many people in our community, and that puts emphasis on the quality of their experience over the effort to create that experience. If you want to be a contributor to this project, we hope you are similarly motivated.

### What we are looking for

Essentially, we are building high-quality products (tools) for other developers to make their programming lives better.

There is no suggestion or contribution that you could make that would be too minor or meaningless.

We are all human and we all make mistakes every day, so even minor typos and corrections to any part of this product are always more than welcome.

Improving the clarity of the code and of the documentation is very welcome and super important. No one person can get this right on their own. They only have one perspective, and we are all biased and shaped by our own experiences. And none of us is (or looks like) the end-user of the product we produce. So if you can help shape this product to improve the end user's experience of it, please make a contribution small or large.

There are many ways to contribute to this project:

* From creating videos or writing tutorials or blog posts,
* improving the documentation,
* submitting bug reports, and
* creating feature requests or writing code that can be incorporated into the technology itself.

## Support Questions

If you have questions about the use of this technology, then, please don't use the Issues register on this project site for that purpose.

The Issues register is a place to address bugs and feature requests in the improvement of the SaaStack product itself.

> You can certainly ask questions for clarity on how things work, in the design of SaaStack


Use one of the following resources for questions about using SaaStack or issues with your own code:

- The `#questions` channel on our [Discord server](https://discord.gg/849Wy2Zwgg)
- Ask on [Stack Overflow](https://stackoverflow.com/questions/tagged/SaaStack?tab=Frequent). Search with Google first using: `site:stackoverflow.com SaaStack {search term, exception message, etc.}`
- Ask on our [GitHub Discussions](https://github.com/jezzsantos/saastack/discussions) for long-term discussion or larger questions.

# Code of Conduct

The goal is to maintain a diverse community that's pleasant for everyone. That's why we would greatly appreciate it if everyone contributing to and interacting with the community also followed this [Code of Conduct](CODE_OF_CONDUCT.md).

The Code of Conduct covers our behavior as members of the community, in any forum, mailing list, wiki, website, Internet relay chat (IRC), public meeting, or private correspondence.

> Our Code of Conduct is adapted from the [Contributor Covenant](https://www.contributor-covenant.org/version/2/0/code_of_conduct.html) version 2.

### Be considerate

Your work will be used by other people, and you in turn will depend on the work of others. Any decision you take will affect users and colleagues, and we expect you to take those consequences into account when making decisions. Even if it's not obvious at the time, our contributions to SaaStack will impact the work of others. For example, changes to code, infrastructure, policy, documentation and translations during a release may negatively impact others' work.

### Be respectful

The SaaStack community and its members treat one another with respect. Everyone can make a valuable contribution to SaaStack. We may not always agree, but disagreement is no excuse for poor behavior and poor manners. We might all experience some frustration now and then, but we cannot allow that frustration to turn into a personal attack. It's important to remember that a community where people feel uncomfortable or threatened isn't a productive one. We expect members of the SaaStack community to be respectful when dealing with other contributors as well as with people outside the SaaStack project and with users of SaaStack.

### Be collaborative

Collaboration is central to SaaStack and to the larger free software community. We should always be open to collaboration. Your work should be done transparently and patches from SaaStack should be given back to the community when they're made, not just when the distribution releases. If you wish to work on new code for existing upstream projects, at least keep those projects informed of your ideas and progress. It may not be possible to get consensus from upstream, or even from your colleagues about the correct implementation for an idea, so don't feel obliged to have that agreement before you begin, but at least keep the outside world informed of your work, and publish your work in a way that allows outsiders to test, discuss, and contribute to your efforts.

### When you disagree, consult others

Disagreements, both political and technical, happen all the time and the SaaStack community is no exception. It's important that we resolve disagreements and differing views constructively and with the help of the community and community process. If you really want to go a different way, then we encourage you to make a derivative distribution or alternate set of packages that still build on the work we've done to utilize as common of a core as possible.

### When you're unsure, ask for help

Nobody knows everything, and nobody is expected to be perfect. Asking questions avoids many problems down the road, and so questions are encouraged. Those who are asked questions should be responsive and helpful. However, when asking a question, care must be taken to do so in an appropriate forum.

### Step down considerately

Developers on every project come and go, and SaaStack is no different. When you leave or disengage from the project, in whole or in part, we ask that you do so in a way that minimizes disruption to the project. This means you should tell people you're leaving and take the proper steps to ensure that others can pick up where you left off.

### Set expectations for behavior (yours, and theirs).

This includes not just how to communicate with others (being respectful, considerate, etc) but also technical responsibilities (importance of testing, project dependencies, etc). Mention and link to your code of conduct, if you have one.

# Ground Rules for Contributing Code

Your Responsibilities are:

* Ensure cross-platform compatibility for every change that's accepted. Windows, Mac, Debian & Ubuntu Linux.
* Ensure that all functional code that goes into SaaStack meets all these requirements
    * The change you make compiles, and a final package can still be built from the changed codebase
    * You have unit tests and integration tests to demonstrate how the change is working correctly. They must all be run and pass.
    * Your code is formatted using the configured formatting/linting tools.
    * Your code is self-documenting; the intent of which is easily understandable by other contributors. Comments are not the mechanism to share what the code does, or how, only why.
    * Dead code should be removed. Do not include unused commented-out sections of code.
    * Your code does not break the build, and passes all checks enforced by GitHub Actions.

* Architectural decisions should be discussed with other contributors.
* Create issues for any major changes and enhancements that you wish to make. Discuss things transparently and get community feedback.
* Keep feature versions as small as possible, preferably one new feature per version.
* There are no specific conventions for formatting commit messages, except to ensure that they are meaningful descriptions of the history of change to the codebase. Describe the overall impact of your change, rather than describing the change you made. Make sure to include a reference to any Issue that the work relates to (e.g. `Closes #12` or `Fixes #56` or reference a discussion or design `#45`)
* Coding style and formatting rules will be included in the source code. The development tools that we are using (e.g. Jetbrains Rider) can be used to apply and enforce them. If you are using other tools, (which is fine) you will need to find a way to apply these tools before submitting your code.

# Documentation

## Product Documentation

[This is where](https://github.com/jezzsantos/saastack/tree/main/docs) we have all of our product documentation, aimed at the users of our product, which in this case are primarily developers themselves.

That documentation is intended to be part of the template itself that users of the product will also own themselves.

# Building & testing the Code

## Developer Environment

* Windows or MacOS.
* Jetbrains Rider or Visual Studio (There is most support for JetBrains dotUltimate)
* Install the .NET8.0 SDK (specifically version 8.0.2). Available for [download here](https://dotnet.microsoft.com/en-us/download/dotnet/8.0)
*

> Included in the codebase there are many formatting rules that need to be run before committing your code. These rules may not be supported by other developer tools.

> It is possible to apply for a personal free license for Rider from Jetbrains, for OSS projects

> We have ensured that you wont need any other infrastructure running on your local machine (i.e. SQLServer database), unless you want to run infrastructure specific integration tests.

* Clone the repo locally
* Ensure that you have `.NET8.0` (specifically version `8.0.2`) installed on your system. [Download](https://dotnet.microsoft.com/en-us/download/dotnet/8.0)
* Open the `src/SaaStack.sln` solution

## Building the code

* Build the solution
* or run `dotnet build` on the `src/SaaStack.sln` solution

There are 3 flavors of build configuration: `Debug`, `Release` and `ReleaseForDeploy`, these are very standard build configurations, and they have been enhanced.

* `Debug` includes symbols for debugging, and defines `TESTINGONLY`, it is intended to be used on developers desktops for local development.
* `Release` does not include debugging symbols, and defines `TESTINGONLY`, it is intended to be used to verify the release and run tests in CI.
* `ReleaseForDeploy` does not include debugging symbols, and does not include `TESTINGONLY`, it is intended to be built in CI as a final build.

* `TESTINGONLY` is a conditional compilation variable, that is used in code blocks: `#if TESTINGONLY...#endif`. It is for including code specifically for testing purposes only - not for deploying to Staging/Production environments. (Code surrounded by `#if TESTINGONLY...` is removed by the compiler in the `ReleaseForDeploy` flavor).

> The `Release` and `ReleaseForDeploy` build flavors must pass in GitHub actions to submit changes to the codebase

## Testing the code

* Run all the **unit** tests (`Category=Unit`)
* Run all the **integration** tests (`Category=Integration.Web`)

> All automated tests must pass in GitHub Actions to submit changes to the codebase

## Publishing

> ChangeLogs and Versioning (publicly) are performed by core contributors only.

### Documenting changes

> Releases to this project are a little different than ordinary projects, because we are releasing a codebase template.

When our users first clone this template (as their own), they will be versioning the codebase their own way (using the [CHANGELOG.md](https://github.com/jezzsantos/saastack/blob/main/CHANGELOG.md) included in their copy of the template itself.

> The included [CHANGELOG.md](https://github.com/jezzsantos/saastack/blob/main/CHANGELOG.md) should not be updated by contributors of this project, for changes to this project.

Therefore, our release strategy, and versioning should be captured outside the template itself.

We will maintain the project [CHANGELOG](https://github.com/jezzsantos/saastack/wiki/CHANGELOG) in the wiki, and we will document changes to the template of this project there, so that our users can keep track of changes we make to the template, as things change over time.

### Releases

* A 'release' in this project will contain one or more 'entries', and each 'entry' may relate to one or more individual 'commits' over some (ideally, short) period of time.
* However, each 'entry' that we document in each 'release' (in the CHANGELOG) will attempt to describe a larger capability/feature/fix to the template. Individual git 'commits' are probably not as important to track for our users, as larger capabilities/features/fixes. We expect that our users will need to decide whether to upgrade their copies based on the value of each release, or ignore it.
* Each 'entry' in the log will declare whether the change is breaking.(Non-breaking entries will not need a designation).
* If an 'entry' is designated as breaking then it is also accompanied by a detailed explanation about how it breaks previous copies of the template, and what would need to be changed/migrated by our users to incorporate this change into an existing copy of their template.
* All entries are created in the log as soon as work is complete enough, before a 'release' is created. Those 'entries' are captured in the `[Unreleased]` section at the top of the log.

To version a release:

> We use [SemVer](https://www.semver.org) rules for publishing releases (Major for breaking, Minor and Patch for Non-Breaking).
> If this is a 'pre-release' version (using `-preview`) we only ever increase the Minor number for breaking changes.

1. The `[Unreleased]` section is reviewed, and assigned a new version number, in this format `[x.x.x] - YYY-MM-DD`.
2. It is copied below the linebreak in the document.
3. A new `[Unreleased]` is created at the top of the document
4. A new tag is created on the last commit in `main` branch, in the format: `#vx.y.z` (or `#vx.y.z-preview`)
5. The tag is pushed to the origin

# Your First Contribution

Unsure about how to start contributing to SaaStack? You can start by tackling any of the Issues in the Issues register marked with either the `help-wanted` or `good-first-issue`.

Those kinds of issues should involve a small amount of time and work.

All other contributions are of course also welcome, should you wish to dive in deeper.

### First contribution to open source?

Here are a couple of friendly tutorials you can include: http://makeapullrequest.com/ and http://www.firsttimersonly.com/

> Working on your first Pull Request? You can learn how from this *free* series, [How to Contribute to an Open Source Project on GitHub](https://egghead.io/series/how-to-contribute-to-an-open-source-project-on-github).

# The Submission Process

All contributions should be submitted via a pull request or arranged directly with a core contributor on this project.

Core contributors will:

* Review your contribution (as quick as possible)
* Provide feedback if necessary (as accurate as possible)
* Provide direction on what to do next
* and integrate your contribution when it is accepted

After a core contributor has responded, contributors are expected to:

* Take an action within two weeks to act on any feedback from a core contributor, otherwise, core contributors may close the pull request if it is not showing any activity.

The core contributors [at present] are not working on this project full-time (they have other day jobs and families to raise), and they may live in other time-zones than you, so please allow a day or so to hear back from them. They know what it feels like to be ignored and so pledge to respond in some way as soon as possible.

> If you want to become a core contributor, and you have a good track record on this project, then contact the core contributors directly.

# Community

We have this [discord server](https://discord.gg/849Wy2Zwgg) to talk or discuss with people directly.
