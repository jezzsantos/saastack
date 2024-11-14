namespace Application.Interfaces;

public static class WorkerConstants
{
    public static class Queues
    {
        public const string Audits = "audits";
        public const string Emails = "emails";
        public const string Provisionings = "tenant_provisionings";
        public const string Smses = "smses";
        public const string Usages = "usages";
    }

    public static class MessageBuses
    {
        public static class Topics
        {
            public const string DomainEvents = EventingConstants.Topics.DomainEvents;
            public const string IntegrationEvents = EventingConstants.Topics.IntegrationEvents;
        }

        public static class Subscribers
        {
            public const string ApiHost1 = "ApiHost1";
        }
    }
}