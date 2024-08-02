using Application.Persistence.Interfaces;
using Application.Persistence.Shared;
using Application.Persistence.Shared.ReadModels;
using Common;
using Infrastructure.Persistence.Common;
using Infrastructure.Persistence.Interfaces;

namespace Infrastructure.Persistence.Shared.ApplicationServices;

public class WebhookNotificationAuditRepository : IWebhookNotificationAuditRepository
{
    private readonly ISnapshottingStore<WebhookNotificationAudit> _audits;

    public WebhookNotificationAuditRepository(IRecorder recorder, IDataStore dataStore)
    {
        _audits = new SnapshottingStore<WebhookNotificationAudit>(recorder, dataStore);
    }

#if TESTINGONLY
    public async Task<Result<Error>> DestroyAllAsync(CancellationToken cancellationToken)
    {
        return await _audits.DestroyAllAsync(cancellationToken);
    }
#endif

    public async Task<Result<WebhookNotificationAudit, Error>> LoadAsync(string auditId,
        CancellationToken cancellationToken)
    {
        var retrieved = await _audits.GetAsync(auditId, true, false, cancellationToken);
        if (retrieved.IsFailure)
        {
            return retrieved.Error;
        }

        if (!retrieved.Value.HasValue)
        {
            return Error.EntityNotFound();
        }

        return retrieved.Value.Value;
    }

    public async Task<Result<WebhookNotificationAudit, Error>> SaveAsync(
        WebhookNotificationAudit audit, CancellationToken cancellationToken)
    {
        var upserted = await _audits.UpsertAsync(audit, false, cancellationToken);
        if (upserted.IsFailure)
        {
            return upserted.Error;
        }

        return upserted.Value;
    }
}