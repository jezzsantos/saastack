using JetBrains.Annotations;
using Xunit;

namespace Infrastructure.Worker.Api.IntegrationTests.AWSLambdas;

[UsedImplicitly]
public class AWSLambdasApiSpec
{
    [Trait("Category", "Integration.External")]
    [Collection("AWSLambdas")]
    public class DeliverUsageSpec : DeliverUsageSpecBase<AWSLambdaHostSetup>
    {
        public DeliverUsageSpec(AWSLambdaHostSetup setup) : base(setup)
        {
        }
    }

    [Trait("Category", "Integration.External")]
    [Collection("AWSLambdas")]
    public class DeliverAuditSpec : DeliverAuditSpecBase<AWSLambdaHostSetup>
    {
        public DeliverAuditSpec(AWSLambdaHostSetup setup) : base(setup)
        {
        }
    }

    [Trait("Category", "Integration.External")]
    [Collection("AWSLambdas")]
    public class SendEmailSpec : SendEmailSpecBase<AWSLambdaHostSetup>
    {
        public SendEmailSpec(AWSLambdaHostSetup setup) : base(setup)
        {
        }
    }

    [Trait("Category", "Integration.External")]
    [Collection("AWSLambdas")]
    public class DeliverProvisioningSpec : DeliverProvisioningSpecBase<AWSLambdaHostSetup>
    {
        public DeliverProvisioningSpec(AWSLambdaHostSetup setup) : base(setup)
        {
        }
    }

    [Trait("Category", "Integration.External")]
    [Collection("AWSLambdas")]
    public class DeliverDomainEventApiHost1Spec : DeliverDomainEventSpecBase<AWSLambdaHostSetup>
    {
        public DeliverDomainEventApiHost1Spec(AWSLambdaHostSetup setup) : base(setup)
        {
        }
    }
}