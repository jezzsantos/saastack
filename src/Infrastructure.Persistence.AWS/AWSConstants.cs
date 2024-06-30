namespace Infrastructure.Persistence.AWS;

public static class AWSConstants
{
    public const string AccessKeySettingName = "ApplicationServices:Persistence:AWS:AccessKey";
    public const string FifoGroupName = "ordered";
    public const string LambdaArnValidationExpression = @"^arn\:aws\:lambda\:[a-zA-Z0-9_\-\:]{20,256}$";
    public const string LocalStackServiceUrl = "http://localhost:4566";
    public const string QueueArnValidationExpression = @"^arn\:aws\:sqs\:[a-zA-Z0-9_\-\:\.]{20,256}$";
    public const string RegionSettingName = "ApplicationServices:Persistence:AWS:Region";
    public const string SecretKeySettingName = "ApplicationServices:Persistence:AWS:SecretKey";
    public const string SNSTopicNameValidationExpression = @"^[a-zA-Z0-9_\-\.]{3,256}$";
    public const string SQSQueueNameValidationExpression = @"^[a-zA-Z0-9_\-]{3,80}$";
}