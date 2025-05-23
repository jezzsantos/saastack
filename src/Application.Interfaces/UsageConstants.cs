namespace Application.Interfaces;

/// <summary>
///     Values used when reporting usages
/// </summary>
public static class UsageConstants
{
    public static class Components
    {
        public const string BackEndApiHost = "API";
        public const string BackEndForFrontEndWebHost = "WEB";
    }

    public static class Properties
    {
        public const string AuditCode = "Code";
        public const string AuthProvider = "AuthProvider";
        public const string AvatarUrl = "AvatarUrl";
        public const string CallId = "CallId";
        public const string CarMake = "Make";
        public const string CarModel = "Model";
        public const string CarYear = "Year";
        public const string Classification = "Classification";
        public const string Component = "Component";
        public const string CountryCode = "CountryCode";
        public const string CreatedById = "CreateBy";
        public const string DefaultOrganizationId = "DefaultOrganizationId";
        public const string Duration = "Duration";
        public const string EmailAddress = "EmailAddress";
        public const string Enabled = "Enabled";
        public const string EndPoint = "EndPoint";
        public const string ForId = "ForId";
        public const string HttpMethod = "Method";
        public const string HttpPath = "Path";
        public const string HttpRoute = "Route";
        public const string HttpStatusCode = "Status";
        public const string Id = ResourceId; // how we identify any resource, including users
        public const string IpAddress = "IpAddress";
        public const string MetricEventName = "Metric";
        public const string MfaAuthenticatorType = "MfaAuthenticatorType";
        public const string Name = "Name";
        public const string Ownership = "Ownership";
        public const string Path = "Path";
        public const string ReferredBy = "ReferredBy";
        public const string ResourceId = "ResourceId";
        public const string Started = "Started";
        public const string TenantId = "TenantId";
        public const string TenantIdOverride = "TenantIdOverride";
        public const string Timestamp = "Timestamp";
        public const string Timezone = "Timezone";
        public const string UsedById = "UserId";
        public const string UserAgent = "UserAgent";
        public const string UserIdOverride = "UserIdOverride";
    }

    public static class Events
    {
        public static class UsageScenarios
        {
            public static class Core
            {
                public const string BookingCanceled = "Booking Canceled";
                public const string BookingCreated = "Booking Created";
                public const string CarRegistered = "Car Registered";
            }

            public static class Generic
            {
                public const string Audit = "Audited";
                public const string GuestInvited = "User Guest Invited";
                public const string MachineRegistered = "Machine Registered";
                public const string Measurement = "Measured";
                public const string MembershipAdded = "Membership Added";
                public const string MembershipChanged = "Membership Changed";
                public const string OrganizationChanged = "Organization Updated";
                public const string OrganizationCreated = "Organization Created";
                public const string PersonRegistrationConfirmed = "User Registered";
                public const string PersonRegistrationCreated = "User Registration Created";
                public const string PersonReRegistered = "User Registration ReAttempted";
                public const string UserExtendedLogin = "User Extended Login";
                public const string UserLogin = "User Login";
                public const string UserLogout = "User Logout";
                public const string UserPasswordForgotten = "User Password Forgotten";
                public const string UserPasswordMfaAssociationStarted = "User MFA Association Started";
                public const string UserPasswordMfaAuthenticated = "User 2FA Login";
                public const string UserPasswordMfaChallenge = "User MFA Challenge";
                public const string UserPasswordMfaDisassociated = "User MFA Disassociated";
                public const string UserPasswordMfaEnabled = "User MFA Enabled";
                public const string UserPasswordReset = "User Password Reset";
                public const string UserProfileChanged = "User Profile Updated";
            }
        }

        public static class Web
        {
            public const string WebPageVisit = "web.page.visit";
        }

        public static class Api
        {
            public const string HttpRequestResponded = "http.request";
        }
    }
}