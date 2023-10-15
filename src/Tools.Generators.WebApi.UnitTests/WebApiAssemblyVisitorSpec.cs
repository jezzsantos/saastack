extern alias Generators;
using System.Collections.Immutable;
using System.Reflection;
using FluentAssertions;
using Generators::Infrastructure.WebApi.Interfaces;
using JetBrains.Annotations;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Moq;
using Xunit;
using WebApiAssemblyVisitor = Generators::Tools.Generators.WebApi.WebApiAssemblyVisitor;

namespace Tools.Generators.WebApi.UnitTests;

[UsedImplicitly]
public class WebApiAssemblyVisitorSpec
{
    private const string CompilationSourceCode = "";

    private static CSharpCompilation CreateCompilation(string sourceCode)
    {
        var assemblyPath = Path.GetDirectoryName(typeof(object).Assembly.Location)!;

        var compilation = CSharpCompilation.Create("compilation",
            new[]
            {
                CSharpSyntaxTree.ParseText(sourceCode)
            },
            new[]
            {
                MetadataReference.CreateFromFile(typeof(WebApiAssemblyVisitor).Assembly.Location),
                MetadataReference.CreateFromFile(typeof(Binder).GetTypeInfo().Assembly.Location),
                MetadataReference.CreateFromFile(Path.Combine(assemblyPath,
                    "System.Runtime.dll")) //HACK: this is required to make custom attributes work
            },
            new CSharpCompilationOptions(OutputKind.ConsoleApplication));

        return compilation;
    }

    [Trait("Category", "Unit")]
    public class GivenAnyClass
    {
        private readonly WebApiAssemblyVisitor _visitor;

        public GivenAnyClass()
        {
            var compilation = CreateCompilation(CompilationSourceCode);
            _visitor = new WebApiAssemblyVisitor(CancellationToken.None, compilation);
        }

        [Fact]
        public void WhenVisitAssembly_ThenVisitsGlobalNamespace()
        {
            var globalNamespace = new Mock<INamespaceSymbol>();
            var assembly = new Mock<IAssemblySymbol>();
            assembly.Setup(ass => ass.GlobalNamespace).Returns(globalNamespace.Object);
            _visitor.VisitAssembly(assembly.Object);

            globalNamespace.Verify(gns => gns.Accept(_visitor));
            _visitor.OperationRegistrations.Should().BeEmpty();
        }

        [Fact]
        public void WhenVisitNamespaceAndIgnoredNamespace_ThenStopsVisiting()
        {
            var @namespace = new Mock<INamespaceSymbol>();
            @namespace.Setup(ns => ns.Name).Returns(WebApiAssemblyVisitor.IgnoredNamespaces[0]);

            _visitor.VisitNamespace(@namespace.Object);

            @namespace.Verify(gns => gns.GetMembers(), Times.Never);
            _visitor.OperationRegistrations.Should().BeEmpty();
        }

        [Fact]
        public void WhenVisitNamespaceAndNoTypes_ThenStopsVisiting()
        {
            var @namespace = new Mock<INamespaceSymbol>();
            @namespace.Setup(ns => ns.Name).Returns("anamespace");
            @namespace.Setup(ns => ns.GetMembers()).Returns(new List<INamespaceOrTypeSymbol>());

            _visitor.VisitNamespace(@namespace.Object);

            @namespace.Verify(gns => gns.GetMembers());
            _visitor.OperationRegistrations.Should().BeEmpty();
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
            _visitor.OperationRegistrations.Should().BeEmpty();
        }

        [Fact]
        public void WhenVisitNamedTypeAndNotServiceClass_ThenStopsVisiting()
        {
            var type = new Mock<INamedTypeSymbol>();
            type.Setup(t => t.GetTypeMembers())
                .Returns(ImmutableArray<INamedTypeSymbol>.Empty);
            type.Setup(t => t.TypeKind).Returns(TypeKind.Interface);

            _visitor.VisitNamedType(type.Object);

            type.Verify(t => t.GetTypeMembers());
            _visitor.OperationRegistrations.Should().BeEmpty();
        }

