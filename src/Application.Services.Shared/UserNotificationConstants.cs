namespace Application.Services.Shared;

public static class UserNotificationConstants
{
    public static class EmailTags
    {
        private const string Authentication = "authentication";
        private const string EndUser = "user";
        private const string Invitation = "invitation";
        private const string PasswordCredential = "password";
        private const string PasswordMfa = "mfa";
        private const string Registration = "registration";
        public static readonly IReadOnlyList<string> RegistrationRepeatCourtesy = new List<string>
        {
            EndUser, Registration
        };
        public static readonly IReadOnlyList<string> PasswordResetUnknownUser = new List<string>
        {
            EndUser, Authentication, PasswordCredential
        };
        public static readonly IReadOnlyList<string> PasswordResetInitiated = new List<string>
        {
            EndUser, Authentication, PasswordCredential
        };
        public static readonly IReadOnlyList<string> PasswordResetResend = new List<string>
        {
            EndUser, Authentication, PasswordCredential
        };
        public static readonly IReadOnlyList<string> RegisterPerson = new List<string>
        {
            EndUser, Registration
        };
        public static readonly IReadOnlyList<string> InviteGuest = new List<string>
        {
            EndUser, Invitation
        };
        public static readonly IReadOnlyList<string> PasswordMfaOob = new List<string>
        {
            EndUser, Authentication, PasswordMfa
        };
    }
}