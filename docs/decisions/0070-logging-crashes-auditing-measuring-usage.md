# Logging Crashing Auditing Measuring Usage

* status: accepted
* date: 2023-09-24
* deciders: jezzsantos

# Context and Problem Statement

Every developer understands the basic idea behind logging, and every developer turns logging ON for their software (at some point), because every developer (at some point) needed logging output to diagnose issues in a running system.

However, not every developer correctly takes care of crash reporting when the system dies, nor thinks about auditing of the use of the software (for legal purposes), nor measuring certain aspects of the software (e.g., perf or usage), nor capturing usage metrics about how the software is actually used by end-users, until very late in the business. And often, only when it is too late to have any past data to learn from.

All of these needs are ever-present in every SaaS business from day one, and collecting (and managing) that data is just another thing that adds value later on when you really need it (in the future).

Logging, specifically, can create a significant load on a cloud-based system, if not handled effectively and can slow down the performance of each HTTP request that uses it (imagine how much more time and risk of failure it would be to post each logging message to a remote logging service). It is not unusual to record 10-100 log messages for any specific API call (depending on its level of complexity). That API call may also require 1-5 audits, and raise 1-10 usages. Most modern logging frameworks deal with this kind of load asynchronously in a background thread, however, handling any of the other 4 mechanisms (i.e., Crashes, Auditing, Usage, Measures) requires significant engineering to offload to asynchronous mechanisms so that the API caller is not delayed while this data is processed.

Rather than separating these concerns and having 5 different disparate ways of capturing all these different kinds of data (some data which is duplicated between the 5 mechanisms) we wish to have one abstraction to take care of it all. Why? because:

1. Developers of SaaS systems need to be aware of these 5 things at all times while designing their software. So having it in one abstraction reminds them of those 5 things.
2. There needs to be an easy and consistent way to capture these 5 kinds of data (and reuse it between these 5 different mechanisms). Rather than engineering 5 different ways, and injecting 5 different things into the code.
3. A single well-known abstraction will need to be provided to work across any cloud platform (e.g., Azure, AWS, GoogleCloud) in any deployment topology (e.g., Monolith, Microservices, etc.).

## Considered Options

The options are:

1. A Recorder
2. 5 Different mechanisms
3. Just use a logging framework

## Decision Outcome

`A Recorder`

- `IRecorder` is a single interface to access these 5 services
- We can plug in the standard .Net `Ilogger` to take care of diagnostic logging.
- We can use ports and adapters to plug in any 3rd party system to handle all 5 mechanisms
- We will need to use a reliable messaging mechanism (i.e. queues) to offload this load on HTTP requests