        [Fact]
        public void WhenVisitNamedTypeAndIsClassButNotPublic_ThenCreatesNoRegistrations()
        {
            var type = new Mock<INamedTypeSymbol>();
            type.Setup(t => t.GetTypeMembers())
                .Returns(ImmutableArray<INamedTypeSymbol>.Empty);
            type.Setup(t => t.TypeKind).Returns(TypeKind.Class);
            type.Setup(t => t.DeclaredAccessibility).Returns(Accessibility.Private);

            _visitor.VisitNamedType(type.Object);

            type.Verify(t => t.GetTypeMembers());
            _visitor.OperationRegistrations.Should().BeEmpty();
        }

        [Fact]
        public void WhenVisitNamedTypeAndIsPublicClassButAlsoAbstract_ThenCreatesNoRegistrations()
        {
            var type = new Mock<INamedTypeSymbol>();
            type.Setup(t => t.GetTypeMembers()).Returns(ImmutableArray<INamedTypeSymbol>.Empty);
            type.Setup(t => t.TypeKind).Returns(TypeKind.Class);
            type.Setup(t => t.DeclaredAccessibility).Returns(Accessibility.Public);
            type.Setup(t => t.IsAbstract).Returns(true);

            _visitor.VisitNamedType(type.Object);

            type.Verify(t => t.GetTypeMembers());
            _visitor.OperationRegistrations.Should().BeEmpty();
        }

        [Fact]
        public void WhenVisitNamedTypeAndIsPublicClassButAlsoStatic_ThenCreatesNoRegistrations()
        {
            var type = new Mock<INamedTypeSymbol>();
            type.Setup(t => t.GetTypeMembers()).Returns(ImmutableArray<INamedTypeSymbol>.Empty);
            type.Setup(t => t.TypeKind).Returns(TypeKind.Class);
            type.Setup(t => t.DeclaredAccessibility).Returns(Accessibility.Public);
            type.Setup(t => t.IsStatic).Returns(true);

            _visitor.VisitNamedType(type.Object);

            type.Verify(t => t.GetTypeMembers());
            _visitor.OperationRegistrations.Should().BeEmpty();
        }

        [Fact]
        public void WhenVisitNamedTypeAndIsPublicClassButNotAnyBaseType_ThenCreatesNoRegistrations()
        {
            var type = new Mock<INamedTypeSymbol>();
            type.Setup(t => t.GetTypeMembers()).Returns(ImmutableArray<INamedTypeSymbol>.Empty);
            type.Setup(t => t.TypeKind).Returns(TypeKind.Class);
            type.Setup(t => t.DeclaredAccessibility).Returns(Accessibility.Public);
            type.Setup(t => t.IsStatic).Returns(false);
            type.Setup(t => t.IsAbstract).Returns(false);
            type.Setup(t => t.AllInterfaces).Returns(ImmutableArray<INamedTypeSymbol>.Empty);

            _visitor.VisitNamedType(type.Object);

            type.Verify(t => t.GetTypeMembers());
            _visitor.OperationRegistrations.Should().BeEmpty();
        }

        [Fact]
        public void WhenVisitNamedTypeAndIsPublicClassButWrongBaseType_ThenCreatesNoRegistrations()
        {
            var type = new Mock<INamedTypeSymbol>();
            type.Setup(t => t.GetTypeMembers()).Returns(ImmutableArray<INamedTypeSymbol>.Empty);
            type.Setup(t => t.TypeKind).Returns(TypeKind.Class);
            type.Setup(t => t.DeclaredAccessibility).Returns(Accessibility.Public);
            var classBaseType = new Mock<INamedTypeSymbol>();
            type.Setup(t => t.IsStatic).Returns(false);
            type.Setup(t => t.IsAbstract).Returns(false);
            type.Setup(t => t.AllInterfaces).Returns(ImmutableArray.Create(classBaseType.Object));

            _visitor.VisitNamedType(type.Object);

            type.Verify(t => t.GetTypeMembers());
            _visitor.OperationRegistrations.Should().BeEmpty();
        }
    }

