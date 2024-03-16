using System.Collections.Immutable;
using Common;
using Common.Extensions;
using Domain.Interfaces;
using Domain.Interfaces.Entities;
using Domain.Interfaces.ValueObjects;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using QueryAny;
using Tools.Analyzers.Common;
using Tools.Analyzers.Common.Extensions;
using Tools.Analyzers.NonPlatform.Extensions;

namespace Tools.Analyzers.NonPlatform;

/// <summary>
///     An analyzer to the correct implementation of root aggregates, entities, value objects and domain events:
///     Aggregate Roots:
///     SAS030: Error: Aggregate roots must have at least one Create() class factory method
///     SAS031: Error: Create() class factory methods must return correct types
///     SAS032: Error: Aggregate roots must raise a create event in the class factory
///     SAS033: Error: Aggregate roots must only have private constructors
///     SAS034: Error: Aggregate roots must have a <see cref="IRehydratableObject.Rehydrate" /> method
///     SAS035: Error: Dehydratable aggregate roots must override the <see cref="IDehydratableEntity.Dehydrate" /> method
///     SAS036: Error: Dehydratable aggregate roots must declare the <see cref="EntityNameAttribute" />
///     SAS037: Error: Properties must not have public setters
///     SAS038: Error: Aggregate roots must be marked as sealed
///     Entities:
///     SAS040: Error: Entities must have at least one Create() class factory method
///     SAS041: Error: Create() class factory methods must return correct types
///     SAS042: Error: Entities must only have private constructors
///     SAS043: Error: Dehydratable entities must have a <see cref="IRehydratableObject.Rehydrate" /> method
///     SAS044: Error: Dehydratable entities must override the <see cref="IDehydratableEntity.Dehydrate" /> method
///     SAS045: Error: Dehydratable entities must declare the <see cref="EntityNameAttribute" />
///     SAS046: Error: Properties must not have public setters
///     SAS047: Error: Entities must be marked as sealed
///     Value Objects:
///     SAS050: Error: ValueObjects must have at least one Create() class factory method
///     SAS051: Error: Create() class factory methods must return correct types
///     SAS052: Error: ValueObjects must only have private constructors
///     SAS053: Error: ValueObjects must have a <see cref="IRehydratableObject.Rehydrate" /> method
///     SAS054: Error: Properties must not have public setters
///     SAS055: Error: ValueObjects must only have immutable methods (or be overridden by the
///     <see cref="SkipImmutabilityCheckAttribute" />)
///     SAS056: Warning: ValueObjects should be marked as sealed
///     DomainEvents:
///     SAS060: Error: DomainEvents must be public
///     SAS061: Warning: DomainEvents must be sealed
///     SAS062: Error: DomainEvents must have a parameterless constructor
///     SAS063: Information: DomainEvents must be named in the past tense
///     SAS064: Error: DomainEvents must have at least one Create() class factory method
///     SAS065: Error: Create() class factory methods must return correct types
///     SAS066: Error: Properties must have public getters and setters
///     SAS067: Error: Properties must be required or nullable or initialized
///     SAS068: Error: Properties must be nullable, not Optional{T} for interoperability
///     SAS069: Error: Properties must have return type of primitives, List{TPrimitive}, Dictionary{string,TPrimitive},
///     or be enums
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class DomainDrivenDesignAnalyzer : DiagnosticAnalyzer
{
    public const string ClassFactoryMethodName = "Create";
    public const string ConstructorMethodCall = "RaiseCreateEvent"; // AggregateRootBase<T>.RaiseCreateEvent
    internal static readonly SpecialType[] AllowableDomainEventPrimitives =
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
    internal static readonly DiagnosticDescriptor Sas030 = "SAS030".GetDescriptor(DiagnosticSeverity.Error,
        AnalyzerConstants.Categories.Ddd, nameof(Resources.SAS030Title), nameof(Resources.SAS030Description),
        nameof(Resources.SAS030MessageFormat));
    internal static readonly DiagnosticDescriptor Sas031 = "SAS031".GetDescriptor(DiagnosticSeverity.Error,
        AnalyzerConstants.Categories.Ddd, nameof(Resources.Diagnostic_Title_ClassFactoryWrongReturnType),
        nameof(Resources.Diagnostic_Description_ClassFactoryWrongReturnType),
        nameof(Resources.Diagnostic_MessageFormat_ClassFactoryWrongReturnType));
    internal static readonly DiagnosticDescriptor Sas032 = "SAS032".GetDescriptor(DiagnosticSeverity.Error,
        AnalyzerConstants.Categories.Ddd, nameof(Resources.SAS032Title), nameof(Resources.SAS032Description),
        nameof(Resources.SAS032MessageFormat));
    internal static readonly DiagnosticDescriptor Sas033 = "SAS033".GetDescriptor(DiagnosticSeverity.Error,
        AnalyzerConstants.Categories.Ddd, nameof(Resources.Diagnostic_Title_ConstructorMustBePrivate),
        nameof(Resources.Diagnostic_Description_ConstructorMustBePrivate),
        nameof(Resources.Diagnostic_MessageFormat_ConstructorMustBePrivate));
    internal static readonly DiagnosticDescriptor Sas034 = "SAS034".GetDescriptor(DiagnosticSeverity.Error,
        AnalyzerConstants.Categories.Ddd, nameof(Resources.Diagnostic_Title_MustImplementRehydrate),
        nameof(Resources.Diagnostic_Description_MustImplementRehydrate),
        nameof(Resources.Diagnostic_MessageFormat_MustImplementRehydrate));
    internal static readonly DiagnosticDescriptor Sas035 = "SAS035".GetDescriptor(DiagnosticSeverity.Error,
        AnalyzerConstants.Categories.Ddd, nameof(Resources.Diagnostic_Title_MustImplementDehydrate),
        nameof(Resources.Diagnostic_Description_MustImplementDehydrate),
        nameof(Resources.Diagnostic_MessageFormat_MustImplementDehydrate));
    internal static readonly DiagnosticDescriptor Sas036 = "SAS036".GetDescriptor(DiagnosticSeverity.Error,
        AnalyzerConstants.Categories.Ddd, nameof(Resources.Diagnostic_Title_MustDeclareEntityNameAttribute),
        nameof(Resources.Diagnostic_Description_MustDeclareEntityNameAttribute),
        nameof(Resources.Diagnostic_MessageFormat_MustDeclareEntityNameAttribute));
    internal static readonly DiagnosticDescriptor Sas037 = "SAS037".GetDescriptor(DiagnosticSeverity.Error,
        AnalyzerConstants.Categories.Ddd, nameof(Resources.Diagnostic_Title_PropertyMustBeSettable),
        nameof(Resources.Diagnostic_Description_PropertyMustBeSettable),
        nameof(Resources.Diagnostic_MessageFormat_PropertyMustBeSettable));
    internal static readonly DiagnosticDescriptor Sas038 = "SAS038".GetDescriptor(DiagnosticSeverity.Error,
        AnalyzerConstants.Categories.Ddd, nameof(Resources.Diagnostic_Title_ClassMustBeSealed),
        nameof(Resources.Diagnostic_Description_ClassMustBeSealed),
        nameof(Resources.Diagnostic_MessageFormat_ClassMustBeSealed));
    internal static readonly DiagnosticDescriptor Sas040 = "SAS040".GetDescriptor(DiagnosticSeverity.Error,
        AnalyzerConstants.Categories.Ddd, nameof(Resources.SAS040Title), nameof(Resources.SAS040Description),
        nameof(Resources.SAS040MessageFormat));
    internal static readonly DiagnosticDescriptor Sas041 = "SAS041".GetDescriptor(DiagnosticSeverity.Error,
        AnalyzerConstants.Categories.Ddd, nameof(Resources.Diagnostic_Title_ClassFactoryWrongReturnType),
        nameof(Resources.Diagnostic_Description_ClassFactoryWrongReturnType),
        nameof(Resources.Diagnostic_MessageFormat_ClassFactoryWrongReturnType));
    internal static readonly DiagnosticDescriptor Sas042 = "SAS042".GetDescriptor(DiagnosticSeverity.Error,
        AnalyzerConstants.Categories.Ddd, nameof(Resources.Diagnostic_Title_ConstructorMustBePrivate),
        nameof(Resources.Diagnostic_Description_ConstructorMustBePrivate),
        nameof(Resources.Diagnostic_MessageFormat_ConstructorMustBePrivate));
    internal static readonly DiagnosticDescriptor Sas043 = "SAS043".GetDescriptor(DiagnosticSeverity.Error,
        AnalyzerConstants.Categories.Ddd, nameof(Resources.Diagnostic_Title_MustImplementRehydrate),
        nameof(Resources.Diagnostic_Description_MustImplementRehydrate),
        nameof(Resources.Diagnostic_MessageFormat_MustImplementRehydrate));
    internal static readonly DiagnosticDescriptor Sas044 = "SAS044".GetDescriptor(DiagnosticSeverity.Error,
        AnalyzerConstants.Categories.Ddd, nameof(Resources.Diagnostic_Title_MustImplementDehydrate),
        nameof(Resources.Diagnostic_Description_MustImplementDehydrate),
        nameof(Resources.Diagnostic_MessageFormat_MustImplementDehydrate));
    internal static readonly DiagnosticDescriptor Sas045 = "SAS045".GetDescriptor(DiagnosticSeverity.Error,
        AnalyzerConstants.Categories.Ddd, nameof(Resources.Diagnostic_Title_MustDeclareEntityNameAttribute),
        nameof(Resources.Diagnostic_Description_MustDeclareEntityNameAttribute),
        nameof(Resources.Diagnostic_MessageFormat_MustDeclareEntityNameAttribute));
    internal static readonly DiagnosticDescriptor Sas046 = "SAS046".GetDescriptor(DiagnosticSeverity.Error,
        AnalyzerConstants.Categories.Ddd, nameof(Resources.Diagnostic_Title_PropertyMustBeSettable),
        nameof(Resources.Diagnostic_Description_PropertyMustBeSettable),
        nameof(Resources.Diagnostic_MessageFormat_PropertyMustBeSettable));
    internal static readonly DiagnosticDescriptor Sas047 = "SAS047".GetDescriptor(DiagnosticSeverity.Error,
        AnalyzerConstants.Categories.Ddd, nameof(Resources.Diagnostic_Title_ClassMustBeSealed),
        nameof(Resources.Diagnostic_Description_ClassMustBeSealed),
        nameof(Resources.Diagnostic_MessageFormat_ClassMustBeSealed));
    internal static readonly DiagnosticDescriptor Sas050 = "SAS050".GetDescriptor(DiagnosticSeverity.Error,
        AnalyzerConstants.Categories.Ddd, nameof(Resources.SAS050Title), nameof(Resources.SAS050Description),
        nameof(Resources.SAS050MessageFormat));
    internal static readonly DiagnosticDescriptor Sas051 = "SAS051".GetDescriptor(DiagnosticSeverity.Error,
        AnalyzerConstants.Categories.Ddd, nameof(Resources.Diagnostic_Title_ClassFactoryWrongReturnType),
        nameof(Resources.Diagnostic_Description_ClassFactoryWrongReturnType),
        nameof(Resources.Diagnostic_MessageFormat_ClassFactoryWrongReturnType));
    internal static readonly DiagnosticDescriptor Sas052 = "SAS052".GetDescriptor(DiagnosticSeverity.Error,
        AnalyzerConstants.Categories.Ddd, nameof(Resources.Diagnostic_Title_ConstructorMustBePrivate),
        nameof(Resources.Diagnostic_Description_ConstructorMustBePrivate),
        nameof(Resources.Diagnostic_MessageFormat_ConstructorMustBePrivate));
    internal static readonly DiagnosticDescriptor Sas053 = "SAS053".GetDescriptor(DiagnosticSeverity.Error,
        AnalyzerConstants.Categories.Ddd, nameof(Resources.Diagnostic_Title_MustImplementRehydrate),
        nameof(Resources.Diagnostic_Description_MustImplementRehydrate),
        nameof(Resources.Diagnostic_MessageFormat_MustImplementRehydrate));
    internal static readonly DiagnosticDescriptor Sas054 = "SAS054".GetDescriptor(DiagnosticSeverity.Error,
        AnalyzerConstants.Categories.Ddd, nameof(Resources.Diagnostic_Title_PropertyMustBeSettable),
        nameof(Resources.Diagnostic_Description_PropertyMustBeSettable),
        nameof(Resources.Diagnostic_MessageFormat_PropertyMustBeSettable));
    internal static readonly DiagnosticDescriptor Sas055 = "SAS055".GetDescriptor(DiagnosticSeverity.Error,
        AnalyzerConstants.Categories.Ddd, nameof(Resources.SAS055Title), nameof(Resources.SAS055Description),
        nameof(Resources.SAS055MessageFormat));
    internal static readonly DiagnosticDescriptor Sas056 = "SAS056".GetDescriptor(DiagnosticSeverity.Warning,
        AnalyzerConstants.Categories.Ddd, nameof(Resources.Diagnostic_Title_ClassMustBeSealed),
        nameof(Resources.Diagnostic_Description_ClassMustBeSealed),
        nameof(Resources.Diagnostic_MessageFormat_ClassMustBeSealed));
    internal static readonly DiagnosticDescriptor Sas060 = "SAS060".GetDescriptor(DiagnosticSeverity.Error,
        AnalyzerConstants.Categories.Ddd, nameof(Resources.Diagnostic_Title_ClassMustBePublic),
        nameof(Resources.Diagnostic_Description_ClassMustBePublic),
        nameof(Resources.Diagnostic_MessageFormat_ClassMustBePublic));
    internal static readonly DiagnosticDescriptor Sas061 = "SAS061".GetDescriptor(DiagnosticSeverity.Warning,
        AnalyzerConstants.Categories.Ddd, nameof(Resources.Diagnostic_Title_ClassMustBeSealed),
        nameof(Resources.Diagnostic_Description_ClassMustBeSealed),
        nameof(Resources.Diagnostic_MessageFormat_ClassMustBeSealed));
    internal static readonly DiagnosticDescriptor Sas062 = "SAS062".GetDescriptor(DiagnosticSeverity.Error,
        AnalyzerConstants.Categories.Ddd, nameof(Resources.Diagnostic_Title_ClassMustHaveParameterlessConstructor),
        nameof(Resources.Diagnostic_Description_ClassMustHaveParameterlessConstructor),
        nameof(Resources.Diagnostic_MessageFormat_ClassMustHaveParameterlessConstructor));
    internal static readonly DiagnosticDescriptor Sas063 = "SAS063".GetDescriptor(DiagnosticSeverity.Info,
        AnalyzerConstants.Categories.Ddd, nameof(Resources.SAS063Title), nameof(Resources.SAS063Description),
        nameof(Resources.SAS063MessageFormat));
    internal static readonly DiagnosticDescriptor Sas064 = "SAS064".GetDescriptor(DiagnosticSeverity.Error,
        AnalyzerConstants.Categories.Ddd, nameof(Resources.SAS064Title), nameof(Resources.SAS064Description),
        nameof(Resources.SAS064MessageFormat));
    internal static readonly DiagnosticDescriptor Sas065 = "SAS065".GetDescriptor(DiagnosticSeverity.Error,
        AnalyzerConstants.Categories.Ddd, nameof(Resources.SAS065Title), nameof(Resources.SAS065Description),
        nameof(Resources.SAS065MessageFormat));
    internal static readonly DiagnosticDescriptor Sas066 = "SAS066".GetDescriptor(DiagnosticSeverity.Error,
        AnalyzerConstants.Categories.Ddd, nameof(Resources.Diagnostic_Title_PropertyMustBeGettableAndSettable),
        nameof(Resources.Diagnostic_Description_PropertyMustBeGettableAndSettable),
        nameof(Resources.Diagnostic_MessageFormat_PropertyMustBeGettableAndSettable));
    internal static readonly DiagnosticDescriptor Sas067 = "SAS067".GetDescriptor(DiagnosticSeverity.Error,
        AnalyzerConstants.Categories.Ddd, nameof(Resources.SAS067Title), nameof(Resources.SAS067Description),
        nameof(Resources.SAS067MessageFormat));
    internal static readonly DiagnosticDescriptor Sas068 = "SAS068".GetDescriptor(DiagnosticSeverity.Error,
        AnalyzerConstants.Categories.Ddd, nameof(Resources.Diagnostic_Title_PropertyMustBeNullableNotOptional),
        nameof(Resources.Diagnostic_Description_PropertyMustBeNullableNotOptional),
        nameof(Resources.Diagnostic_MessageFormat_PropertyMustBeNullableNotOptional));
    internal static readonly DiagnosticDescriptor Sas069 = "SAS069".GetDescriptor(DiagnosticSeverity.Error,
        AnalyzerConstants.Categories.Ddd, nameof(Resources.SAS069Title), nameof(Resources.SAS069Description),
        nameof(Resources.SAS069MessageFormat));
    private static readonly string[] IgnoredValueObjectMethodNames = { nameof(IDehydratableEntity.Dehydrate) };

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
        ImmutableArray.Create(
            Sas030, Sas031, Sas032, Sas033, Sas034, Sas035, Sas036, Sas037, Sas038,
            Sas040, Sas041, Sas042, Sas043, Sas044, Sas045, Sas046, Sas047,
            Sas050, Sas051, Sas052, Sas053, Sas054, Sas055, Sas056,
            Sas060, Sas061, Sas062, Sas063, Sas064, Sas065, Sas066, Sas067, Sas068, Sas069);

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterSyntaxNodeAction(AnalyzeAggregateRoot, SyntaxKind.ClassDeclaration);
        context.RegisterSyntaxNodeAction(AnalyzeEntity, SyntaxKind.ClassDeclaration);
        context.RegisterSyntaxNodeAction(AnalyzeValueObject, SyntaxKind.ClassDeclaration);
        context.RegisterSyntaxNodeAction(AnalyzeDomainEvent, SyntaxKind.ClassDeclaration);
    }

    private static void AnalyzeAggregateRoot(SyntaxNodeAnalysisContext context)
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

        if (classDeclarationSyntax.IsNotType<IAggregateRoot>(context))
        {
            return;
        }

        var allMethods = classDeclarationSyntax.Members.Where(member => member is MethodDeclarationSyntax)
            .Cast<MethodDeclarationSyntax>()
            .ToList();
        var classFactoryMethods = allMethods
            .Where(method => method.IsPublicStaticMethod() && method.IsNamed(ClassFactoryMethodName))
            .ToList();
        if (classFactoryMethods.HasNone())
        {
            context.ReportDiagnostic(Sas030, classDeclarationSyntax);
        }
        else
        {
            foreach (var method in classFactoryMethods)
            {
                var allowedReturnTypes = context.GetAllowableClassFactoryReturnTypes(classDeclarationSyntax);
                if (context.HasIncorrectReturnType(method, allowedReturnTypes))
                {
                    var acceptableReturnTypes =
                        allowedReturnTypes.Select(allowable => allowable.ToDisplayString()).Join(" or ");
                    context.ReportDiagnostic(Sas031, method, acceptableReturnTypes);
                }

                if (context.IsMissingContent(method, ConstructorMethodCall))
                {
                    context.ReportDiagnostic(Sas032, method, ConstructorMethodCall);
                }
            }
        }

        var allConstructors = classDeclarationSyntax.Members.Where(member => member is ConstructorDeclarationSyntax)
            .Cast<ConstructorDeclarationSyntax>()
            .ToList();
        if (allConstructors.HasAny())
        {
            foreach (var constructor in allConstructors)
            {
                if (!constructor.IsPrivateInstanceConstructor())
                {
                    context.ReportDiagnostic(Sas033, constructor);
                }
            }
        }

        var dehydratable = context.IsDehydratableAggregateRoot(classDeclarationSyntax);
        if (!dehydratable.ImplementsRehydrate)
        {
            context.ReportDiagnostic(Sas034, classDeclarationSyntax);
        }

        if (dehydratable is { IsDehydratable: true, ImplementsDehydrate: false })
        {
            context.ReportDiagnostic(Sas035, classDeclarationSyntax);
        }

        if (dehydratable is { IsDehydratable: true, MarkedAsEntityName: false })
        {
            context.ReportDiagnostic(Sas036, classDeclarationSyntax);
        }

        var allProperties = classDeclarationSyntax.Members.Where(member => member is PropertyDeclarationSyntax)
            .Cast<PropertyDeclarationSyntax>()
            .ToList();
        if (allProperties.HasAny())
        {
            foreach (var property in allProperties)
            {
                if (property.HasPublicSetter())
                {
                    context.ReportDiagnostic(Sas037, property);
                }
            }
        }

        if (!classDeclarationSyntax.IsSealed())
        {
            context.ReportDiagnostic(Sas038, classDeclarationSyntax);
        }
    }

    private static void AnalyzeEntity(SyntaxNodeAnalysisContext context)
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

        if (classDeclarationSyntax.IsNotType<IEntity>(context))
        {
            return;
        }

        var allMethods = classDeclarationSyntax.Members.Where(member => member is MethodDeclarationSyntax)
            .Cast<MethodDeclarationSyntax>()
            .ToList();
        var classFactoryMethods = allMethods
            .Where(method => method.IsPublicStaticMethod() && method.IsNamed(ClassFactoryMethodName))
            .ToList();
        if (classFactoryMethods.HasNone())
        {
            context.ReportDiagnostic(Sas040, classDeclarationSyntax);
        }
        else
        {
            foreach (var method in classFactoryMethods)
            {
                var allowedReturnTypes = context.GetAllowableClassFactoryReturnTypes(classDeclarationSyntax);
                if (context.HasIncorrectReturnType(method, allowedReturnTypes))
                {
                    var acceptableReturnTypes =
                        allowedReturnTypes.Select(allowable => allowable.ToDisplayString()).Join(" or ");
                    context.ReportDiagnostic(Sas041, method, acceptableReturnTypes);
                }
            }
        }

        var allConstructors = classDeclarationSyntax.Members.Where(member => member is ConstructorDeclarationSyntax)
            .Cast<ConstructorDeclarationSyntax>()
            .ToList();
        if (allConstructors.HasAny())
        {
            foreach (var constructor in allConstructors)
            {
                if (!constructor.IsPrivateInstanceConstructor())
                {
                    context.ReportDiagnostic(Sas042, constructor);
                }
            }
        }

        var dehydratable = context.IsDehydratableEntity(classDeclarationSyntax);
        if (dehydratable is { IsDehydratable: true, ImplementsRehydrate: false })
        {
            context.ReportDiagnostic(Sas043, classDeclarationSyntax);
        }

        if (dehydratable is { IsDehydratable: true, ImplementsDehydrate: false })
        {
            context.ReportDiagnostic(Sas044, classDeclarationSyntax);
        }

        if (dehydratable is { IsDehydratable: true, MarkedAsEntityName: false })
        {
            context.ReportDiagnostic(Sas045, classDeclarationSyntax);
        }

        var allProperties = classDeclarationSyntax.Members.Where(member => member is PropertyDeclarationSyntax)
            .Cast<PropertyDeclarationSyntax>()
            .ToList();
        if (allProperties.HasAny())
        {
            foreach (var property in allProperties)
            {
                if (property.HasPublicSetter())
                {
                    context.ReportDiagnostic(Sas046, property);
                }
            }
        }

        if (!classDeclarationSyntax.IsSealed())
        {
            context.ReportDiagnostic(Sas047, classDeclarationSyntax);
        }
    }

    private static void AnalyzeValueObject(SyntaxNodeAnalysisContext context)
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

        if (classDeclarationSyntax.IsNotType<IValueObject>(context))
        {
            return;
        }

        var allMethods = classDeclarationSyntax.Members.Where(member => member is MethodDeclarationSyntax)
            .Cast<MethodDeclarationSyntax>()
            .ToList();
        var classFactoryMethods = allMethods
            .Where(method => method.IsPublicStaticMethod() && method.IsNamed(ClassFactoryMethodName))
            .ToList();
        if (classFactoryMethods.HasNone())
        {
            context.ReportDiagnostic(Sas050, classDeclarationSyntax);
        }
        else
        {
            foreach (var method in classFactoryMethods)
            {
                var allowedReturnTypes = context.GetAllowableClassFactoryReturnTypes(classDeclarationSyntax);
                if (context.HasIncorrectReturnType(method, allowedReturnTypes))
                {
                    var acceptableReturnTypes =
                        allowedReturnTypes.Select(allowable => allowable.ToDisplayString()).Join(" or ");
                    context.ReportDiagnostic(Sas051, method, acceptableReturnTypes);
                }
            }
        }

        var allConstructors = classDeclarationSyntax.Members.Where(member => member is ConstructorDeclarationSyntax)
            .Cast<ConstructorDeclarationSyntax>()
            .ToList();
        if (allConstructors.HasAny())
        {
            foreach (var constructor in allConstructors)
            {
                if (!constructor.IsPrivateInstanceConstructor())
                {
                    context.ReportDiagnostic(Sas052, constructor);
                }
            }
        }

        var dehydratable = context.IsDehydratableValueObject(classDeclarationSyntax);
        if (!dehydratable.ImplementsRehydrate)
        {
            context.ReportDiagnostic(Sas053, classDeclarationSyntax);
        }

        var allProperties = classDeclarationSyntax.Members.Where(member => member is PropertyDeclarationSyntax)
            .Cast<PropertyDeclarationSyntax>()
            .ToList();
        if (allProperties.HasAny())
        {
            foreach (var property in allProperties)
            {
                if (property.HasPublicSetter())
                {
                    context.ReportDiagnostic(Sas054, property);
                }
            }
        }

        var allImmutableMethods =
            classDeclarationSyntax.Members
                .Where(member => member is not ConstructorDeclarationSyntax)
                .Where(member => member is MethodDeclarationSyntax)
                .Cast<MethodDeclarationSyntax>()
                .Where(method => method.IsPublicOrInternalInstanceMethod())
                .Where(method => method.GetAttributeOfType<SkipImmutabilityCheckAttribute>(context).NotExists())
                .Where(method => IgnoredValueObjectMethodNames.NotContainsIgnoreCase(method.Identifier.Text));
        foreach (var method in allImmutableMethods)
        {
            var allowedReturnTypes = context.GetAllowableValueObjectMutableMethodReturnTypes(classDeclarationSyntax);
            if (context.HasIncorrectReturnType(method, allowedReturnTypes))
            {
                var acceptableReturnTypes =
                    allowedReturnTypes.Select(allowable => allowable.ToDisplayString()).Join(" or ");
                context.ReportDiagnostic(Sas055, method, acceptableReturnTypes, nameof(SkipImmutabilityCheckAttribute));
            }
        }

        if (!classDeclarationSyntax.IsSealed())
        {
            context.ReportDiagnostic(Sas056, classDeclarationSyntax);
        }
    }

    private static void AnalyzeDomainEvent(SyntaxNodeAnalysisContext context)
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

        if (classDeclarationSyntax.IsNotType<IDomainEvent>(context))
        {
            return;
        }

        if (!context.IsNamedInPastTense(classDeclarationSyntax))
        {
            context.ReportDiagnostic(Sas063, classDeclarationSyntax);
        }

        if (!classDeclarationSyntax.IsPublic())
        {
            context.ReportDiagnostic(Sas060, classDeclarationSyntax);
        }

        if (!classDeclarationSyntax.IsSealed())
        {
            context.ReportDiagnostic(Sas061, classDeclarationSyntax);
        }

        var allMethods = classDeclarationSyntax.Members.Where(member => member is MethodDeclarationSyntax)
            .Cast<MethodDeclarationSyntax>()
            .ToList();
        var classFactoryMethods = allMethods
            .Where(method => method.IsPublicStaticMethod() && method.IsNamed(ClassFactoryMethodName))
            .ToList();
        if (classFactoryMethods.HasNone())
        {
            context.ReportDiagnostic(Sas064, classDeclarationSyntax);
        }
        else
        {
            foreach (var method in classFactoryMethods)
            {
                var allowedReturnTypes = context.GetAllowableDomainEventFactoryReturnTypes(classDeclarationSyntax);
                if (context.HasIncorrectReturnType(method, allowedReturnTypes))
                {
                    var acceptableReturnTypes =
                        allowedReturnTypes.Select(allowable => allowable.ToDisplayString()).Join(" or ");
                    context.ReportDiagnostic(Sas065, method, acceptableReturnTypes);
                }
            }
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
                context.ReportDiagnostic(Sas062, classDeclarationSyntax);
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
                    context.ReportDiagnostic(Sas066, property);
                }

                if (!property.IsRequired() && !property.IsInitialized())
                {
                    if (!property.IsNullableType(context))
                    {
                        context.ReportDiagnostic(Sas067, property);
                    }
                }

                if (!property.IsNullableType(context) && property.IsOptionalType(context))
                {
                    context.ReportDiagnostic(Sas068, property);
                }

                var allowedReturnTypes = context.GetAllowableDomainEventPropertyReturnTypes();
                if (context.HasIncorrectReturnType(property, allowedReturnTypes)
                    && !property.IsEnumType(context))
                {
                    var acceptableReturnTypes =
                        allowedReturnTypes
                            .Where(allowable =>
                                !allowable.ToDisplayString().StartsWith("System.Collections")
                                && !allowable.ToDisplayString().EndsWith("?"))
                            .Select(allowable => allowable.ToDisplayString()).Join(" or ");
                    context.ReportDiagnostic(Sas069, property, acceptableReturnTypes);
                }
            }
        }
    }
}

