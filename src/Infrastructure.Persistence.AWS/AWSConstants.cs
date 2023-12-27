namespace Infrastructure.Persistence.AWS;

public static class AWSConstants
{
    public const string LocalStackServiceUrl = "http://localhost.localstack.cloud:4566";
    public const string SqsQueueNameValidationExpression = @"^[a-zA-Z0-9\-_]{3,80}$";
    public const string AccessKeySettingName = "ApplicationServices:Persistence:AWS:AccessKey";
    public const string SecretKeySettingName = "ApplicationServices:Persistence:AWS:SecretKey";
    public const string RegionSettingName = "ApplicationServices:Persistence:AWS:Region";
}