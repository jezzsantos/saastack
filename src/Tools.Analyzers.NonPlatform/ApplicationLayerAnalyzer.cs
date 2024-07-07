using System.Collections.Immutable;
using Application.Interfaces.Resources;
using Application.Persistence.Interfaces;
using Common.Extensions;
using Domain.Interfaces.ValueObjects;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Tools.Analyzers.Common;
using Tools.Analyzers.Common.Extensions;
using Tools.Analyzers.NonPlatform.Extensions;

namespace Tools.Analyzers.NonPlatform;

/// <summary>
///     An analyzer to correct the implementation of application DTOs and ReadModels:
///     Application Resources:
///     SAASAPP010: Error: Resources must be public
///     SAASAPP011: Error: Resources must have a parameterless constructor
///     SAASAPP012: Error: Properties must have public getters and setters
///     SAASAPP013: Error: Properties must be nullable, not Optional{T} for interoperability
///     SAASAPP014: Error: Properties must have primitives, List{T}, Dictionary{string,T},
///     or any other Type in the 'Application.Resources.Shared' namespace, or be enums
///     ReadModels:
///     SAASAPP020: Error: ReadModels must be public
///     SAASAPP021: Error: ReadModels must have the EntityNameAttribute
///     SAASAPP022: Error: ReadModels must have a parameterless constructor
///     SAASAPP023: Error: Properties must have public getters and setters
///     SAASAPP024: Warning: Properties should be Optional{T} not nullable
///     SAASAPP025: Error: Properties must have return type of primitives, any ValueObject,
///     Optional{TPrimitive}, Optional{TValueObject}, List{TPrimitive}, Dictionary{string,TPrimitive}, or be enums
///     ApplicationServices
///     SAASAPP030: Error: Repositories should derive from IApplicationRepository
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class ApplicationLayerAnalyzer : DiagnosticAnalyzer
{
    private const string ApplicationRepositoryExpression = @"^I[\w\d]+Repository$";
    private const string ApplicationRepositoryNamespaceSubstring = "Application";
    internal static readonly SpecialType[] AllowableReadModelPrimitives =
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
    internal static readonly SpecialType[] AllowableResourcePrimitives = AllowableReadModelPrimitives;
    internal static readonly DiagnosticDescriptor Rule010 = "SAASAPP010".GetDescriptor(DiagnosticSeverity.Error,
        AnalyzerConstants.Categories.Application, nameof(Resources.Diagnostic_Title_ClassMustBePublic),
        nameof(Resources.Diagnostic_Description_ClassMustBePublic),
        nameof(Resources.Diagnostic_MessageFormat_ClassMustBePublic));
    internal static readonly DiagnosticDescriptor Rule011 = "SAASAPP011".GetDescriptor(DiagnosticSeverity.Error,
        AnalyzerConstants.Categories.Application,
        nameof(Resources.Diagnostic_Title_ClassMustHaveParameterlessConstructor),
        nameof(Resources.Diagnostic_Description_ClassMustHaveParameterlessConstructor),
        nameof(Resources.Diagnostic_MessageFormat_ClassMustHaveParameterlessConstructor));
    internal static readonly DiagnosticDescriptor Rule012 = "SAASAPP012".GetDescriptor(DiagnosticSeverity.Error,
        AnalyzerConstants.Categories.Application, nameof(Resources.Diagnostic_Title_PropertyMustBeGettableAndSettable),
        nameof(Resources.Diagnostic_Description_PropertyMustBeGettableAndSettable),
        nameof(Resources.Diagnostic_MessageFormat_PropertyMustBeGettableAndSettable));
    internal static readonly DiagnosticDescriptor Rule013 = "SAASAPP013".GetDescriptor(DiagnosticSeverity.Error,
        AnalyzerConstants.Categories.Application, nameof(Resources.Diagnostic_Title_PropertyMustBeNullableNotOptional),
        nameof(Resources.Diagnostic_Description_PropertyMustBeNullableNotOptional),
        nameof(Resources.Diagnostic_MessageFormat_PropertyMustBeNullableNotOptional));
    internal static readonly DiagnosticDescriptor Rule014 = "SAASAPP014".GetDescriptor(DiagnosticSeverity.Error,
        AnalyzerConstants.Categories.Application, nameof(Resources.SAASAPP014Title),
        nameof(Resources.SAASAPP014Description),
        nameof(Resources.SAASAPP014MessageFormat));
    internal static readonly DiagnosticDescriptor Rule020 = "SAASAPP020".GetDescriptor(DiagnosticSeverity.Error,
        AnalyzerConstants.Categories.Application, nameof(Resources.Diagnostic_Title_ClassMustBePublic),
        nameof(Resources.Diagnostic_Description_ClassMustBePublic),
        nameof(Resources.Diagnostic_MessageFormat_ClassMustBePublic));
    internal static readonly DiagnosticDescriptor Rule021 = "SAASAPP021".GetDescriptor(DiagnosticSeverity.Error,
        AnalyzerConstants.Categories.Application, nameof(Resources.Diagnostic_Title_MustDeclareEntityNameAttribute),
        nameof(Resources.Diagnostic_Description_MustDeclareEntityNameAttribute),
        nameof(Resources.Diagnostic_MessageFormat_MustDeclareEntityNameAttribute));
    internal static readonly DiagnosticDescriptor Rule022 = "SAASAPP022".GetDescriptor(DiagnosticSeverity.Error,
        AnalyzerConstants.Categories.Application,
        nameof(Resources.Diagnostic_Title_ClassMustHaveParameterlessConstructor),
        nameof(Resources.Diagnostic_Description_ClassMustHaveParameterlessConstructor),
        nameof(Resources.Diagnostic_MessageFormat_ClassMustHaveParameterlessConstructor));
    internal static readonly DiagnosticDescriptor Rule023 = "SAASAPP023".GetDescriptor(DiagnosticSeverity.Error,
        AnalyzerConstants.Categories.Application, nameof(Resources.Diagnostic_Title_PropertyMustBeGettableAndSettable),
        nameof(Resources.Diagnostic_Description_PropertyMustBeGettableAndSettable),
        nameof(Resources.Diagnostic_MessageFormat_PropertyMustBeGettableAndSettable));
    internal static readonly DiagnosticDescriptor Rule024 = "SAASAPP024".GetDescriptor(DiagnosticSeverity.Warning,
        AnalyzerConstants.Categories.Application, nameof(Resources.SAASAPP024Title),
        nameof(Resources.SAASAPP024Description),
        nameof(Resources.SAASAPP024MessageFormat));
    internal static readonly DiagnosticDescriptor Rule025 = "SAASAPP025".GetDescriptor(DiagnosticSeverity.Error,
        AnalyzerConstants.Categories.Application, nameof(Resources.SAASAPP025Title),
        nameof(Resources.SAASAPP025Description),
        nameof(Resources.SAASAPP025MessageFormat));
    internal static readonly DiagnosticDescriptor Rule030 = "SAASAPP030".GetDescriptor(DiagnosticSeverity.Error,
        AnalyzerConstants.Categories.Application, nameof(Resources.SAASAPP030Title),
        nameof(Resources.SAASAPP030Description),
        nameof(Resources.SAASAPP030MessageFormat));

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
        ImmutableArray.Create(
            Rule010, Rule011, Rule012, Rule013, Rule014,
            Rule020, Rule021, Rule022, Rule023, Rule024, Rule025,
            Rule030);

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterSyntaxNodeAction(AnalyzeResource, SyntaxKind.ClassDeclaration);
        context.RegisterSyntaxNodeAction(AnalyzeReadModel, SyntaxKind.ClassDeclaration);
        context.RegisterSyntaxNodeAction(AnalyzeRepository, SyntaxKind.InterfaceDeclaration);
    }

    private static void AnalyzeResource(SyntaxNodeAnalysisContext context)
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

        if (classDeclarationSyntax.IsNotType<IIdentifiableResource>(context))
        {
            return;
        }

        if (!classDeclarationSyntax.IsPublic())
        {
            context.ReportDiagnostic(Rule010, classDeclarationSyntax);
        }

        if (!classDeclarationSyntax.HasParameterlessConstructor())
        {
            context.ReportDiagnostic(Rule011, classDeclarationSyntax);
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
                    context.ReportDiagnostic(Rule012, property);
                }

                if (!property.IsNullableType(context) && property.IsOptionalType(context))
                {
                    context.ReportDiagnostic(Rule013, property);
                }

                var allowedReturnTypes = context.GetAllowableApplicationResourcePropertyReturnTypes();
                if (context.HasIncorrectReturnType(property, allowedReturnTypes)
                    && !property.IsReturnTypeInNamespace(context, AnalyzerConstants.ResourceTypesNamespace))
                {
                    var acceptableReturnTypes =
                        allowedReturnTypes
                            .Where(allowable =>
                                !allowable.ToDisplayString().StartsWith("System.Collections")
                                && !allowable.ToDisplayString().EndsWith("?"))
                            .Select(allowable => allowable.ToDisplayString()).Join(" or ");
                    context.ReportDiagnostic(Rule014, property, acceptableReturnTypes,
                        AnalyzerConstants.ResourceTypesNamespace);
                }
            }
        }
    }

    private static void AnalyzeReadModel(SyntaxNodeAnalysisContext context)
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

        if (classDeclarationSyntax.IsNotType<IReadModelEntity>(context))
        {
            return;
        }

        if (!classDeclarationSyntax.IsPublic())
        {
            context.ReportDiagnostic(Rule020, classDeclarationSyntax);
        }

        if (!classDeclarationSyntax.HasEntityNameAttribute(context))
        {
            context.ReportDiagnostic(Rule021, classDeclarationSyntax);
        }

        if (!classDeclarationSyntax.HasParameterlessConstructor())
        {
            context.ReportDiagnostic(Rule022, classDeclarationSyntax);
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
                    context.ReportDiagnostic(Rule023, property);
                }

                if (property.IsNullableType(context))
                {
                    context.ReportDiagnostic(Rule024, property);
                }

                var allowedReturnTypes = context.GetAllowableApplicationReadModelPropertyReturnTypes();
                if (context.HasIncorrectReturnType(property, allowedReturnTypes)
                    && !property.IsReadModelEnumType(context)
                    && !property.IsReadModelValueObjectType(context))
                {
                    var acceptableReturnTypes =
                        allowedReturnTypes
                            .Where(allowable =>
                                !allowable.ToDisplayString().StartsWith("System.Collections")
                                && !allowable.ToDisplayString().StartsWith("Common.Optional"))
                            .Select(allowable => allowable.ToDisplayString()).Join(" or ");
                    context.ReportDiagnostic(Rule025, property, acceptableReturnTypes);
                }
            }
        }
    }

    private static void AnalyzeRepository(SyntaxNodeAnalysisContext context)
    {
        var methodSyntax = context.Node;
        if (methodSyntax is not InterfaceDeclarationSyntax interfaceDeclarationSyntax)
        {
            return;
        }

        if (context.IsExcludedInNamespace(interfaceDeclarationSyntax, AnalyzerConstants.PlatformNamespaces))
        {
            return;
        }

        var name = interfaceDeclarationSyntax.Identifier.Text;
        if (!name.IsMatchWith(ApplicationRepositoryExpression))
        {
            return;
        }

        var containingNamespace = interfaceDeclarationSyntax.GetContainingNamespace(context);
        if (!containingNamespace.EndsWith(ApplicationRepositoryNamespaceSubstring))
        {
            return;
        }

        if (interfaceDeclarationSyntax.IsNotType<IApplicationRepository>(context))
        {
            context.ReportDiagnostic(Rule030, interfaceDeclarationSyntax);
        }
    }
}

