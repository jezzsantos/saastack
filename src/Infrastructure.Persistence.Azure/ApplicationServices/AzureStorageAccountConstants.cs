namespace Infrastructure.Persistence.Azure.ApplicationServices;

public static class AzureStorageAccountConstants
{
    public const string AccountKeySettingName = "ApplicationServices:Persistence:AzureStorageAccount:AccountKey";
    public const string AccountNameSettingName = "ApplicationServices:Persistence:AzureStorageAccount:AccountName";
    public const string ConnectionString =
        "DefaultEndpointsProtocol=https;AccountName={0};AccountKey={1};EndpointSuffix=core.windows.net";
    public const string DefaultConnectionString = "UseDevelopmentStorage=true";
}