    [Trait("Category", "Unit")]
    public class GivenAServiceClass
    {
        private readonly CSharpCompilation _compilation;
        private readonly WebApiAssemblyVisitor _visitor;

        public GivenAServiceClass()
        {
            _compilation = CreateCompilation(CompilationSourceCode);
            _visitor = new WebApiAssemblyVisitor(CancellationToken.None, _compilation);
        }

        [Fact]
        public void WhenVisitNamedTypeAndNoMethods_ThenCreatesNoRegistrations()
        {
            var type = SetupServiceClass(_compilation);

            _visitor.VisitNamedType(type.Object);

            type.Verify(t => t.GetTypeMembers());
            _visitor.OperationRegistrations.Should().BeEmpty();
        }

        [Fact]
        public void WhenVisitNamedTypeAndOnlyPrivateMethod_ThenCreatesNoRegistrations()
        {
            var type = SetupServiceClass(_compilation);
            var method = new Mock<IMethodSymbol>();
            method.Setup(m => m.DeclaredAccessibility).Returns(Accessibility.Private);

            _visitor.VisitNamedType(type.Object);

            type.Verify(t => t.GetTypeMembers());
            _visitor.OperationRegistrations.Should().BeEmpty();
        }

        [Fact]
        public void WhenVisitNamedTypeAndOnlyPublicStaticMethod_ThenCreatesNoRegistrations()
        {
            var type = SetupServiceClass(_compilation);
            var method = new Mock<IMethodSymbol>();
            method.Setup(m => m.DeclaredAccessibility).Returns(Accessibility.Public);
            method.Setup(m => m.IsStatic).Returns(true);

            _visitor.VisitNamedType(type.Object);

            type.Verify(t => t.GetTypeMembers());
            _visitor.OperationRegistrations.Should().BeEmpty();
        }

        [Fact]
        public void WhenVisitNamedTypeAndVoidReturnType_ThenCreatesNoRegistrations()
        {
            var voidMetadata = _compilation.GetTypeByMetadataName(typeof(void).FullName!)!;
            var type = SetupServiceClass(_compilation);
            var method = new Mock<IMethodSymbol>();
            method.Setup(m => m.DeclaredAccessibility).Returns(Accessibility.Public);
            method.Setup(m => m.IsStatic).Returns(false);
            method.Setup(m => m.ReturnType).Returns(voidMetadata);
            type.Setup(t => t.GetMembers()).Returns(ImmutableArray.Create<ISymbol>(method.Object));

            _visitor.VisitNamedType(type.Object);

            type.Verify(t => t.GetTypeMembers());
            _visitor.OperationRegistrations.Should().BeEmpty();
        }

        [Fact]
        public void WhenVisitNamedTypeAndHasNoParameters_ThenCreatesNoRegistrations()
        {
            var taskMetadata = _compilation.GetTypeByMetadataName(typeof(Task<>).FullName!)!;
            var type = SetupServiceClass(_compilation);
            var method = new Mock<IMethodSymbol>();
            method.Setup(m => m.DeclaredAccessibility).Returns(Accessibility.Public);
            method.Setup(m => m.IsStatic).Returns(false);
            method.Setup(m => m.ReturnType).Returns(taskMetadata);
            method.Setup(m => m.Parameters).Returns(ImmutableArray.Create<IParameterSymbol>());
            type.Setup(t => t.GetMembers()).Returns(ImmutableArray.Create<ISymbol>(method.Object));

            _visitor.VisitNamedType(type.Object);

            type.Verify(t => t.GetTypeMembers());
            _visitor.OperationRegistrations.Should().BeEmpty();
        }

