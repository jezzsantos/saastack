namespace ArchitectureTesting.Common;

public static class ArchitectureTestingConstants
{
    public static class Layers
    {
        public static class Domain
        {
            public const string AllOthersLabel = "Other Subdomains' Domain Layer types";
            public const string DisplayName = "Domain Layer";
            public const string PlatformProjectNamespaces = $@"^{ProjectSuffix}[\w\.]*$";
            public const string ProjectSuffix = "Domain";
            public const string SubdomainProjectNamespaces = $@"^[\w]+{ProjectSuffix}[\w\.]*$";
        }

        public static class Application
        {
            public const string AllOthersLabel = "Other Subdomains' Application Layer types";
            public const string DisplayName = "Application Layer";
            public const string PlatformProjectNamespaces = $@"^{ProjectSuffix}[\w\.]*$";
            public const string ProjectSuffix = "Application";
            public const string SubdomainProjectNamespaces = $@"^[\w]+{ProjectSuffix}[\w\.]*$";
        }

        public static class Infrastructure
        {
            public const string AllOthersLabel = "Other Subdomains' Infrastructure Layer types";
            public const string ApiHostProjectsNamespaces = @"^Api[\w]+Host[\w\.]*$";
            public const string DisplayName = "Infrastructure Layer";
            public const string PlatformProjectsNamespaces = $@"^{ProjectSuffix}[\w\.]*$";
            public const string ProjectSuffix = "Infrastructure";
            public const string SubdomainProjectNamespaces = $@"^[\w]+{ProjectSuffix}[\w\.]*$";
        }
    }
}