using Amazon;
using Amazon.Runtime;
using Common.Configuration;
using Common.Extensions;

namespace Infrastructure.Persistence.AWS.Extensions;

public static class SettingsExtensions
{
    public static (BasicAWSCredentials Credentials, RegionEndpoint? RegionEndPoint) GetConnection(
        this ISettings settings)
    {
        var accessKey = settings.GetString(AWSConstants.AccessKeySettingName);
        var secretKey = settings.GetString(AWSConstants.SecretKeySettingName);

        var credentials = new BasicAWSCredentials(accessKey, secretKey);

        if (accessKey.HasNoValue()
            || secretKey.HasNoValue())
        {
            return (credentials, null);
        }

        var region = settings.GetString(AWSConstants.RegionSettingName);
        var regionEndpoint = RegionEndpoint.GetBySystemName(region);

        return (credentials, regionEndpoint);
    }
}