internal static partial class DomainDrivenDesignExtensions
{
    private static INamedTypeSymbol[]? _allowableDomainEventPropertyReturnTypes;

    public static INamedTypeSymbol[] GetAllowableClassFactoryReturnTypes(this SyntaxNodeAnalysisContext context,
        ClassDeclarationSyntax classDeclarationSyntax)
    {
        var classSymbol = context.SemanticModel.GetDeclaredSymbol(classDeclarationSyntax);
        if (classSymbol is null)
        {
            return Array.Empty<INamedTypeSymbol>();
        }

        var errorType = context.Compilation.GetTypeByMetadataName(typeof(Error).FullName!)!;
        var resultType = context.Compilation.GetTypeByMetadataName(typeof(Result<,>).FullName!)!;
        var resultOfClassAndErrorType = resultType.Construct(classSymbol, errorType);

        return new[] { classSymbol, resultOfClassAndErrorType };
    }

    public static INamedTypeSymbol[] GetAllowableDomainEventFactoryReturnTypes(this SyntaxNodeAnalysisContext context,
        ClassDeclarationSyntax classDeclarationSyntax)
    {
        var classSymbol = context.SemanticModel.GetDeclaredSymbol(classDeclarationSyntax);
        if (classSymbol is null)
        {
            return Array.Empty<INamedTypeSymbol>();
        }

        return new[] { classSymbol };
    }

