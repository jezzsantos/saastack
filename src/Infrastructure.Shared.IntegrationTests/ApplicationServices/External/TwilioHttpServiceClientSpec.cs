using Common.Configuration;
using Common.Recording;
using FluentAssertions;
using Infrastructure.Shared.ApplicationServices.External;
using IntegrationTesting.WebApi.Common;
using Microsoft.Extensions.DependencyInjection;
using UnitTesting.Common;
using Xunit;

namespace Infrastructure.Shared.IntegrationTests.ApplicationServices.External;

[Trait("Category", "Integration.External")]
[Collection("EXTERNAL")]
public class TwilioHttpServiceClientSpec : ExternalApiSpec
{
    private readonly string _recipientPhoneNumber;
    private readonly TwilioHttpServiceClient _serviceClient;

    public TwilioHttpServiceClientSpec(ExternalApiSetup setup) : base(setup, OverrideDependencies)
    {
        var settings = setup.GetRequiredService<IConfigurationSettings>();
        _serviceClient = new TwilioHttpServiceClient(NoOpRecorder.Instance, settings, new TestHttpClientFactory());
        _recipientPhoneNumber = settings.GetString("ApplicationServices:Twilio:TestingOnly:RecipientPhoneNumber");
    }

    [Fact]
    public async Task WhenSendAsync_ThenSends()
    {
        var result = await _serviceClient.SendAsync(new TestCaller(), "atestmessage", _recipientPhoneNumber,
            CancellationToken.None);

        result.Should().BeSuccess();
        result.Value.ReceiptId.Should().NotBeEmpty();
    }

    private static void OverrideDependencies(IServiceCollection services)
    {
        //Do nothing
    }
}