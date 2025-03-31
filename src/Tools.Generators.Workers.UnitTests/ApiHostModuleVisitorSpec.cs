extern alias Generators;
using System.Collections.Immutable;
using System.Reflection;
using FluentAssertions;
using Generators::Infrastructure.Eventing.Interfaces.Notifications;
using Generators::JetBrains.Annotations;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Moq;
using Xunit;
using ApiHostModuleVisitor = Generators::Tools.Generators.Workers.ApiHostModuleVisitor;

namespace Tools.Generators.Workers.UnitTests;

extern alias Generators;

[UsedImplicitly]
public class ApiHostModuleVisitorSpec
{
    private const string CompilationSourceCode = "";
    private static readonly string[] AdditionalCompilationAssemblies = [];

    private static CSharpCompilation CreateCompilation(string sourceCode)
    {
        var assemblyPath = Path.GetDirectoryName(typeof(object).Assembly.Location)!;
        var references = new List<MetadataReference>
        {
            MetadataReference.CreateFromFile(typeof(ApiHostModuleVisitor).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(Binder).GetTypeInfo().Assembly.Location)
        };
        AdditionalCompilationAssemblies.ToList()
            .ForEach(item => references.Add(MetadataReference.CreateFromFile(Path.Combine(assemblyPath, item))));
        var compilation = CSharpCompilation.Create("compilation",
            [
                CSharpSyntaxTree.ParseText(sourceCode)
            ],
            references,
            new CSharpCompilationOptions(OutputKind.ConsoleApplication));

        return compilation;
    }

    [Trait("Category", "Unit.Tooling")]
    public class GivenAnyClass
    {
        private readonly ApiHostModuleVisitor _visitor;

        public GivenAnyClass()
        {
            var compilation = CreateCompilation(CompilationSourceCode);
            _visitor = new ApiHostModuleVisitor(compilation, CancellationToken.None);
        }

        [Fact]
        public void WhenVisitAssembly_ThenVisitsGlobalNamespace()
        {
            var globalNamespace = new Mock<INamespaceSymbol>();
            var assembly = new Mock<IAssemblySymbol>();
            assembly.Setup(ass => ass.GlobalNamespace).Returns(globalNamespace.Object);

            _visitor.VisitAssembly(assembly.Object);

            globalNamespace.Verify(gns => gns.Accept(_visitor));
            _visitor.ConsumerTypes.Should().BeEmpty();
        }

        [Fact]
        public void WhenVisitModule_ThenVisitsAllReferencedAssemblies()
        {
            var module = new Mock<IModuleSymbol>();
            var assembly = new Mock<IAssemblySymbol>();
            module.Setup(mod => mod.ReferencedAssemblySymbols)
                .Returns([assembly.Object]);

            _visitor.VisitModule(module.Object);

            assembly.Verify(ass => ass.Accept(_visitor));
            _visitor.ConsumerTypes.Should().BeEmpty();
        }

        [Fact]
        public void WhenVisitNamespaceAndNoTypes_ThenStopsVisiting()
        {
            var @namespace = new Mock<INamespaceSymbol>();
            @namespace.Setup(ns => ns.Name).Returns("anamespace");
            @namespace.Setup(ns => ns.GetMembers()).Returns(new List<INamespaceOrTypeSymbol>());

            _visitor.VisitNamespace(@namespace.Object);

            @namespace.Verify(gns => gns.GetMembers());
            _visitor.ConsumerTypes.Should().BeEmpty();
        }

        [Fact]
        public void WhenVisitNamespaceThatHasTypes_ThenVisitsTypes()
        {
            var @namespace = new Mock<INamespaceSymbol>();
            @namespace.Setup(ns => ns.Name).Returns("anamespace");
            var type = new Mock<INamespaceOrTypeSymbol>();
            @namespace.Setup(ns => ns.GetMembers()).Returns(new List<INamespaceOrTypeSymbol>
            {
                type.Object
            });

            _visitor.VisitNamespace(@namespace.Object);

            @namespace.Verify(gns => gns.GetMembers());
            type.Verify(t => t.Accept(_visitor));
            _visitor.ConsumerTypes.Should().BeEmpty();
        }

        [Fact]
        public void WhenVisitNamedTypeAndNotNotificationConsumer_ThenStopsVisiting()
        {
            var type = new Mock<INamedTypeSymbol>();
            type.Setup(t => t.GetTypeMembers())
                .Returns(ImmutableArray<INamedTypeSymbol>.Empty);
            type.Setup(t => t.TypeKind).Returns(TypeKind.Interface);

            _visitor.VisitNamedType(type.Object);

            _visitor.ConsumerTypes.Should().BeEmpty();
        }

        [Fact]
        public void WhenVisitNamedTypeAndIsClassButNotPublic_ThenCreatesNoConsumers()
        {
            var type = new Mock<INamedTypeSymbol>();
            type.Setup(t => t.GetTypeMembers())
                .Returns(ImmutableArray<INamedTypeSymbol>.Empty);
            type.Setup(t => t.TypeKind).Returns(TypeKind.Class);
            type.Setup(t => t.DeclaredAccessibility).Returns(Accessibility.Private);

            _visitor.VisitNamedType(type.Object);

            _visitor.ConsumerTypes.Should().BeEmpty();
        }

