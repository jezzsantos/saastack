namespace Tools.Analyzers.Core;

public static class AnalyzerConstants
{
    public static readonly string[] CommonNamespaces =
    {
#if TESTINGONLY
        "<global namespace>",
#endif
        "Common", "UnitTesting.Common", "IntegrationTesting.Common",
        "Infrastructure.Common", "Infrastructure.Interfaces",
        "Infrastructure.Persistence.Common", "Infrastructure.Persistence.Interfaces",
        "Infrastructure.WebApi.Common", "Infrastructure.WebApi.Interfaces",
        "Domain.Common", "Domain.Interfaces", "Application.Common", "Application.Interfaces"
    };

    public static class Categories
    {
        public const string Documentation = "SaaStackDocumentation";
        public const string WebApi = "SaaStackWebApi";
    }
}