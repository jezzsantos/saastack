using System.Diagnostics.CodeAnalysis;
using Common;
using Common.Recording;
using JetBrains.Annotations;

namespace Infrastructure.Common.Recording;

/// <summary>
///     An <see cref="IAuditReporter" /> that does nothing
/// </summary>
[ExcludeFromCodeCoverage]
public class NoOpAuditReporter : IAuditReporter
{
    public void Audit(ICallContext? context, string againstId, string auditCode,
        [StructuredMessageTemplate] string messageTemplate, params object[] templateArgs)
    {
    }
}