    public static INamedTypeSymbol[] GetAllowableDomainEventPropertyReturnTypes(this SyntaxNodeAnalysisContext context)
    {
        // Cache this
        if (_allowableDomainEventPropertyReturnTypes is null)
        {
            var stringType = context.Compilation.GetSpecialType(SpecialType.System_String);
            var primitiveTypes = DomainDrivenDesignAnalyzer.AllowableDomainEventPrimitives
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

            _allowableDomainEventPropertyReturnTypes = primitiveTypes
                .Concat(nullableTypes)
                .Concat(listTypes)
                .Concat(stringDictionaryTypes).ToArray();
        }

        return _allowableDomainEventPropertyReturnTypes;
    }

    public static INamedTypeSymbol[] GetAllowableValueObjectMutableMethodReturnTypes(
        this SyntaxNodeAnalysisContext context, ClassDeclarationSyntax classDeclarationSyntax)
    {
        var classSymbol = context.SemanticModel.GetDeclaredSymbol(classDeclarationSyntax);
        if (classSymbol is null)
        {
            return Array.Empty<INamedTypeSymbol>();
        }

        var errorType = context.Compilation.GetTypeByMetadataName(typeof(Error).FullName!)!;
        var resultType = context.Compilation.GetTypeByMetadataName(typeof(Result<,>).FullName!)!;
        var resultOfClassAndErrorType = resultType.Construct(classSymbol, errorType);

        return new[] { classSymbol, resultOfClassAndErrorType };
    }

