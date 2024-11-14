using System.Text.Json;
using Application.Common.Extensions;
using Application.Interfaces;
using Application.Interfaces.Services;
using Application.Persistence.Shared.ReadModels;
using Common;
using Common.Extensions;

namespace AncillaryApplication;

partial class AncillaryApplication
{
#if TESTINGONLY
    public async Task<Result<Error>> DrainAllProvisioningsAsync(ICallerContext caller,
        CancellationToken cancellationToken)
    {
        await DrainAllOnQueueAsync(_provisioningMessageQueue,
            message => NotifyProvisioningInternalAsync(caller, message, cancellationToken), cancellationToken);

        _recorder.TraceInformation(caller.ToCall(), "Drained all provisioning messages");

        return Result.Ok;
    }
#endif

    public async Task<Result<bool, Error>> NotifyProvisioningAsync(ICallerContext caller, string messageAsJson,
        CancellationToken cancellationToken)
    {
        var rehydrated = RehydrateMessage<ProvisioningMessage>(messageAsJson);
        if (rehydrated.IsFailure)
        {
            return rehydrated.Error;
        }

        var delivered = await NotifyProvisioningInternalAsync(caller, rehydrated.Value, cancellationToken);
        if (delivered.IsFailure)
        {
            return delivered.Error;
        }

        _recorder.TraceInformation(caller.ToCall(), "Delivered provisioning message: {Message}", messageAsJson);
        return true;
    }

    private async Task<Result<bool, Error>> NotifyProvisioningInternalAsync(ICallerContext caller,
        ProvisioningMessage message, CancellationToken cancellationToken)
    {
        if (message.TenantId.IsInvalidParameter(x => x.HasValue(), nameof(ProvisioningMessage.TenantId), out _))
        {
            return Error.RuleViolation(Resources.AncillaryApplication_Provisioning_MissingTenantId);
        }

        var tenantSettings = new TenantSettings(message.Settings.ToDictionary(pair => pair.Key,
            pair =>
            {
                var value = pair.Value.Value;
                if (value is JsonElement jsonElement)
                {
                    value = jsonElement.ValueKind switch
                    {
                        JsonValueKind.String => jsonElement.GetString(),
                        JsonValueKind.Number => jsonElement.GetDouble(),
                        JsonValueKind.True => true,
                        JsonValueKind.False => false,
                        _ => null
                    };
                }

                return new TenantSetting(value);
            }));
        var notified =
            await _provisioningNotificationService.NotifyAsync(caller, message.TenantId!, tenantSettings,
                cancellationToken);
        if (notified.IsFailure)
        {
            return notified.Error;
        }

        _recorder.TraceInformation(caller.ToCall(), "Notified provisioning for {Tenant}", message.TenantId!);

        return true;
    }
}