using Application.Services.Shared;
using Common;
using Common.Configuration;
using Domain.Services.Shared;

namespace Infrastructure.Shared.ApplicationServices.External;

/// <summary>
///     Provides a <see cref="IBillingProvider" /> for integrating with Chargebee Billing.
/// </summary>
public sealed class ChargebeeBillingProvider : IBillingProvider
{
    public ChargebeeBillingProvider(IRecorder recorder, IConfigurationSettings settings)
    {
        GatewayService = new ChargebeeHttpServiceClient(recorder, settings);
        StateInterpreter = new ChargebeeStateInterpreter(settings);
    }

    public IBillingGatewayService GatewayService { get; }

    public string ProviderName => StateInterpreter.ProviderName;

    public IBillingStateInterpreter StateInterpreter { get; }
}