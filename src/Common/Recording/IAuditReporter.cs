namespace Common.Recording;

/// <summary>
///     Records permanent audit events in the system
/// </summary>
public interface IAuditReporter
{
    void Audit(ICallContext? context, string againstId, string auditCode, string messageTemplate,
        params object[] templateArgs);
}