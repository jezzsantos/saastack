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
///     An analyzer to the correct implementation of application DTOs and ReadModels:
///     Application Resources:
///     SAS070: Error: Resources must be public
///     SAS071: Error: Resources must have a parameterless constructor
///     SAS072: Error: Properties must have public getters and setters
///     SAS073: Error: Properties must be nullable, not Optional{T} for interoperability
///     SAS074: Error: Properties must have primitives, List{T}, Dictionary{string,T},
///     or any other Type in the 'Application.Resources.Shared' namespace, or be enums
///     ReadModels:
///     SAS080: Error: ReadModels must be public
///     SAS081: Error: ReadModels must have the EntityNameAttribute
///     SAS082: Error: ReadModels must have a parameterless constructor
///     SAS083: Error: Properties must have public getters and setters
///     SAS084: Warning: Properties should be Optional{T} not nullable
///     SAS085: Error: Properties must have return type of primitives, any ValueObject,
///     Optional{TPrimitive}, Optional{TValueObject}, List{TPrimitive}, Dictionary{string,TPrimitive}, or be enums
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class ApplicationLayerAnalyzer : DiagnosticAnalyzer
{
    internal static readonly SpecialType[] AllowableReadModelPrimitives =
    {
        SpecialType.System_Boolean,
        SpecialType.System_String,
        SpecialType.System_UInt64,
        SpecialType.System_Int32,
        SpecialType.System_Int64,
        SpecialType.System_Double,
        SpecialType.System_Decimal,
        SpecialType.System_DateTime,
        SpecialType.System_Byte
    };
    internal static readonly SpecialType[] AllowableResourcePrimitives = AllowableReadModelPrimitives;
    internal static readonly DiagnosticDescriptor Sas070 = "SAS070".GetDescriptor(DiagnosticSeverity.Error,
        AnalyzerConstants.Categories.Application, nameof(Resources.Diagnostic_Title_ClassMustBePublic),
        nameof(Resources.Diagnostic_Description_ClassMustBePublic),
        nameof(Resources.Diagnostic_MessageFormat_ClassMustBePublic));
    internal static readonly DiagnosticDescriptor Sas071 = "SAS071".GetDescriptor(DiagnosticSeverity.Error,
        AnalyzerConstants.Categories.Application,
        nameof(Resources.Diagnostic_Title_ClassMustHaveParameterlessConstructor),
        nameof(Resources.Diagnostic_Description_ClassMustHaveParameterlessConstructor),
        nameof(Resources.Diagnostic_MessageFormat_ClassMustHaveParameterlessConstructor));
    internal static readonly DiagnosticDescriptor Sas072 = "SAS072".GetDescriptor(DiagnosticSeverity.Error,
        AnalyzerConstants.Categories.Application, nameof(Resources.Diagnostic_Title_PropertyMustBeGettableAndSettable),
        nameof(Resources.Diagnostic_Description_PropertyMustBeGettableAndSettable),
        nameof(Resources.Diagnostic_MessageFormat_PropertyMustBeGettableAndSettable));
    internal static readonly DiagnosticDescriptor Sas073 = "SAS073".GetDescriptor(DiagnosticSeverity.Error,
        AnalyzerConstants.Categories.Application, nameof(Resources.Diagnostic_Title_PropertyMustBeNullableNotOptional),
        nameof(Resources.Diagnostic_Description_PropertyMustBeNullableNotOptional),
        nameof(Resources.Diagnostic_MessageFormat_PropertyMustBeNullableNotOptional));
    internal static readonly DiagnosticDescriptor Sas074 = "SAS074".GetDescriptor(DiagnosticSeverity.Error,
        AnalyzerConstants.Categories.Application, nameof(Resources.SAS074Title), nameof(Resources.SAS074Description),
        nameof(Resources.SAS074MessageFormat));
    internal static readonly DiagnosticDescriptor Sas080 = "SAS080".GetDescriptor(DiagnosticSeverity.Error,
        AnalyzerConstants.Categories.Application, nameof(Resources.Diagnostic_Title_ClassMustBePublic),
        nameof(Resources.Diagnostic_Description_ClassMustBePublic),
        nameof(Resources.Diagnostic_MessageFormat_ClassMustBePublic));
    internal static readonly DiagnosticDescriptor Sas081 = "SAS081".GetDescriptor(DiagnosticSeverity.Error,
        AnalyzerConstants.Categories.Application, nameof(Resources.Diagnostic_Title_MustDeclareEntityNameAttribute),
        nameof(Resources.Diagnostic_Description_MustDeclareEntityNameAttribute),
        nameof(Resources.Diagnostic_MessageFormat_MustDeclareEntityNameAttribute));
    internal static readonly DiagnosticDescriptor Sas082 = "SAS082".GetDescriptor(DiagnosticSeverity.Error,
        AnalyzerConstants.Categories.Application,
        nameof(Resources.Diagnostic_Title_ClassMustHaveParameterlessConstructor),
        nameof(Resources.Diagnostic_Description_ClassMustHaveParameterlessConstructor),
        nameof(Resources.Diagnostic_MessageFormat_ClassMustHaveParameterlessConstructor));
    internal static readonly DiagnosticDescriptor Sas083 = "SAS083".GetDescriptor(DiagnosticSeverity.Error,
        AnalyzerConstants.Categories.Application, nameof(Resources.Diagnostic_Title_PropertyMustBeGettableAndSettable),
        nameof(Resources.Diagnostic_Description_PropertyMustBeGettableAndSettable),
        nameof(Resources.Diagnostic_MessageFormat_PropertyMustBeGettableAndSettable));
    internal static readonly DiagnosticDescriptor Sas084 = "SAS084".GetDescriptor(DiagnosticSeverity.Warning,
        AnalyzerConstants.Categories.Application, nameof(Resources.SAS084Title), nameof(Resources.SAS084Description),
        nameof(Resources.SAS084MessageFormat));
    internal static readonly DiagnosticDescriptor Sas085 = "SAS085".GetDescriptor(DiagnosticSeverity.Error,
        AnalyzerConstants.Categories.Application, nameof(Resources.SAS085Title), nameof(Resources.SAS085Description),
        nameof(Resources.SAS085MessageFormat));

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
        ImmutableArray.Create(
            Sas070, Sas071, Sas072, Sas073, Sas074,
            Sas080, Sas081, Sas082, Sas083, Sas084, Sas085);

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterSyntaxNodeAction(AnalyzeResource, SyntaxKind.ClassDeclaration);
        context.RegisterSyntaxNodeAction(AnalyzeReadModel, SyntaxKind.ClassDeclaration);
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
            context.ReportDiagnostic(Sas070, classDeclarationSyntax);
        }

        var allConstructors = classDeclarationSyntax.Members.Where(member => member is ConstructorDeclarationSyntax)
            .Cast<ConstructorDeclarationSyntax>()
            .ToList();
        if (allConstructors.HasAny())
        {
            var parameterlessConstructors = allConstructors
                .Where(constructor => constructor.ParameterList.Parameters.Count == 0 && constructor.IsPublic());
            if (parameterlessConstructors.HasNone())
            {
                context.ReportDiagnostic(Sas071, classDeclarationSyntax);
            }
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
                    context.ReportDiagnostic(Sas072, property);
                }

                if (!property.IsNullableType(context) && property.IsOptionalType(context))
                {
                    context.ReportDiagnostic(Sas073, property);
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
                    context.ReportDiagnostic(Sas074, property, acceptableReturnTypes,
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
            context.ReportDiagnostic(Sas080, classDeclarationSyntax);
        }

        if (!classDeclarationSyntax.HasEntityNameAttribute(context))
        {
            context.ReportDiagnostic(Sas081, classDeclarationSyntax);
        }

        var allConstructors = classDeclarationSyntax.Members.Where(member => member is ConstructorDeclarationSyntax)
            .Cast<ConstructorDeclarationSyntax>()
            .ToList();
        if (allConstructors.HasAny())
        {
            var parameterlessConstructors = allConstructors
                .Where(constructor => constructor.ParameterList.Parameters.Count == 0 && constructor.IsPublic());
            if (parameterlessConstructors.HasNone())
            {
                context.ReportDiagnostic(Sas082, classDeclarationSyntax);
            }
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
                    context.ReportDiagnostic(Sas083, property);
                }

                if (property.IsNullableType(context))
                {
                    context.ReportDiagnostic(Sas084, property);
                }

                var allowedReturnTypes = context.GetAllowableApplicationReadModelPropertyReturnTypes();
                if (context.HasIncorrectReturnType(property, allowedReturnTypes)
                    && !property.IsEnumType(context)
                    && !property.IsValueObjectType(context))
                {
                    var acceptableReturnTypes =
                        allowedReturnTypes
                            .Where(allowable =>
                                !allowable.ToDisplayString().StartsWith("System.Collections")
                                && !allowable.ToDisplayString().StartsWith("Common.Optional"))
                            .Select(allowable => allowable.ToDisplayString()).Join(" or ");
                    context.ReportDiagnostic(Sas085, property, acceptableReturnTypes);
                }
            }
        }
    }
}