        [Fact]
        public void WhenVisitNamedTypeAndHasWrongFirstParameter_ThenCreatesNoRegistrations()
        {
            var taskMetadata = _compilation.GetTypeByMetadataName(typeof(Task<>).FullName!)!;
            var type = SetupServiceClass(_compilation);
            var method = new Mock<IMethodSymbol>();
            method.Setup(m => m.DeclaredAccessibility).Returns(Accessibility.Public);
            method.Setup(m => m.IsStatic).Returns(false);
            method.Setup(m => m.ReturnType).Returns(taskMetadata);
            var parameter = new Mock<IParameterSymbol>();
            var classBaseType = new Mock<INamedTypeSymbol>();
            parameter.Setup(p => p.Type.AllInterfaces).Returns(ImmutableArray.Create(classBaseType.Object));
            method.Setup(m => m.Parameters).Returns(ImmutableArray.Create(parameter.Object));
            type.Setup(t => t.GetMembers()).Returns(ImmutableArray.Create<ISymbol>(method.Object));

            _visitor.VisitNamedType(type.Object);

            type.Verify(t => t.GetTypeMembers());
            _visitor.OperationRegistrations.Should().BeEmpty();
        }

        [Fact]
        public void WhenVisitNamedTypeAndHasWrongSecondParameter_ThenCreatesNoRegistrations()
        {
            var requestMetadata = _compilation.GetTypeByMetadataName(typeof(IWebRequest).FullName!)!;
            var stringMetadata = _compilation.GetTypeByMetadataName(typeof(string).FullName!)!;
            var taskMetadata = _compilation.GetTypeByMetadataName(typeof(Task<>).FullName!)!;
            var type = SetupServiceClass(_compilation);
            var method = new Mock<IMethodSymbol>();
            method.Setup(m => m.DeclaredAccessibility).Returns(Accessibility.Public);
            method.Setup(m => m.IsStatic).Returns(false);
            method.Setup(m => m.ReturnType).Returns(taskMetadata);
            var firstParameter = new Mock<IParameterSymbol>();
            firstParameter.Setup(p => p.Type.AllInterfaces).Returns(ImmutableArray.Create(requestMetadata));
            var secondParameter = new Mock<IParameterSymbol>();
            secondParameter.Setup(p => p.Type).Returns(stringMetadata);
            method.Setup(m => m.Parameters)
                .Returns(ImmutableArray.Create(firstParameter.Object, secondParameter.Object));
            type.Setup(t => t.GetMembers()).Returns(ImmutableArray.Create<ISymbol>(method.Object));

            _visitor.VisitNamedType(type.Object);

            type.Verify(t => t.GetTypeMembers());
            _visitor.OperationRegistrations.Should().BeEmpty();
        }

        [Fact]
        public void WhenVisitNamedTypeAndHasNoAttributes_ThenCreatesNoRegistrations()
        {
            var requestMetadata = _compilation.GetTypeByMetadataName(typeof(IWebRequest).FullName!)!;
            var cancellationTokenMetadata = _compilation.GetTypeByMetadataName(typeof(CancellationToken).FullName!)!;
            var taskMetadata = _compilation.GetTypeByMetadataName(typeof(Task<>).FullName!)!;
            var type = SetupServiceClass(_compilation);
            var method = new Mock<IMethodSymbol>();
            method.Setup(m => m.DeclaredAccessibility).Returns(Accessibility.Public);
            method.Setup(m => m.IsStatic).Returns(false);
            method.Setup(m => m.ReturnType).Returns(taskMetadata);
            var firstParameter = new Mock<IParameterSymbol>();
            firstParameter.Setup(p => p.Type.GetAttributes()).Returns(ImmutableArray.Create<AttributeData>());
            firstParameter.Setup(p => p.Type.AllInterfaces).Returns(ImmutableArray.Create(requestMetadata));
            var secondParameter = new Mock<IParameterSymbol>();
            secondParameter.Setup(p => p.Type).Returns(cancellationTokenMetadata);
            method.Setup(m => m.Parameters)
                .Returns(ImmutableArray.Create(firstParameter.Object, secondParameter.Object));
            type.Setup(t => t.GetMembers()).Returns(ImmutableArray.Create<ISymbol>(method.Object));

            _visitor.VisitNamedType(type.Object);

            type.Verify(t => t.GetTypeMembers());
            _visitor.OperationRegistrations.Should().BeEmpty();
        }

