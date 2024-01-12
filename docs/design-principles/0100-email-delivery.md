# Email Delivery

## Design Principles

Many processes in the backend of a SaaS product aim to alert the user to activities or processes in a SaaS product that warrant their attention, and most of these notifications/alerts are ultimately delivered by email (albeit some are delivered by other means, too, i.e., in-app, SMS texts, etc.).

Sending emails is often done via 3rd party systems like SendGrid, MailGun, PostMark, etc., over HTTP. Due to its nature, this mechanism isn't very reliable, especially with systems under load.

* We need to "broker" between the sending of emails and the delivering of them to make the entire process more reliable, and we need to provide observability when things go wrong.
* Since an inbound API request to any API backend can yield of the order ~10 emails per API call, delivering them reliably across HTTP can require minutes of time if you consider the possibility of retries and back-offs, etc. We simply could not afford to keep API clients blocked and waiting while email delivery takes place, let alone the risk of timing out their connection to the inbound API call in the first place.

Fortunately, an individual email arriving in a person's inbox is not a time-critical and synchronous usability function to begin with. Some delay is anticipated.

Thus, we need to take advantage of all these facts and engineer a reliable mechanism.

## Implementation

![Email Delivery](../../docs/images/Email-Delivery.png)

### Sending notifications

Any API Host (any subdomain) may want to send notifications to users.

They do this by calling very specific `INotificationsService.NotifyXXXAsync()` methods.

Injected into the runtime will be an instance of the `INotificationService`, which can then deliver notifications to new and existing users based on communication preferences that those users have set up in the system.

Without such information present in the system (as is the present case), a simple default implementation of the `INotificaitonsService` is being used called the `EmailNotificationService`. This simple implementation is simply going to send an email notification to a user based on the email address. (future implementations may behave differently).

### Sending emails

Receiving emails from a SaaS product such as this one actually represents critical end-user processes, be those just "alert" notifications or instructional "call to action" (CTA) notifications.

These email communications, thus, require reliable delivery in order to ensure the recipient gets the email.

Typically, emails will be actually delivered by 3rd party online services (e.g., SendGrid, MailGun, or PostMark), and those systems can employ their own management and rules for delivering emails. For example, they may support rate limits, daily/monthly quotas, and email templates, and may support blocked email lists, and things like that that prevent emails from being received by recipients.

For most startup businesses using these services, operationally, they will need to manage those systems directly with whatever tools are available.

On the automation side, these 3rd party services may support API integration and also Webhook integrations so that they can "call back" to the SaaS product asynchronously later and report delivery statuses.

#### Reliable delivery

The injected implementation of the `EmailNotificationsService` hands off the scheduling of the delivery to an implementation of the `IEmailSchedulingService`. This service packages up the scheduled email and enqueues it to the "emails" queue, and the thread that sent the notification is returned to immediately. Delivery of the actual email to the 3rd party service is not performed at this point, and the request thread is not blocked waiting for that to occur.

A scheduled email message goes onto the "emails" FIFO queue, where a cloud-based trigger (i.e., an Azure Function or AWS Lambda) picks up the message and calls back the Ancillary API to deliver the message.

This cloud-based mechanism is designed with queues to be reliable in several ways.

1. The queues are always FIFO
2. When a message is "dequeued" (processed) by a "client" (a piece of code like an Azure Function or a Lambda), the message is not removed/deleted from the queue immediately, but it becomes "invisible" to further processing by either the same client or another client. However, this message is only "invisible" for a [configurable] period of time (by default: 30 seconds).
3. The message is only "deleted" from the queue when the client explicitly instructs the queue to delete the message after successfully processing it. Failing to explicitly delete the message from the queue (by the client) returns the message to the queue (making it "visible" again) after the visibility timeout has expired.
4. Also, any exception raised by the client while processing the message will automatically return the message to the queue, making it "visible" to be consumed by another client again.
5. If the client fails to process the message, they can explicitly return the message to the queue, and this then counts as a "try".
6. The queue will keep track of the number of "tries", and when it reaches the [configurable] maximum retries, it will move the message to a "poison" (or "dead-letter") queue
7. Messages in the "poison" queue must be handled manually by operations.

> It is generally recommended to send alerts to the operational team when messages are dropped into "poison" queues.

The cloud-based "client" dequeues one message from the queue and simply forwards the message to the Ancillary API.

If it gets an `HTTP 200 - OK` response, it will delete the message from the queue. If it gets an HTTP - 4XX or HTTP - 5XX response, it will inform the queue and try again until it reaches the maximum number of queue retries. At this point, the message is automatically moved to the "poison" queue.

In the Ancillary API, the message is processed by the injected instance of the `IEmailDeliveryService`.

> The actual injected implementation of the `IEmailDeliveryService` will be responsible for getting the message to a recipient. Typically, via a 3rd party service (e.g., SendGrid, MailGun or PostMark, etc.)

Any exception that is raised from this processing will fail the API call.

### Delivery status

Some 3rd party services may fail to deliver the email message, even though they respond to the delivery request with success.

> They may employ retry strategies themselves, or they may configure policies like clocked email domain lists, etc.

Some 3rd party email delivery services report these kinds of delayed failures via webhooks that would need to be wired into the Ancillary service.
