# Billing Integration

* status: accepted
* date: 2024-06-11
* deciders: jezzsantos

# Context and Problem Statement

Most SaaS products charge their customers for use of the product and they receive revenue.

> The rest does not apply to businesses that don't charge for their products

There is a universe of options and possibilities when it comes to:

1. How much to charge,
2. What to charge for (e.g., seats, tenancies, services, etc),
3. Whether there are recurring charges for some things and fixed charges for others,
4. Whether to charge on some schedule (e.g., daily, monthly, annually) and/or usage charges (e.g., per API call, per transaction, storage capacity) etc.

There are infinite possibilities and variations of some or all of the aspects above and more.

At the end of the day, managing these "policies" and keeping them up to date for existing and new customers, and correctly charging the correct amounts at the correct times for the
*actual* service delivered for those customers is (or becomes) extremely complicated for any small, medium or large business venture. For that very reason alone, no product company should be rolling their own Billing Management Systems (BMS) or Payment Gateways (PG). These services are now provided by several vendors all around the world, to such a degree that this is a well-solved and supported solution that should be adopted by all SaaS businesses.

The challenge that SaaS businesses
*should* face instead is how to integrate with these BMS systems to automate as much of this stuff as possible so that a business removes the need to manually provision, manage, apply, or modify any of these policies. The goal for these businesses is for customers to self-serve the management of their usage and charges of the service they desire at every opportunity until manual intervention is absolutely necessary.

> Manual intervention is frequently required and usually performed by some Support/Success function of the SaaS business in response to individual customer interaction.

To make this experience seamless and reliable for customers of SaaS products, those products are required to integrate with BMS systems to facilitate self-service. In many cases, products themselves will be required to gate-keep and govern when it comes to enforcing quotas and limits defined by these billing policies. This requires the products to have up-to-date definitions of each customer's billing policy and enact the constraints defined within it.

Furthermore, the SaaS business's customer support/success functions will be required to centrally manage their customers' data and billing policies, and they will need comprehensive tools to do that effectively and efficiently, when required. These tools are already provided by the BMS vendors themselves as table-stakes for all SaaS products to use. (whether they integrate their products or not).

Thus we have a situation where both the SaaS product and the BMS system are sharing the same set of data and acting upon it. Given each party can change that data, there is a case here for bi-direction syncing of that data to some reasonable timeframe. This is a challenge the SaaS product needs to resolve.

## Considered Options

The options are:

1. Full, 2-way sync with BMS
2. One-way sync with BMS (product -> BMS)
3. No integration with a BMS
4. No BMS

## Decision Outcome

`2-way sync`

- With 2-way sync, data can be easily kept up to date in the SaaS product by means of webhooks and caches, with some acceptable latency period. (i.e. within 1 minute). Data for each tenant in the product can be changed either in-app or in the management portal of the BMS, and these changes will be "eventually consistent" with the customers' experience in-app.
- With 2 way sync, we can employ the use of the mature BMS toolsets, that can be used by non-technical roles (such as finance or customer success) and can be easily cross-referenced by the product (using well-known identifiers in the product)
- With 2 way sync, we can fully automate the self-serve experience for customers in-app. Rather than having to talk to a customer success agent that just uses the BMS tools.