internal static class ApplicationLayerExtensions
{
    private static INamedTypeSymbol[]? _allowableApplicationReadModelPropertyReturnTypes;
    private static INamedTypeSymbol[]? _allowableApplicationResourcePropertyReturnTypes;

    public static INamedTypeSymbol[] GetAllowableApplicationReadModelPropertyReturnTypes(
        this SyntaxNodeAnalysisContext context)
    {
        // Cache this
        if (_allowableApplicationReadModelPropertyReturnTypes is null)
        {
            var stringType = context.Compilation.GetSpecialType(SpecialType.System_String);
            var primitiveTypes = ApplicationLayerAnalyzer.AllowableReadModelPrimitives
                .Select(context.Compilation.GetSpecialType).ToArray();
            var primitiveNullableTypes = primitiveTypes
                .Select(primitive =>
                    context.Compilation.GetSpecialType(SpecialType.System_Nullable_T).Construct(primitive))
                .ToArray();

            var optionalOfType =
                context.Compilation.GetTypeByMetadataName(typeof(global::Common.Optional<>).FullName!)!;
            var optionalTypes = primitiveTypes
                .Select(primitive => optionalOfType.Construct(primitive)).ToArray();
            var optionalNullableTypes = primitiveNullableTypes
                .Select(primitive => optionalOfType.Construct(primitive)).ToArray();

            var listOfType = context.Compilation.GetTypeByMetadataName(typeof(List<>).FullName!)!;
            var listTypes = primitiveTypes
                .Select(primitive => listOfType.Construct(primitive)).ToArray();
            var optionalListTypes = primitiveTypes
                .Select(primitive => optionalOfType.Construct(listOfType.Construct(primitive)))
                .ToArray();

            var dictionaryOfType = context.Compilation.GetTypeByMetadataName(typeof(Dictionary<,>).FullName!)!;
            var stringDictionaryTypes = primitiveTypes
                .Select(primitive => dictionaryOfType.Construct(stringType, primitive))
                .ToArray();
            var optionalStringDictionaryTypes = primitiveTypes
                .Select(primitive => optionalOfType.Construct(dictionaryOfType.Construct(stringType, primitive)))
                .ToArray();

            _allowableApplicationReadModelPropertyReturnTypes = primitiveTypes
                .Concat(optionalTypes)
                .Concat(optionalNullableTypes)
                .Concat(listTypes)
                .Concat(optionalListTypes)
                .Concat(stringDictionaryTypes)
                .Concat(optionalStringDictionaryTypes)
                .ToArray();
        }

        return _allowableApplicationReadModelPropertyReturnTypes;
    }

