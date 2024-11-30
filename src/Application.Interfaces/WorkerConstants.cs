using Application.Interfaces.Services;

namespace Application.Interfaces;

public static class WorkerConstants
{
    public static class Queues
    {
        // EXTEND: Add other queues here
        public const string Audits = "audits";
        public const string Emails = "emails";
        public const string Provisionings = "tenant-provisionings";
        public const string Smses = "smses";
        public const string Usages = "usages";
        /// <summary>
        ///     Defines the Api endpoints for delivering the messages to the respective queues
        /// </summary>
        public static readonly Dictionary<string, Func<IHostSettings, (string BaseUrl, string HmacSecret)>>
            QueueDeliveryApiEndpoints =
                new()
                {
                    {
                        Audits,
                        settings => (settings.GetAncillaryApiHostBaseUrl(),
                            settings.GetAncillaryApiHostHmacAuthSecret())
                    },
                    {
                        Emails,
                        settings => (settings.GetAncillaryApiHostBaseUrl(),
                            settings.GetAncillaryApiHostHmacAuthSecret())
                    },
                    {
                        Provisionings,
                        settings => (settings.GetAncillaryApiHostBaseUrl(),
                            settings.GetAncillaryApiHostHmacAuthSecret())
                    },
                    {
                        Smses,
                        settings => (settings.GetAncillaryApiHostBaseUrl(),
                            settings.GetAncillaryApiHostHmacAuthSecret())
                    },
                    {
                        Usages,
                        settings => (settings.GetAncillaryApiHostBaseUrl(),
                            settings.GetAncillaryApiHostHmacAuthSecret())
                    }
                    // EXTEND: Add other queues and endpoints
                };
    }

    public static class MessageBuses
    {
        public static class Topics
        {
            // EXTEND: Add other topics here
            public const string DomainEvents = EventingConstants.Topics.DomainEvents;
            public const string IntegrationEvents = EventingConstants.Topics.IntegrationEvents;
        }

        public static class Subscribers
        {
            // EXTEND: add other subscribers here
            public const string ApiHost1 = "ApiHost1";
        }
    }
}