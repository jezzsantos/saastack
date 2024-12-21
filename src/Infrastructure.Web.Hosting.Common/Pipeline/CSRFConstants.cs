using Infrastructure.Web.Interfaces;

namespace Infrastructure.Web.Hosting.Common.Pipeline;

public static class CSRFConstants
{
    public static class Html
    {
        public const string CSRFFieldNamePlaceholder = "%%CSRFFIELDNAME%%";
        public const string CSRFRequestFieldName = "csrf-token";
        public const string CSRFTokenPlaceholder = "%%CSRFTOKEN%%";
    }

    public static class Cookies
    {
        public const string AntiCSRF = HttpConstants.Headers.AntiCSRF;
        public static readonly TimeSpan DefaultCSRFExpiry = TimeSpan.FromDays(14);
    }

    public static class Headers
    {
        public const string AntiCSRF = HttpConstants.Headers.AntiCSRF;
    }
}