    public static bool HasIncorrectReturnType(this SyntaxNodeAnalysisContext context,
        MethodDeclarationSyntax methodDeclarationSyntax, INamedTypeSymbol[] allowableTypes)
    {
        var semanticModel = context.SemanticModel;
        var compilation = context.Compilation;
        return semanticModel.HasIncorrectReturnType(compilation, methodDeclarationSyntax, allowableTypes);
    }

    public static bool HasIncorrectReturnType(this SyntaxNodeAnalysisContext context,
        PropertyDeclarationSyntax propertyDeclarationSyntax, INamedTypeSymbol[] allowableTypes)
    {
        var semanticModel = context.SemanticModel;
        var compilation = context.Compilation;
        return semanticModel.HasIncorrectReturnType(compilation, propertyDeclarationSyntax, allowableTypes);
    }

    public static DehydratableStatus IsDehydratableAggregateRoot(this SyntaxNodeAnalysisContext context,
        ClassDeclarationSyntax classDeclarationSyntax)
    {
        return context.SemanticModel.IsDehydratableAggregateRoot(context.Compilation, classDeclarationSyntax);
    }

    public static DehydratableStatus IsDehydratableAggregateRoot(this SemanticModel semanticModel,
        Compilation compilation, ClassDeclarationSyntax classDeclarationSyntax)
    {
        var allMethods = classDeclarationSyntax.Members.Where(member => member is MethodDeclarationSyntax)
            .Cast<MethodDeclarationSyntax>()
            .ToList();
        var implementsRehydrate =
            ImplementsAggregateRehydrateMethod(semanticModel, compilation, classDeclarationSyntax, allMethods);
        var implementsDehydrate =
            ImplementsDehydrateMethod(semanticModel, compilation, classDeclarationSyntax, allMethods);
        var hasAttribute = HasEntityNameAttribute(semanticModel, compilation, classDeclarationSyntax);
        return new DehydratableStatus(implementsDehydrate, implementsRehydrate, hasAttribute,
            () => implementsDehydrate || hasAttribute);
    }

