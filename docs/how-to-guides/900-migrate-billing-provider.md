# Migrating To Another Billing Provider

## Why?

You have reached the point where you need to integrate with a real Billing Management system (BMS). like [Chargebee](https://www.chargebee.com/), [Maxio (formerly Chargify)](https://www.maxio.com/subscription-management), [Recurly](https://recurly.com/), [Zoho](https://www.zoho.com/us/billing/), or [Stripe Billing](https://stripe.com/billing).

## What is the mechanism?

There are two pieces of this mechanism:

1. An implementation of an `IBillingProvider` specific to the BMS.
2. Webhooks, or custom syncing mechanisms to ensure that changes in the BMS reach this product.

> We highly recommend using Webhooks notifications where possible; otherwise, you must poll the BMS frequently and you risk being rate-limited.

## Where to start?

Initially, SaaStack comes configured with a built-in `IBillingProvider` called `SimpleBillingProvider`.

This provider is a stand-in provider to be used in the early days of product development until the point you decide to replace it with an integration to a third-party Billing Management System (BMS), such as [Chargebee](https://www.chargebee.com/), [Maxio (formerly Chargify)](https://www.maxio.com/subscription-management), [Recurly](https://recurly.com/), [Zoho](https://www.zoho.com/us/billing/), and [Stripe Billing](https://stripe.com/billing).

At that point, you may have already onboarded numerous new customers, and each of them is going to have an already created billing `Subscription`.

Now, the job will be to migrate the data that has been captured by the current `IBillingProvider` and use it to pre-populate subscriptions in your chosen third-party BMS.

> The last thing you would want is to manually input the data you have already collected from existing customers into your new BMS.

The good news is that we have built support to make this process straightforward.

If you want to minimize interrupting service, you will need to do some planning and determine a moment in time to make the switch.

## Preparing for Migration

Depending on the BMS you select, they will each have their own conceptual models of how to manage the billing subscriptions of your customers.

The first step is to learn about how your customers are modeled in the BMS software.

For example, in Chargebee, your customer (i.e., `Organization` + "Buyer" in the product) will be modeled as a Chargebee Customer record, and a billing subscription (i.e., `Subscription` in the product) is modeled as a Chargebee Subscription. Chargebee has a notion of a Plan, but that has no real equivalent concept in the product, except for the `TierId` and the `PlanId`.

Other BMS will have slightly different conceptual models, and you will need to understand them first, and how they map to the concepts in the product.

You will also need to configure the basic rules and other policies in the BMS first, before you start configuring your customer data.

For example, configure API Keys, Webhooks, etc.

The last thing will be to explore whether the BMS supports a "sandbox" environment for you to play around with and test your migration. You don't want to be adding test data to your production customer data.

> You may need to sign up for a free plan first to play around with and explore the options, and then use a paid version for your production data.

### View the available data

Use the API endpoint `GET /subscriptions/export` to view the data available for export into your chosen BMS.

> Note: this is a protected endpoint that you can only access with HMAC secrets

This data represents all the subscriptions created in the product so far.

This is the data you will need to import into your chosen BMS during the migration.

> Note: some of the values are simply encoded JSON values

```json
{
  "subscriptions": [
    {
      "buyer": {
        "Address": "{\"City\":\"\",\"CountryCode\":\"NZL\",\"Line1\":\"\",\"Line2\":\"\",\"Line3\":\"\",\"State\":\"\",\"Zip\":\"\"}",
        "CompanyReference": "org_SmpntwRFK0OOtQcoMu9N7g",
        "EmailAddress": "auser@company.com",
        "Id": "user_KSASWz7eUq6zcVeUbGSzw",
        "Name": "{\"FirstName\":\"afirstname\",\"LastName\":\"alastname\"}"
      },
      "buyerId": "user_KSASWz7eUq6zcVeUbGSzw",
      "owningEntityId": "org_SmpntwRFK0OOtQcoMu9N7g",
      "providerName": "simple_billing_provider",
      "providerState": {
        "BuyerId": "user_KSASWz7eUq6zcVeUbGSzw",
        "SubscriptionId": "simplesub_4a84ac3c69c344568cca2867c29d6cc0"
      },
      "id": "billsubscript_MkiRvPBa0i4e7C2yjn3AA"
    }
  ],
  "metadata": {
    "filter": {
      "fields": []
    },
    "limit": 100,
    "offset": -1,
    "total": 1
  }
}
```

### Build Your Migration Scripts

You will likely need to build some scripts that translate the raw data above and automate the creation of various related data structures in the new BMS.

For example, in Chargebee:

1. You would create a Chargebee Customer record using the data in the `buyer` property. You would save the `buyer.id` in the metadata of the Chargebee Customer record.
2. You would create a Chargebee Subscription for the Chargebee Customer. You would also save the `id` and `owningEntityId` as metadata in the Chargebee Subscription.
3. You would define some Chargebee Plans and assign one of those plans to the Chargebee Subscription.

Next, during the migration, once you have automated the creation of the BMS records, you will also need a collection of metadata of those BMS records back into the data of the `IBillingProvider` for when it is being used.

Use the API endpoint `POST /subscriptions/{Id}/migrate` to copy the data from your BMS into the product.

For example, for Chargebee, we would be saving, at the very least, the following data:

1. The Chargebee `CustomerId`
2. The Chargebee `SubscriptionId` and `SubscriptionStatus`
3. The Chargebee `PlanId`

> There are a total of about 15 other properties about the Customer, Subscription, Plan, and the payment method that will be stored by the `IBillingProvider` over the course of the lifetime of a subscription. See the implementation of the `ChargebeeBillingProvider`.

Next, you would need to test and refine these scripts thoroughly so that they are reliable when run during the migration with hundreds/thousands of subscriptions (depending on how many customers you have at the time).

### Configure Your New Plans

In your chosen BMS, you will need to design and define the new pricing plans you intend to support for all your customers moving forward in this BMS.

> If you are using the `SimpleBillingProvider` prior to this step, you won't see much plan information in the exported data from the previous step, that's because this provider does not maintain much plan information at all. It hardcodes a single plan for everyone's use. You can find that single hardcoded plan information in the `InProcessInMemSimpleBillingGatewayService`.
>
> You also need to remember that the `SimpleBillingProvider` has everyone on a "free" plan that requires no payment method and does not support a Trial period.

This means that when you import these subscriptions (created by the `SimpleBillingProvider`) into your new BMS, you need to import them into a "free" plan that does not require them to have a valid payment method. If migrating from the `SimpleBillingProvider`, your customers will not have provided any payment method yet.

In the product, by default, we have defined the following tiers (see: `SubscriptionTier`):

* Standard
* Professional
* Enterprise

You are free to rename, add, or remove these tiers (in the code) to whatever you would like to support in your future pricing plans in your new BMS. Essentially, we have 3 paid tiers, where `Standard` may have a trial and is generally the default plan for new users.

> Remember, if you modify these tiers, you will also need to modify the mapping between these tiers and the feature levels you will be supporting in your pricing plans. see the `EndUserRoot` for details.

In your BMS, we recommend defining at least the following plans:

1. `Free` - a "freemium" type plan to move your customers on to, to begin with (must not have a trial period unless you intend to force all your existing customers to eventually provide a payment method before the trial ends).
2. `Tier1` - equivalent to `Standard` that might have a Trial period. This could be the initial plan that you want new users to be put on, and a trial applied. Or you may want new customers to go on the `Free` plan.
3. `Tier2` - equivalent to `Professional`, which would be a paid tier.
4. `Tier3` - equivalent to `Enterprise`, which would be a higher paid tier.

> You are free to name your plans whatever you like in your BMS. Those names and descriptions will be shown to your customers.

You will need to define all the parameters for each of these new plans, including pricing, limits, frequency of billing, etc.

### Configure the Billing Provider

Your newly chosen BMS will require a built and tested implementation of the `IBillingProvider` to work with it.

You will also need to build any webhooks or synchronization processes to handle updates originating from the BMS to the `Subscriptions` subdomain so that changes in the BMS update the data kept in the `Subscription` of the product.

> SaaStack comes with a small number of existing `IBillingProvider` implementations already. These can be used and they can be referenced to build your own implementations for other BMSs.

To swap out the existing `IBillingProvider` (e.g. `SimpleBillingProvider`) with your new implementation, you simply change the dependency injection code in the `Subscriptions` subdomain (see: `SubscriptionsModule`).

> You can then delete the `SimpleBillingProvider` and its associated classes and tests. You are very unlikely to revert back to using it ever again.

You will also need to make sure that you provide all the necessary configuration settings for your new `IBillingProvider` in the relevant `appsettings.json` files for the host project where the `Subscriptions` subdomain is deployed.

You might also consider updating your web/mobile apps to support the self-serve of capturing credit cards (payment methods), and support self-serve for changing plans. However, the built-in pricing page in the `WebsiteHost` should already be updated with your new plans.

> None of the BMS-specific UX is built in when using the `SimpleBillingProvider` as this provider neither allows you to select from a list of plans nor captures payment methods.

Finally, when you are satisfied (and tested) that you have a working version of the software with your new `IBillingProvider`, it will be time to plan the switch out.

> Remember that you should not deploy the new `IBillingProvider` before you export the existing subscriptions and migrate them to your new BMS. You might put this new implementation in a tested branch of the code, in preparation however.

## Perform the Migration

Unfortunately, due to the nature of this migration, you are going to need to schedule some kind of outage of your product service while you migrate to your new BMS.

These are the activities to schedule and perform to complete the migration and before you can resume service with the new BMS integration:

1. Export the data from the running product using the endpoint: `GET /subscriptions/export`
2. Immediately shutdown your service, to prevent any new customers signing up (and thus creating new subscriptions). You may need other measures if you have heavy sign-up traffic to ensure no one is signing up while you are exporting the data.
3. Import the exported data it into your new BMS (using your scripts and the BMS API). You are likely going to be scripting the creation of numerous new data structures in the new BMS with this data, and collecting a bunch of key identifiers that are created.
4. Deploy a new version of your software that includes the configured new `IBillingProvider` to your new BMS.
5. With the collected data from the BMS, import that data back into your API (using `POST /subscriptions/{Id}/migrate`)
6. Resume service using your new BMS integration, and test by signing up new users and ensuring that new subscriptions are created in your new BMS. This may be performed in a non-production slot.
7. Resume service for all your customers.

After this migration, any new users that are registered in your product will be automatically integrated into your product, and appear in the BMS. 