    public static INamedTypeSymbol[] GetAllowableApplicationResourcePropertyReturnTypes(
        this SyntaxNodeAnalysisContext context)
    {
        // Cache this
        if (_allowableApplicationResourcePropertyReturnTypes is null)
        {
            var streamType = context.Compilation.GetTypeByMetadataName(typeof(Stream).FullName!)!;
            var stringType = context.Compilation.GetSpecialType(SpecialType.System_String);
            var primitiveTypes = ApplicationLayerAnalyzer.AllowableResourcePrimitives
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

            _allowableApplicationResourcePropertyReturnTypes = primitiveTypes
                .Concat(new[] { streamType })
                .Concat(nullableTypes)
                .Concat(listTypes)
                .Concat(stringDictionaryTypes)
                .ToArray();
        }

        return _allowableApplicationResourcePropertyReturnTypes;
    }

    public static bool IsReadModelEnumType(this PropertyDeclarationSyntax propertyDeclarationSyntax,
        SyntaxNodeAnalysisContext context)
    {
        var propertySymbol = context.SemanticModel.GetDeclaredSymbol(propertyDeclarationSyntax);
        if (propertySymbol is null)
        {
            return false;
        }

        var getter = propertySymbol.GetMethod;
        if (getter is null)
        {
            return false;
        }

        var returnType = getter.ReturnType.WithoutOptional(context);

        var listType = context.Compilation.GetTypeByMetadataName(typeof(List<>).FullName!)!;
        if (returnType.OriginalDefinition.IsOfType(listType))
        {
            returnType = ((INamedTypeSymbol)returnType).TypeArguments[0];
        }

        var dictionaryType = context.Compilation.GetTypeByMetadataName(typeof(Dictionary<,>).FullName!)!;
        if (returnType.OriginalDefinition.IsOfType(dictionaryType))
        {
            returnType = ((INamedTypeSymbol)returnType).TypeArguments[1];
        }

        return returnType.IsEnum();
    }

