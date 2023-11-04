namespace Tools.Analyzers.Platform;

public static class AnalyzerConstants
{
    public static readonly string CommonDomainNamespace = "Domain.Common";
    public static readonly string[] PlatformNamespaces =
    {
#if TESTINGONLY
        "<global namespace>",
#endif
        "Common",
        "Infrastructure.Common", "Infrastructure.Interfaces",
        "Infrastructure.Persistence.Common", "Infrastructure.Persistence.Interfaces",
        "Infrastructure.Web.Api.Common", "Infrastructure.Web.Api.Interfaces",
        "Infrastructure.Web.Hosting.Common",
        "Application.Common", "Application.Interfaces",
        "Application.Persistence.Common", "Application.Persistence.Interfaces",
        CommonDomainNamespace, "Domain.Interfaces",
        "IntegrationTesting.WebApi.Common", "UnitTesting.Common"
    };

    public static class Categories
    {
        public const string Documentation = "SaaStackDocumentation";
        public const string WebApi = "SaaStackWebApi";
        public const string Ddd = "SaaStackDDD";
    }
}