internal static partial class DomainDrivenDesignExtensions
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

    public static bool HasEntityNameAttribute(this ClassDeclarationSyntax classDeclarationSyntax,
        SyntaxNodeAnalysisContext context)
    {
        return HasEntityNameAttribute(context.SemanticModel, context.Compilation, classDeclarationSyntax);
    }

    public static bool IsOptionalType(this PropertyDeclarationSyntax propertyDeclarationSyntax,
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

        var returnType = propertySymbol.GetMethod!.ReturnType;
        if (returnType.IsOptionalType(context))
        {
            return true;
        }

        return false;
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
            returnType = ((INamedTypeSymbol)returnType).TypeArguments[1].WithoutNullable(context);
        }

        if (returnType.ContainingNamespace.ToDisplayString().StartsWith(containedNamespace))
        {
            return true;
        }

        return false;
    }

    private static ITypeSymbol WithoutOptional(this ITypeSymbol symbol, SyntaxNodeAnalysisContext context)
    {
        if (symbol.IsOptionalType(context))
        {
            return ((INamedTypeSymbol)symbol).TypeArguments[0];
        }

        return symbol;
    }

    public static bool IsValueObjectType(this PropertyDeclarationSyntax propertyDeclarationSyntax,
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

    public static bool IsEnumType(this PropertyDeclarationSyntax propertyDeclarationSyntax,
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

    private static bool IsOptionalType(this ITypeSymbol symbol, SyntaxNodeAnalysisContext context)
    {
        var optionalType = context.Compilation.GetTypeByMetadataName(typeof(global::Common.Optional<>).FullName!)!;

        return symbol.OriginalDefinition.IsOfType(optionalType);
    }
}