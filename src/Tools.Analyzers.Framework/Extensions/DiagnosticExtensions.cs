using Microsoft.CodeAnalysis;

namespace Tools.Analyzers.Framework.Extensions;

public static class DiagnosticExtensions
{
    public static DiagnosticDescriptor GetDescriptor(this string diagnosticId, DiagnosticSeverity severity,
        string category, string title, string description, string messageFormat)
    {
        return new DiagnosticDescriptor(diagnosticId, title.GetLocalizableString(),
            messageFormat.GetLocalizableString(), category, DiagnosticSeverity.Warning, true,
            description.GetLocalizableString());
    }
}