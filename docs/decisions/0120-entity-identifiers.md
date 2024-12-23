# Entity Identifiers

* status: accepted
* date: 2023-09-14
* deciders: jezzsantos

# Context and Problem Statement

In every REST system resides with resources, and each resource is addressable by a unique identifier (scope: whole system).
In every \[DDD\] domain resides entities, each defined by its unique identifier (scope: whole domain of the system).

Given these two design constraints, we can simplify things by considering a single unique identifier to represent both.

1. These identifiers need to be created and validated (they mut be of a known form).
2. These identifiers need to be guaranteed to be unique in the scope of the whole system.
3. In reality, these identifiers are likely to be seen and referred to by humans, who are both diagnosing the system (when errors occur), and seeing representations of these identifiers in 3rd party systems that are integrated with.

Traditionally, in most digital systems, either integers or GUIDs/UUIDs have been used to meet these constraints.
Better options include Base62 encoded letters, as seen in use by [YouTube](https://www.youtube.com) for their video links, which are shorter and more readable to humans.

*
* UUIDS are a good choice (in terms of guaranteeing uniqueness), but they are clumsy when being consumed by humans.
* Base62 encoded letters are readable, memorable and short.
* There are hybrid options that are both short, and provide the human reader a clue about what the entity is, for which you have the identifier.

Naming schemes that include the kind of entity the id is referring to have proven to be useful in popular SaaS systems, for example in the [Stripe.com API](https://stripe.com/docs/api).

## Considered Options

The options are:

1. Named Short Identifiers
2. Short Identifiers
3. GUIDs/UUIDs
4. Large integers

## Decision Outcome

`Named Short Identifiers`

- Short identifiers try to be as short as possible to guarantee uniqueness, and they use only letters (upper and lower case) to be human-readable
- Named identifiers tell the \[human\] reader what entity the identifier belongs to in cases where the human is dealing with many identifiers at the same time.
- Named identifiers also clear up ambiguity in ID found all over REST JSON responses