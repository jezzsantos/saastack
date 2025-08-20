using System.Collections.Immutable;
using Common.Extensions;
using Infrastructure.Eventing.Interfaces.Notifications;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Tools.Analyzers.Common;
using Tools.Analyzers.Common.Extensions;
using Tools.Analyzers.NonFramework.Extensions;

namespace Tools.Analyzers.NonFramework;

/// <summary>
///     An analyzer to correct the implementation of eventing:
///     IntegrationEvents:
///     SAASEVT010: Error: IntegrationEvents must be public
///     SAASEVT011: Warning: IntegrationEvents should be sealed
///     SAASEVT012: Error: IntegrationEvents must have a parameterless constructor
///     SAASEVT013: Error: Properties must have public getters and setters
///     SAASEVT014: Error: Properties must be required or nullable or initialized
///     SAASEVT015: Error: Properties must be nullable, not Optional{T} for interoperability
///     SAASEVT016: Error: Properties must have return type of primitives, List{TPrimitive}, Dictionary{string,TPrimitive},
///     or be other DTOs
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class EventingAnalyzer : DiagnosticAnalyzer
{
    internal static readonly SpecialType[] AllowableIntegrationEventPrimitives =
    [
        SpecialType.System_Boolean,
        SpecialType.System_String,
        SpecialType.System_UInt64,
        SpecialType.System_Int32,
        SpecialType.System_Int64,
        SpecialType.System_Double,
        SpecialType.System_Decimal,
        SpecialType.System_DateTime,
        SpecialType.System_Byte
    ];
    internal static readonly DiagnosticDescriptor Rule010 = "SAASEVT010".GetDescriptor(DiagnosticSeverity.Error,
        AnalyzerConstants.Categories.Eventing, nameof(Resources.Diagnostic_Title_ClassMustBePublic),
        nameof(Resources.Diagnostic_Description_ClassMustBePublic),
        nameof(Resources.Diagnostic_MessageFormat_ClassMustBePublic));
    internal static readonly DiagnosticDescriptor Rule011 = "SAASEVT011".GetDescriptor(DiagnosticSeverity.Warning,
        AnalyzerConstants.Categories.Eventing, nameof(Resources.Diagnostic_Title_ClassShouldBeSealed),
        nameof(Resources.Diagnostic_Description_ClassShouldBeSealed),
        nameof(Resources.Diagnostic_MessageFormat_ClassShouldBeSealed));
    internal static readonly DiagnosticDescriptor Rule012 = "SAASEVT012".GetDescriptor(DiagnosticSeverity.Error,
        AnalyzerConstants.Categories.Eventing, nameof(Resources.Diagnostic_Title_ClassMustHaveParameterlessConstructor),
        nameof(Resources.Diagnostic_Description_ClassMustHaveParameterlessConstructor),
        nameof(Resources.Diagnostic_MessageFormat_ClassMustHaveParameterlessConstructor));
    internal static readonly DiagnosticDescriptor Rule013 = "SAASEVT013".GetDescriptor(DiagnosticSeverity.Error,
        AnalyzerConstants.Categories.Eventing, nameof(Resources.Diagnostic_Title_PropertyMustBeGettableAndSettable),
        nameof(Resources.Diagnostic_Description_PropertyMustBeGettableAndSettable),
        nameof(Resources.Diagnostic_MessageFormat_PropertyMustBeGettableAndSettable));
    internal static readonly DiagnosticDescriptor Rule014 = "SAASEVT014".GetDescriptor(DiagnosticSeverity.Error,
        AnalyzerConstants.Categories.Eventing, nameof(Resources.SAASEVT014Title),
        nameof(Resources.SAASEVT014Description), nameof(Resources.SAASEVT014MessageFormat));
    internal static readonly DiagnosticDescriptor Rule015 = "SAASEVT015".GetDescriptor(DiagnosticSeverity.Error,
        AnalyzerConstants.Categories.Eventing, nameof(Resources.Diagnostic_Title_PropertyMustBeNullableNotOptional),
        nameof(Resources.Diagnostic_Description_PropertyMustBeNullableNotOptional),
        nameof(Resources.Diagnostic_MessageFormat_PropertyMustBeNullableNotOptional));
    internal static readonly DiagnosticDescriptor Rule016 = "SAASEVT016".GetDescriptor(DiagnosticSeverity.Error,
        AnalyzerConstants.Categories.Eventing, nameof(Resources.SAASEVT016Title),
        nameof(Resources.SAASEVT016Description), nameof(Resources.SAASEVT016MessageFormat));

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
        ImmutableArray.Create(
            Rule010, Rule011, Rule012, Rule013, Rule014, Rule015, Rule016);

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterSyntaxNodeAction(AnalyzeIntegrationEvent, SyntaxKind.ClassDeclaration);
    }

    private static void AnalyzeIntegrationEvent(SyntaxNodeAnalysisContext context)
    {
        var methodSyntax = context.Node;
        if (methodSyntax is not ClassDeclarationSyntax classDeclarationSyntax)
        {
            return;
        }

        if (context.IsExcludedInNamespace(classDeclarationSyntax, AnalyzerConstants.PlatformNamespaces))
        {
            return;
        }

        if (classDeclarationSyntax.IsNotType<IIntegrationEvent>(context))
        {
            return;
        }

        if (!classDeclarationSyntax.IsPublic())
        {
            context.ReportDiagnostic(Rule010, classDeclarationSyntax);
        }

        if (!classDeclarationSyntax.IsSealed())
        {
            context.ReportDiagnostic(Rule011, classDeclarationSyntax);
        }

        if (!classDeclarationSyntax.HasParameterlessConstructor())
        {
            context.ReportDiagnostic(Rule012, classDeclarationSyntax);
        }

        var allProperties = classDeclarationSyntax.Members.Where(member => member is PropertyDeclarationSyntax)
            .Cast<PropertyDeclarationSyntax>()
            .ToList();
        if (allProperties.HasAny())
        {
            foreach (var property in allProperties)
            {
                if (!property.HasPublicGetterAndSetter())
                {
                    context.ReportDiagnostic(Rule013, property);
                }

                if (!property.IsRequired()
                    && !property.IsInitialized())
                {
                    if (!property.IsNullableType(context))
                    {
                        context.ReportDiagnostic(Rule014, property);
                    }
                }

                if (!property.IsNullableType(context) && property.IsOptionalType(context))
                {
                    context.ReportDiagnostic(Rule015, property);
                }

                var allowedReturnTypes = context.GetAllowableIntegrationEventPropertyReturnTypes();
                if (context.HasIncorrectReturnType(property, allowedReturnTypes)
                    && !property.IsDtoOrNullableDto(context, allowedReturnTypes.ToList()))
                {
                    var acceptableReturnTypes =
                        allowedReturnTypes
                            .Where(allowable =>
                                !allowable.ToDisplayString().StartsWith("System.Collections")
                                && !allowable.ToDisplayString().EndsWith("?"))
                            .Select(allowable => allowable.ToDisplayString()).Join(" or ");
                    context.ReportDiagnostic(Rule016, property, acceptableReturnTypes);
                }
            }
        }
    }
}

