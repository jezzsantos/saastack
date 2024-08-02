using Application.Persistence.Interfaces;
using Application.Persistence.Shared.ReadModels;
using Common;

namespace Application.Persistence.Shared;

public interface IWebhookNotificationAuditRepository : IApplicationRepository
{
    Task<Result<WebhookNotificationAudit, Error>> LoadAsync(string auditId, CancellationToken cancellationToken);

    Task<Result<WebhookNotificationAudit, Error>> SaveAsync(WebhookNotificationAudit audit,
        CancellationToken cancellationToken);
}