        [Fact]
        public void WhenVisitNamedTypeAndIsPublicClassButAlsoAbstract_ThenCreatesNoConsumers()
        {
            var type = new Mock<INamedTypeSymbol>();
            type.Setup(t => t.GetTypeMembers()).Returns(ImmutableArray<INamedTypeSymbol>.Empty);
            type.Setup(t => t.TypeKind).Returns(TypeKind.Class);
            type.Setup(t => t.DeclaredAccessibility).Returns(Accessibility.Public);
            type.Setup(t => t.IsAbstract).Returns(true);

            _visitor.VisitNamedType(type.Object);

            _visitor.ConsumerTypes.Should().BeEmpty();
        }

        [Fact]
        public void WhenVisitNamedTypeAndIsPublicClassButAlsoStatic_ThenCreatesNoConsumers()
        {
            var type = new Mock<INamedTypeSymbol>();
            type.Setup(t => t.GetTypeMembers()).Returns(ImmutableArray<INamedTypeSymbol>.Empty);
            type.Setup(t => t.TypeKind).Returns(TypeKind.Class);
            type.Setup(t => t.DeclaredAccessibility).Returns(Accessibility.Public);
            type.Setup(t => t.IsStatic).Returns(true);

            _visitor.VisitNamedType(type.Object);

            _visitor.ConsumerTypes.Should().BeEmpty();
        }

        [Fact]
        public void WhenVisitNamedTypeAndIsPublicClassButNotAnyBaseType_ThenCreatesNoConsumers()
        {
            var type = new Mock<INamedTypeSymbol>();
            type.Setup(t => t.GetTypeMembers()).Returns(ImmutableArray<INamedTypeSymbol>.Empty);
            type.Setup(t => t.TypeKind).Returns(TypeKind.Class);
            type.Setup(t => t.DeclaredAccessibility).Returns(Accessibility.Public);
            type.Setup(t => t.IsStatic).Returns(false);
            type.Setup(t => t.IsAbstract).Returns(false);
            type.Setup(t => t.AllInterfaces).Returns(ImmutableArray<INamedTypeSymbol>.Empty);

            _visitor.VisitNamedType(type.Object);

            _visitor.ConsumerTypes.Should().BeEmpty();
        }

        [Fact]
        public void WhenVisitNamedTypeAndIsPublicClassButWrongBaseType_ThenCreatesNoConsumers()
        {
            var type = new Mock<INamedTypeSymbol>();
            type.Setup(t => t.GetTypeMembers()).Returns(ImmutableArray<INamedTypeSymbol>.Empty);
            type.Setup(t => t.TypeKind).Returns(TypeKind.Class);
            type.Setup(t => t.DeclaredAccessibility).Returns(Accessibility.Public);
            var classBaseType = new Mock<INamedTypeSymbol>();
            type.Setup(t => t.IsStatic).Returns(false);
            type.Setup(t => t.IsAbstract).Returns(false);
            type.Setup(t => t.AllInterfaces).Returns([classBaseType.Object]);

            _visitor.VisitNamedType(type.Object);

            _visitor.ConsumerTypes.Should().BeEmpty();
        }
    }

    [Trait("Category", "Unit.Tooling")]
    public class GivenAConsumerNotificationClass
    {
        private readonly CSharpCompilation _compilation;
        private readonly ApiHostModuleVisitor _visitor;

        public GivenAConsumerNotificationClass()
        {
            _compilation = CreateCompilation(CompilationSourceCode);
            _visitor = new ApiHostModuleVisitor(_compilation, CancellationToken.None);
        }

        [Fact]
        public void WhenVisitNamedType_ThenCreatesConsumer()
        {
            var type = SetupNotificationConsumerClass(_compilation);

            _visitor.VisitNamedType(type.Object);

            _visitor.ConsumerTypes.Should().Contain(consumer =>
                consumer.Name.Namespace == "adisplaystring" && consumer.Name.Name == "aname");
        }

        private static Mock<INamedTypeSymbol> SetupNotificationConsumerClass(CSharpCompilation compilation)
        {
            var consumerClassMetadata =
                compilation.GetTypeByMetadataName(typeof(IDomainEventNotificationConsumer).FullName!)!;
            var type = new Mock<INamedTypeSymbol>();
            type.Setup(t => t.GetTypeMembers()).Returns(ImmutableArray<INamedTypeSymbol>.Empty);
            type.Setup(t => t.TypeKind).Returns(TypeKind.Class);
            type.Setup(t => t.DeclaredAccessibility).Returns(Accessibility.Public);
            type.Setup(t => t.IsStatic).Returns(false);
            type.Setup(t => t.IsAbstract).Returns(false);
            type.Setup(t => t.AllInterfaces).Returns([consumerClassMetadata]);
            var @namespace = new Mock<INamespaceSymbol>();
            @namespace.As<ISymbol>().Setup(ns => ns.ToDisplayString(It.IsAny<SymbolDisplayFormat?>()))
                .Returns("adisplaystring");
            type.Setup(t => t.ContainingNamespace).Returns(@namespace.Object);
            type.Setup(t => t.Name).Returns("aname");
            type.Setup(t => t.GetAttributes()).Returns(ImmutableArray<AttributeData>.Empty);

            return type;
        }
    }
}