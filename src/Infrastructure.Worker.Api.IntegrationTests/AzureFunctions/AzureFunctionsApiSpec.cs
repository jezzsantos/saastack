using JetBrains.Annotations;
using Xunit;

namespace Infrastructure.Worker.Api.IntegrationTests.AzureFunctions;

[UsedImplicitly]
public class AzureFunctionsApiSpec
{
    [Trait("Category", "Integration.External")]
    [Collection("AzureFunctions")]
    public class DeliverUsageSpec : DeliverUsageSpecBase<AzureFunctionHostSetup>
    {
        public DeliverUsageSpec(AzureFunctionHostSetup setup) : base(setup)
        {
        }
    }

    [Trait("Category", "Integration.External")]
    [Collection("AzureFunctions")]
    public class DeliverAuditSpec : DeliverAuditSpecBase<AzureFunctionHostSetup>
    {
        public DeliverAuditSpec(AzureFunctionHostSetup setup) : base(setup)
        {
        }
    }

    [Trait("Category", "Integration.External")]
    [Collection("AzureFunctions")]
    public class SendEmailSpec : SendEmailSpecBase<AzureFunctionHostSetup>
    {
        public SendEmailSpec(AzureFunctionHostSetup setup) : base(setup)
        {
        }
    }

    [Trait("Category", "Integration.External")]
    [Collection("AzureFunctions")]
    public class DeliverProvisioningSpec : DeliverProvisioningSpecBase<AzureFunctionHostSetup>
    {
        public DeliverProvisioningSpec(AzureFunctionHostSetup setup) : base(setup)
        {
        }
    }

    [Trait("Category", "Integration.External")]
    [Collection("AzureFunctions")]
    public class DeliverDomainEventApiHost1Spec : DeliverDomainEventSpecBase<AzureFunctionHostSetup>
    {
        public DeliverDomainEventApiHost1Spec(AzureFunctionHostSetup setup) : base(setup)
        {
        }
    }
}