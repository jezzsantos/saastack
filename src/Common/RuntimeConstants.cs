namespace Common;

public static class RuntimeConstants
{
    public static class Dotnet
    {
        public const string RuntimeVersion = "8.0.19"; //Must also match the MSBUILD variable <RoslynTargetFramework>
        public const string SdkVersion = "8.0.413"; //Download and Installer version
        public const string Version = "8.0";
    }
}