    public static DehydratableStatus IsDehydratableEntity(this SyntaxNodeAnalysisContext context,
        ClassDeclarationSyntax classDeclarationSyntax)
    {
        return context.SemanticModel.IsDehydratableEntity(context.Compilation, classDeclarationSyntax);
    }

    public static DehydratableStatus IsDehydratableValueObject(this SyntaxNodeAnalysisContext context,
        ClassDeclarationSyntax classDeclarationSyntax)
    {
        return context.SemanticModel.IsDehydratableValueObject(context.Compilation, classDeclarationSyntax);
    }

    public static bool IsMissingContent(this SyntaxNodeAnalysisContext context,
        MethodDeclarationSyntax methodDeclarationSyntax, string match)
    {
        var methodSymbol = context.SemanticModel.GetDeclaredSymbol(methodDeclarationSyntax);
        if (methodSymbol is null)
        {
            return true;
        }

        var body = methodSymbol.GetMethodBody();
        return !body.Contains(match);
    }

    public static bool IsNamedInPastTense(this SyntaxNodeAnalysisContext context,
        ClassDeclarationSyntax classDeclarationSyntax)
    {
        var typeSymbol = context.SemanticModel.GetDeclaredSymbol(classDeclarationSyntax);
        if (typeSymbol is null)
        {
            return false;
        }

        var name = typeSymbol.Name;

        //HACK: this will work when using a regular verb, but not when using irregular verbs
        return name.EndsWith("ed", StringComparison.OrdinalIgnoreCase);
    }

