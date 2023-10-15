using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Tools.Analyzers.Core.Extensions;

public static class DiagnosticExtensions
{
    public static DiagnosticDescriptor GetDescriptor(this string diagnosticId, DiagnosticSeverity severity,
        string category, string title, string description, string messageFormat)
    {
        return new DiagnosticDescriptor(diagnosticId, title.GetLocalizableString(),
            messageFormat.GetLocalizableString(), category, DiagnosticSeverity.Warning, true,
            description.GetLocalizableString());
    }

    public static void ReportDiagnostic(this SyntaxNodeAnalysisContext context, DiagnosticDescriptor descriptor,
        IdentifierNameSyntax nameSyntax, params object?[]? messageArgs)
    {
        var identifier = nameSyntax.Identifier;
        var arguments = messageArgs is not null && messageArgs.Any()
            ? new object[] { identifier.Text }.Concat(messageArgs)
            : new object[] { identifier.Text };
        var diagnostic = Diagnostic.Create(descriptor, identifier.GetLocation(), arguments.ToArray());
        context.ReportDiagnostic(diagnostic);
    }
    
    public static void ReportDiagnostic(this SyntaxNodeAnalysisContext context, DiagnosticDescriptor descriptor,
        MethodDeclarationSyntax methodDeclarationSyntax, params object?[]? messageArgs)
    {
        var identifier = methodDeclarationSyntax.Identifier;
        var arguments = messageArgs is not null && messageArgs.Any()
            ? new object[] { identifier.Text }.Concat(messageArgs)
            : new object[] { identifier.Text };
        var diagnostic = Diagnostic.Create(descriptor, identifier.GetLocation(), arguments.ToArray());
        context.ReportDiagnostic(diagnostic);
    }

    public static void ReportDiagnostic(this SyntaxNodeAnalysisContext context, DiagnosticDescriptor descriptor,
        MemberDeclarationSyntax memberDeclarationSyntax, params object?[]? messageArgs)
    {
        var location = Location.None;
        var text = "Unknown";

        if (memberDeclarationSyntax is BaseTypeDeclarationSyntax baseType)
        {
            location = baseType.Identifier.GetLocation();
            text = baseType.Identifier.Text;
        }

        if (memberDeclarationSyntax is DelegateDeclarationSyntax delegateType)
        {
            location = delegateType.Identifier.GetLocation();
            text = delegateType.Identifier.Text;
        }

        var arguments = messageArgs is not null && messageArgs.Any()
            ? new object[] { text }.Concat(messageArgs)
            : new object[] { text };

        var diagnostic = Diagnostic.Create(descriptor, location, text, arguments.ToArray());
        context.ReportDiagnostic(diagnostic);
    }
}