internal static class EventingExtensions
{
    private static INamedTypeSymbol[]? _allowableIntegrationEventPropertyReturnTypes;

    public static INamedTypeSymbol[] GetAllowableIntegrationEventPropertyReturnTypes(
        this SyntaxNodeAnalysisContext context)
    {
        // Cache this
        if (_allowableIntegrationEventPropertyReturnTypes is null)
        {
            var stringType = context.Compilation.GetSpecialType(SpecialType.System_String);
            var primitiveTypes = EventingAnalyzer.AllowableIntegrationEventPrimitives
                .Select(context.Compilation.GetSpecialType).ToArray();

            var nullableOfType = context.Compilation.GetTypeByMetadataName(typeof(Nullable<>).FullName!)!;
            var nullableTypes = primitiveTypes
                .Select(primitive => nullableOfType.Construct(primitive)).ToArray();

            var listOfType = context.Compilation.GetTypeByMetadataName(typeof(List<>).FullName!)!;
            var listTypes = primitiveTypes
                .Select(primitive => listOfType.Construct(primitive)).ToArray();

            var dictionaryOfType = context.Compilation.GetTypeByMetadataName(typeof(Dictionary<,>).FullName!)!;
            var stringDictionaryTypes = primitiveTypes
                .Select(primitive => dictionaryOfType.Construct(stringType, primitive))
                .ToArray();

            _allowableIntegrationEventPropertyReturnTypes = primitiveTypes
                .Concat(nullableTypes)
                .Concat(listTypes)
                .Concat(stringDictionaryTypes).ToArray();
        }

        return _allowableIntegrationEventPropertyReturnTypes;
    }
}