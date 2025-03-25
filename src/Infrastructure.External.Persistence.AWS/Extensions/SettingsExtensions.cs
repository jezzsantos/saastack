using Amazon;
using Amazon.Runtime;
using Common.Configuration;
using Common.Extensions;

namespace Infrastructure.External.Persistence.AWS.Extensions;

public static class SettingsExtensions
{
    public static (AWSCredentials Credentials, RegionEndpoint? RegionEndPoint) GetConnection(
        this IConfigurationSettings settings)
    {
        var accessKey = settings.GetString(AWSConstants.AccessKeySettingName);
        var secretKey = settings.GetString(AWSConstants.SecretKeySettingName);

        var credentials = new BasicAWSCredentials(accessKey, secretKey);

        //Note: It is this setting that makes the difference between using LocalStack and using the real AWS
        var region = settings.GetString(AWSConstants.RegionSettingName);
        if (region.HasNoValue())
        {
            return (new AnonymousAWSCredentials(), null);
        }

        var regionEndpoint = RegionEndpoint.GetBySystemName(region);
        return (credentials, regionEndpoint);
    }
}