        [Trait("Category", "Unit")]
        public class GivenAServiceOperation
        {
            [Fact]
            public void WhenVisitNamedTypeAndHasAttribute_ThenCreatesRegistration()
            {
                var compilation = CreateCompilation("""
                                                    using System;
                                                    using Infrastructure.WebApi.Interfaces;

                                                    namespace ANamespace;

                                                    public class AResponse : IWebResponse
                                                    {
                                                    }
                                                    [Route("aroute", ServiceOperation.Get)]
                                                    public class ARequest : IWebRequest<AResponse>
                                                    {
                                                    }
                                                    public class AServiceClass : Infrastructure.WebApi.Interfaces.IWebApiService
                                                    {
                                                        public string AMethod(ARequest request)
                                                        {
                                                             return "";
                                                        }
                                                    }
                                                    """);

                var serviceClass = compilation.GetTypeByMetadataName("ANamespace.AServiceClass")!;
                var visitor = new WebApiAssemblyVisitor(CancellationToken.None, compilation);

                visitor.VisitNamedType(serviceClass);

                visitor.OperationRegistrations.Count.Should().Be(1);
                var registration = visitor.OperationRegistrations.First();
                registration.Class.Constructors.Count().Should().Be(1);
                registration.Class.Constructors.First().CtorParameters.Count().Should().Be(0);
                registration.Class.Constructors.First().IsInjectionCtor.Should().BeFalse();
                registration.Class.Constructors.First().MethodBody.Should().BeEmpty();
                registration.Class.TypeName.Name.Should().Be("AServiceClass");
                registration.Class.TypeName.Namespace.Should().Be("ANamespace");
                registration.Class.TypeName.FullName.Should().Be("ANamespace.AServiceClass");
                registration.Class.UsingNamespaces.Count().Should().Be(2);
                registration.MethodBody.Should().Be($"    {{{Environment.NewLine}"
                                                    + $"         return \"\";{Environment.NewLine}"
                                                    + $"    }}{Environment.NewLine}");
                registration.MethodName.Should().Be("AMethod");
                registration.OperationType.Should().Be(ServiceOperation.Get);
                registration.RoutePath.Should().Be("aroute");
                registration.IsTestingOnly.Should().BeFalse();
                registration.RequestDtoType.Name.Should().Be("ARequest");
                registration.RequestDtoType.Namespace.Should().Be("ANamespace");
                registration.ResponseDtoType.Name.Should().Be("AResponse");
                registration.ResponseDtoType.Namespace.Should().Be("ANamespace");
            }
        }

        private static Mock<INamedTypeSymbol> SetupServiceClass(CSharpCompilation compilation)
        {
            var serviceClassMetadata = compilation.GetTypeByMetadataName(typeof(IWebApiService).FullName!)!;
            var type = new Mock<INamedTypeSymbol>();
            type.Setup(t => t.GetTypeMembers()).Returns(ImmutableArray<INamedTypeSymbol>.Empty);
            type.Setup(t => t.TypeKind).Returns(TypeKind.Class);
            type.Setup(t => t.DeclaredAccessibility).Returns(Accessibility.Public);
            type.Setup(t => t.IsStatic).Returns(false);
            type.Setup(t => t.IsAbstract).Returns(false);
            type.Setup(t => t.AllInterfaces).Returns(ImmutableArray.Create(serviceClassMetadata));
            var @namespace = new Mock<INamespaceSymbol>();
            @namespace.As<ISymbol>().Setup(ns => ns.ToDisplayString(It.IsAny<SymbolDisplayFormat?>()))
                .Returns("adisplaystring");
            type.Setup(t => t.ContainingNamespace).Returns(@namespace.Object);
            type.Setup(t => t.Name).Returns("aname");

            return type;
        }
    }
}