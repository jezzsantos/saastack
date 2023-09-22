# Logging Crashing Auditing Measuring Usage

* status: accepted
* date: 2023-09-24
* deciders: jezzsantos

# Context and Problem Statement

Every developer understanding logging, and every developer turns on logging for their software (at some point), because every developer (at some point) needed logging output to diagnose issues in a running system. However, not every developer correctly takes care of crash reporting, when the system dies, nor thinks about auditing of the use of the software (for legal purposes), nor measuring certain aspects of the software, nor capturing usage metrics about how the software is actually used, until very late in the business. And often, only when it is too late to have any past data to learn from.

All of these needs are present in every SaaS business, and collecting (and managing) that data from day one is just another thing that adds value later on when you need it.

Rather than separating these concerns and having 5 different disparate ways of capturing this data (some data which is duplicated between the 5 mechanisms) we wish to have one abstraction to take care of it all. Why? because:

1. Developers of SaaS systems need to be aware of these 5 things at all times while designing their software. So having it in one abstraction reminds them of those 5 things.
2. There needs to be an easy and consistent way to capture these 5 kinds of data (and reuse it between these 5 different mechanisms).
3. A single well known abstraction will need to work across any cloud platform (e.g. Azure, AWS, GoogleCloud) in any deployment topology (e.g. Monolith, Microservices).

## Considered Options

The options are:

1. A Recorder
2. 5 Different mechanisms
3. Just use a logging framework

## Decision Outcome

`A Recorder`

- `IRecorder` is a single interface to access these 5 services
- We can plugin in the standard .Net `Ilogger` to take care of diagnostic logging.
- We can use ports and adapters to plugin any 3rd party system to handle all 5 mechanisms