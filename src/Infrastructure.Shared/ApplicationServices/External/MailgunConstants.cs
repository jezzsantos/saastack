namespace Infrastructure.Shared.ApplicationServices.External;

public static class MailgunConstants
{
    public const string APIKeySettingName = "ApplicationServices:Mailgun:ApiKey";
    public const string BaseUrlSettingName = "ApplicationServices:Mailgun:BaseUrl";
    public const string DomainNameSettingName = "ApplicationServices:Mailgun:DomainName";
    public const string WebhookSigningKeySettingName = "ApplicationServices:Mailgun:WebhookSigningKey";

    public static class Events
    {
        public const string Delivered = "delivered";
        public const string Failed = "failed";
    }

    public static class Values
    {
        public const string PermanentSeverity = "permanent";
        public const string TemporarySeverity = "temporary";
    }
}