    public static bool IsReadModelValueObjectType(this PropertyDeclarationSyntax propertyDeclarationSyntax,
        SyntaxNodeAnalysisContext context)
    {
        var propertySymbol = context.SemanticModel.GetDeclaredSymbol(propertyDeclarationSyntax);
        if (propertySymbol is null)
        {
            return false;
        }

        var getter = propertySymbol.GetMethod;
        if (getter is null)
        {
            return false;
        }

        var returnType = getter.ReturnType.WithoutOptional(context);

        var listType = context.Compilation.GetTypeByMetadataName(typeof(List<>).FullName!)!;
        if (returnType.OriginalDefinition.IsOfType(listType))
        {
            returnType = ((INamedTypeSymbol)returnType).TypeArguments[0];
        }

        var dictionaryType = context.Compilation.GetTypeByMetadataName(typeof(Dictionary<,>).FullName!)!;
        if (returnType.OriginalDefinition.IsOfType(dictionaryType))
        {
            returnType = ((INamedTypeSymbol)returnType).TypeArguments[1];
        }

        var valueObjectType = context.Compilation.GetTypeByMetadataName(typeof(IValueObject).FullName!)!;
        return returnType.AllInterfaces.Any(@interface => @interface.IsOfType(valueObjectType));
    }

    public static bool IsReturnTypeInNamespace(this PropertyDeclarationSyntax propertyDeclarationSyntax,
        SyntaxNodeAnalysisContext context, string containedNamespace)
    {
        var propertySymbol = context.SemanticModel.GetDeclaredSymbol(propertyDeclarationSyntax);
        if (propertySymbol is null)
        {
            return false;
        }

        var getter = propertySymbol.GetMethod;
        if (getter is null)
        {
            return false;
        }

        var returnType = getter.ReturnType.WithoutNullable(context);

        var listType = context.Compilation.GetTypeByMetadataName(typeof(List<>).FullName!)!;
        if (returnType.OriginalDefinition.IsOfType(listType))
        {
            returnType = ((INamedTypeSymbol)returnType).TypeArguments[0].WithoutNullable(context);
        }

        var dictionaryType = context.Compilation.GetTypeByMetadataName(typeof(Dictionary<,>).FullName!)!;
        if (returnType.OriginalDefinition.IsOfType(dictionaryType))
        {
            var stringType = context.Compilation.GetSpecialType(SpecialType.System_String);
            var firstArgument = ((INamedTypeSymbol)returnType).TypeArguments[0];
            if (!firstArgument.IsOfType(stringType))
            {
                return false;
            }

            returnType = ((INamedTypeSymbol)returnType).TypeArguments[1].WithoutNullable(context);
        }

        if (returnType.ContainingNamespace.ToDisplayString().StartsWith(containedNamespace))
        {
            return true;
        }

        return false;
    }
}