    public static bool IsSingleValueValueObject(this SemanticModel semanticModel,
        Compilation compilation, ClassDeclarationSyntax classDeclarationSyntax)
    {
        var symbol = semanticModel.GetDeclaredSymbol(classDeclarationSyntax);
        if (symbol is null)
        {
            return false;
        }

        var singleValueObject = compilation.GetTypeByMetadataName(typeof(ISingleValueObject<>).FullName!)!;
        return symbol.AllInterfaces.Any(@interface => @interface.OriginalDefinition.IsOfType(singleValueObject));
    }

    private static DehydratableStatus IsDehydratableEntity(this SemanticModel semanticModel, Compilation compilation,
        ClassDeclarationSyntax classDeclarationSyntax)
    {
        var allMethods = classDeclarationSyntax.Members.Where(member => member is MethodDeclarationSyntax)
            .Cast<MethodDeclarationSyntax>()
            .ToList();
        var implementsRehydrate =
            ImplementsEntityRehydrateMethod(semanticModel, compilation, classDeclarationSyntax, allMethods);
        var implementsDehydrate =
            ImplementsDehydrateMethod(semanticModel, compilation, classDeclarationSyntax, allMethods);
        var hasAttribute = HasEntityNameAttribute(semanticModel, compilation, classDeclarationSyntax);
        return new DehydratableStatus(implementsDehydrate, implementsRehydrate, hasAttribute,
            () => implementsRehydrate || hasAttribute || implementsRehydrate);
    }

