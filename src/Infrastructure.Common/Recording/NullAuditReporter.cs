using Common;
using Common.Recording;

namespace Infrastructure.Common.Recording;

/// <summary>
///     An <see cref="IAuditReporter" /> that does nothing
/// </summary>
public class NullAuditReporter : IAuditReporter
{
    public void Audit(ICallContext? context, string againstId, string auditCode, string messageTemplate,
        params object[] templateArgs)
    {
    }
}