namespace Common;

public static class RuntimeConstants
{
    public static class Dotnet
    {
        public const string RuntimeVersion = "8.0.6"; //Must also match the MSBUILD variable <RoslynTargetFramework>
        public const string SdkVersion = "8.0.302"; //Download and Installer version
        public const string Version = "8.0";
    }
}