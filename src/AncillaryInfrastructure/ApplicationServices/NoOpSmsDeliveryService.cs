using Application.Common.Extensions;
using Application.Interfaces;
using Application.Persistence.Shared;
using Common;
using Common.Extensions;
using Task = System.Threading.Tasks.Task;

namespace AncillaryInfrastructure.ApplicationServices;

/// <summary>
///     Provides a <see cref="ISmsDeliveryService" /> that does nothing
/// </summary>
public class NoOpSmsDeliveryService : ISmsDeliveryService
{
    private readonly IRecorder _recorder;

    public NoOpSmsDeliveryService(IRecorder recorder)
    {
        _recorder = recorder;
    }

    public Task<Result<SmsDeliveryReceipt, Error>> SendAsync(ICallerContext caller, string body,
        string toPhoneNumber, CancellationToken cancellationToken = default)
    {
        _recorder.TraceDebug(caller.ToCall(),
            $"{nameof(NoOpSmsDeliveryService)} would have delivered SMS message for {{For}} with properties: {{Properties}}",
            toPhoneNumber, new
            {
                To = toPhoneNumber,
                Body = body
            }.ToJson()!);

        return Task.FromResult<Result<SmsDeliveryReceipt, Error>>(new SmsDeliveryReceipt
        {
            ReceiptId = $"receipt_{Guid.NewGuid():N}"
        });
    }
}