    private static DehydratableStatus IsDehydratableValueObject(this SemanticModel semanticModel,
        Compilation compilation, ClassDeclarationSyntax classDeclarationSyntax)
    {
        var allMethods = classDeclarationSyntax.Members.Where(member => member is MethodDeclarationSyntax)
            .Cast<MethodDeclarationSyntax>()
            .ToList();
        var implementsRehydrate =
            ImplementsValueObjectRehydrateMethod(semanticModel, compilation, classDeclarationSyntax, allMethods);
        return new DehydratableStatus(true, implementsRehydrate, false, () => true);
    }

    private static bool HasIncorrectReturnType(this SemanticModel semanticModel, Compilation compilation,
        MethodDeclarationSyntax methodDeclarationSyntax, INamedTypeSymbol[] allowableTypes)
    {
        var methodSymbol = semanticModel.GetDeclaredSymbol(methodDeclarationSyntax);
        if (methodSymbol is null)
        {
            return true;
        }

        var returnType = methodSymbol.ReturnType;
        if (returnType.IsVoid(compilation))
        {
            return true;
        }

        foreach (var allowableType in allowableTypes)
        {
            if (returnType.IsOfType(allowableType))
            {
                return false;
            }
        }

        return true;
    }

    private static bool HasIncorrectReturnType(this SemanticModel semanticModel, Compilation compilation,
        PropertyDeclarationSyntax propertyDeclarationSyntax, INamedTypeSymbol[] allowableTypes)
    {
        var propertySymbol = semanticModel.GetDeclaredSymbol(propertyDeclarationSyntax);
        if (propertySymbol is null)
        {
            return true;
        }

        if (propertySymbol.GetMethod is null)
        {
            return true;
        }

        var returnType = propertySymbol.GetMethod.ReturnType;
        if (returnType.IsVoid(compilation))
        {
            return true;
        }

        foreach (var allowableType in allowableTypes)
        {
            if (returnType.IsOfType(allowableType))
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>
    ///     Whether the class either has a <see cref="EntityNameAttribute" /> declaration
    /// </summary>
    private static bool HasEntityNameAttribute(this SemanticModel semanticModel, Compilation compilation,
        ClassDeclarationSyntax classDeclarationSyntax)
    {
        var attribute = classDeclarationSyntax.GetAttributeOfType<EntityNameAttribute>(semanticModel, compilation);
        return attribute.Exists();
    }

    /// <summary>
    ///     Whether the class implements the <see cref="IDehydratableEntity.Dehydrate" /> method
    /// </summary>
    private static bool ImplementsDehydrateMethod(this SemanticModel semanticModel, Compilation compilation,
        ClassDeclarationSyntax classDeclarationSyntax, IEnumerable<MethodDeclarationSyntax> allMethods)
    {
        var allowableTypes = GetAllowableDehydrateReturnType(semanticModel, compilation, classDeclarationSyntax);
        return allMethods
            .Any(method => method.IsPublicOverrideMethod()
                           && method.IsNamed(nameof(IDehydratableEntity.Dehydrate))
                           && !HasIncorrectReturnType(semanticModel, compilation, method, allowableTypes));
    }

    private static INamedTypeSymbol[] GetAllowableDehydrateReturnType(this SemanticModel semanticModel,
        Compilation compilation, ClassDeclarationSyntax classDeclarationSyntax)
    {
        var classSymbol = semanticModel.GetDeclaredSymbol(classDeclarationSyntax);
        if (classSymbol is null)
        {
            return Array.Empty<INamedTypeSymbol>();
        }

        var propertiesType = compilation.GetTypeByMetadataName(typeof(HydrationProperties).FullName!)!;
        return new[] { propertiesType };
    }

    /// <summary>
    ///     Whether the class implements the <see cref="IRehydratableObject.Rehydrate" /> method
    /// </summary>
    private static bool ImplementsAggregateRehydrateMethod(this SemanticModel semanticModel, Compilation compilation,
        ClassDeclarationSyntax classDeclarationSyntax, IEnumerable<MethodDeclarationSyntax> allMethods)
    {
        var allowableTypes =
            GetAllowableAggregateRehydrateReturnType(semanticModel, compilation, classDeclarationSyntax);
        return allMethods
            .Any(method => method.IsPublicStaticMethod()
                           && method.IsNamed(nameof(IRehydratableObject.Rehydrate))
                           && !semanticModel.HasIncorrectReturnType(compilation, method, allowableTypes));
    }

    /// <summary>
    ///     Whether the class implements the <see cref="IRehydratableObject.Rehydrate" /> method
    /// </summary>
    private static bool ImplementsEntityRehydrateMethod(this SemanticModel semanticModel, Compilation compilation,
        ClassDeclarationSyntax classDeclarationSyntax, IEnumerable<MethodDeclarationSyntax> allMethods)
    {
        var allowableTypes = GetAllowableEntityRehydrateReturnType(semanticModel, compilation, classDeclarationSyntax);
        return allMethods
            .Any(method => method.IsPublicStaticMethod()
                           && method.IsNamed(nameof(IRehydratableObject.Rehydrate))
                           && !semanticModel.HasIncorrectReturnType(compilation, method, allowableTypes));
    }

    /// <summary>
    ///     Whether the class implements the <see cref="IRehydratableObject.Rehydrate" /> method
    /// </summary>
    private static bool ImplementsValueObjectRehydrateMethod(this SemanticModel semanticModel, Compilation compilation,
        ClassDeclarationSyntax classDeclarationSyntax, IEnumerable<MethodDeclarationSyntax> allMethods)
    {
        var allowableTypes =
            GetAllowableValueObjectRehydrateReturnType(semanticModel, compilation, classDeclarationSyntax);
        return allMethods
            .Any(method => method.IsPublicStaticMethod()
                           && method.IsNamed(nameof(IRehydratableObject.Rehydrate))
                           && !semanticModel.HasIncorrectReturnType(compilation, method, allowableTypes));
    }

    private static INamedTypeSymbol[] GetAllowableAggregateRehydrateReturnType(this SemanticModel semanticModel,
        Compilation compilation, ClassDeclarationSyntax classDeclarationSyntax)
    {
        var classSymbol = semanticModel.GetDeclaredSymbol(classDeclarationSyntax);
        if (classSymbol is null)
        {
            return Array.Empty<INamedTypeSymbol>();
        }

        var factoryType = compilation.GetTypeByMetadataName(typeof(AggregateRootFactory<>).FullName!)!;
        var factoryOfClass = factoryType.Construct(classSymbol);

        return new[] { factoryOfClass };
    }

    private static INamedTypeSymbol[] GetAllowableEntityRehydrateReturnType(this SemanticModel semanticModel,
        Compilation compilation, ClassDeclarationSyntax classDeclarationSyntax)
    {
        var classSymbol = semanticModel.GetDeclaredSymbol(classDeclarationSyntax);
        if (classSymbol is null)
        {
            return Array.Empty<INamedTypeSymbol>();
        }

        var factoryType = compilation.GetTypeByMetadataName(typeof(EntityFactory<>).FullName!)!;
        var factoryOfClass = factoryType.Construct(classSymbol);

        return new[] { factoryOfClass };
    }

    private static INamedTypeSymbol[] GetAllowableValueObjectRehydrateReturnType(this SemanticModel semanticModel,
        Compilation compilation, ClassDeclarationSyntax classDeclarationSyntax)
    {
        var classSymbol = semanticModel.GetDeclaredSymbol(classDeclarationSyntax);
        if (classSymbol is null)
        {
            return Array.Empty<INamedTypeSymbol>();
        }

        var factoryType = compilation.GetTypeByMetadataName(typeof(ValueObjectFactory<>).FullName!)!;
        var factoryOfClass = factoryType.Construct(classSymbol);

        return new[] { factoryOfClass };
    }

    public class DehydratableStatus
    {
        private readonly Func<bool> _isDehydratable;

        public DehydratableStatus(bool implementsDehydrate, bool implementsRehydrate, bool markedWithEntityAttribute,
            Func<bool> isDehydratable)
        {
            _isDehydratable = isDehydratable;
            ImplementsDehydrate = implementsDehydrate;
            ImplementsRehydrate = implementsRehydrate;
            MarkedAsEntityName = markedWithEntityAttribute;
        }

        public bool ImplementsDehydrate { get; }

        public bool ImplementsRehydrate { get; }

        public bool IsDehydratable => _isDehydratable();

        public bool MarkedAsEntityName { get; }
    }
}