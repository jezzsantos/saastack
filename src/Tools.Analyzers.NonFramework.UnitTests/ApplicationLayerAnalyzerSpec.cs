extern alias NonFrameworkAnalyzers;
extern alias CommonAnalyzers;
using CommonAnalyzers::Tools.Analyzers.Common;
using Xunit;
using ApplicationLayerAnalyzer = NonFrameworkAnalyzers::Tools.Analyzers.NonFramework.ApplicationLayerAnalyzer;
using NonFrameworkAnalyzers::JetBrains.Annotations;

namespace Tools.Analyzers.NonFramework.UnitTests;

[UsedImplicitly]
public class ApplicationLayerAnalyzerSpec
{
    [UsedImplicitly]
    public class GivenAResource
    {
        [Trait("Category", "Unit.Tooling")]
        public class GivenAnyResource
        {
            [Fact]
            public async Task WhenComplete_ThenNoAlert()
            {
                const string input = @"
using System;
using Application.Interfaces.Resources;
namespace ANamespace;
public class AClass : IIdentifiableResource
{
    public required string Id { get; set; }
}";

                await Verify.NoDiagnosticExists<ApplicationLayerAnalyzer>(input);
            }
        }

        [Trait("Category", "Unit.Tooling")]
        public class GivenRule010
        {
            [Fact]
            public async Task WhenIsNotPublic_ThenAlerts()
            {
                const string input = @"
using System;
using Application.Interfaces.Resources;
namespace ANamespace;
internal class AClass : IIdentifiableResource
{
    public required string Id { get; set; }
}";

                await Verify.DiagnosticExists<ApplicationLayerAnalyzer>(
                    ApplicationLayerAnalyzer.Rule010, input, 5, 16, "AClass");
            }
        }

        [Trait("Category", "Unit.Tooling")]
        public class GivenRule011
        {
            [Fact]
            public async Task WhenHasCtorAndNotParameterless_ThenAlerts()
            {
                const string input = @"
using System;
using Application.Interfaces.Resources;
namespace ANamespace;
public class AClass : IIdentifiableResource
{
    public AClass(string id)
    {
        Id = id;
    }

    public required string Id { get; set; }
}";

                await Verify.DiagnosticExists<ApplicationLayerAnalyzer>(
                    ApplicationLayerAnalyzer.Rule011, input, 5, 14, "AClass");
            }

            [Fact]
            public async Task WhenHasCtorAndPrivate_ThenAlerts()
            {
                const string input = @"
using System;
using Application.Interfaces.Resources;
namespace ANamespace;
public class AClass : IIdentifiableResource
{
    private AClass()
    {
        Id = string.Empty;
    }

    public required string Id { get; set; }
}";

                await Verify.DiagnosticExists<ApplicationLayerAnalyzer>(
                    ApplicationLayerAnalyzer.Rule011, input, 5, 14, "AClass");
            }

            [Fact]
            public async Task WhenHasCtorAndIsParameterless_ThenNoAlert()
            {
                const string input = @"
using System;
using Application.Interfaces.Resources;
namespace ANamespace;
public class AClass : IIdentifiableResource
{
    public AClass()
    {
        Id = string.Empty;
    }

    public required string Id { get; set; }
}";

                await Verify.NoDiagnosticExists<ApplicationLayerAnalyzer>(input);
            }
        }

        [Trait("Category", "Unit.Tooling")]
        public class GivenRule012
        {
            [Fact]
            public async Task WhenAnyPropertyHasNoSetter_ThenAlerts()
            {
                const string input = @"
using System;
using Application.Interfaces.Resources;
namespace ANamespace;
public class AClass : IIdentifiableResource
{
    public required string Id { get; set; }

    public string? AProperty { get; }
}";

                await Verify.DiagnosticExists<ApplicationLayerAnalyzer>(
                    ApplicationLayerAnalyzer.Rule012, input, 9, 20, "AProperty");
            }
        }

        [Trait("Category", "Unit.Tooling")]
        public class GivenRule013
        {
            [Fact]
            public async Task WhenAnyPropertyReferenceTypeIsOptional_ThenAlerts()
            {
                const string input = @"
using System;
using Application.Interfaces.Resources;
using Common;
namespace ANamespace;
public class AClass : IIdentifiableResource
{
    public required string Id { get; set; }

    public Optional<string> AProperty { get; set; }
}";

                await Verify.DiagnosticExists<ApplicationLayerAnalyzer>(input,
                    (ApplicationLayerAnalyzer.Rule013, 10, 29, "AProperty", null),
                    (ApplicationLayerAnalyzer.Rule014, 10, 29, "AProperty", [
                        GivenRule014.AllTypes,
                        AnalyzerConstants.ResourceTypesNamespace
                    ]));
            }

            [Fact]
            public async Task WhenAnyPropertyReferenceTypeIsNullable_ThenNoAlert()
            {
                const string input = @"
using System;
using Application.Interfaces.Resources;
namespace ANamespace;
public class AClass : IIdentifiableResource
{
    public required string Id { get; set; }

    public string? AProperty { get; set; }
}";

                await Verify.NoDiagnosticExists<ApplicationLayerAnalyzer>(input);
            }
        }

        [Trait("Category", "Unit.Tooling")]
        public class GivenRule014
        {
            public const string AllTypes =
                "bool or string or ulong or int or long or double or decimal or System.DateTime or byte or System.IO.Stream";

            [Fact]
            public async Task WhenAnyPropertyIsNotSupportedPrimitive_ThenAlerts()
            {
                const string input = @"
using System;
using System.Collections.Generic;
using Application.Interfaces.Resources;
namespace ANamespace;
public class AClass : IIdentifiableResource
{
    public required string Id { get; set; }

    public required char AProperty { get; set; }
}";

                await Verify.DiagnosticExists<ApplicationLayerAnalyzer>(
                    ApplicationLayerAnalyzer.Rule014, input, 10, 26, "AProperty", AllTypes,
                    AnalyzerConstants.ResourceTypesNamespace);
            }

            [Fact]
            public async Task WhenAnyPropertyIsNotSupportedListOfPrimitive_ThenAlerts()
            {
                const string input = @"
using System;
using System.Collections.Generic;
using Application.Interfaces.Resources;
namespace ANamespace;
public class AClass : IIdentifiableResource
{
    public required string Id { get; set; }

    public required List<char> AProperty { get; set; }
}";

                await Verify.DiagnosticExists<ApplicationLayerAnalyzer>(
                    ApplicationLayerAnalyzer.Rule014, input, 10, 32, "AProperty", AllTypes,
                    AnalyzerConstants.ResourceTypesNamespace);
            }

            [Fact]
            public async Task WhenAnyPropertyIsNotSupportedDictionaryOfPrimitive_ThenAlerts()
            {
                const string input = @"
using System;
using System.Collections.Generic;
using Application.Interfaces.Resources;
namespace ANamespace;
public class AClass : IIdentifiableResource
{
    public required string Id { get; set; }

    public required Dictionary<string, char> AProperty { get; set; }
}";

                await Verify.DiagnosticExists<ApplicationLayerAnalyzer>(
                    ApplicationLayerAnalyzer.Rule014, input, 10, 46, "AProperty", AllTypes,
                    AnalyzerConstants.ResourceTypesNamespace);
            }

            [Fact]
            public async Task WhenAnyPropertyIsNotSupportedDictionaryKeyType_ThenAlerts()
            {
                const string input = @"
using System;
using System.Collections.Generic;
using Application.Interfaces.Resources;
namespace ANamespace;
public class AClass : IIdentifiableResource
{
    public required string Id { get; set; }

    public required Dictionary<char, string> AProperty { get; set; }
}";

                await Verify.DiagnosticExists<ApplicationLayerAnalyzer>(
                    ApplicationLayerAnalyzer.Rule014, input, 10, 46, "AProperty", AllTypes,
                    AnalyzerConstants.ResourceTypesNamespace);
            }

            [Fact]
            public async Task WhenAnyPropertyIsSupportedPrimitive_ThenNoAlert()
            {
                const string input = @"
using System;
using System.Collections.Generic;
using Application.Interfaces.Resources;
namespace ANamespace;
public class AClass : IIdentifiableResource
{
    public required string Id { get; set; }

    public required string AProperty { get; set; }
}";

                await Verify.NoDiagnosticExists<ApplicationLayerAnalyzer>(input);
            }

            [Fact]
            public async Task WhenAnyPropertyIsSupportedNullablePrimitive_ThenNoAlert()
            {
                const string input = @"
using System;
using System.Collections.Generic;
using Application.Interfaces.Resources;
namespace ANamespace;
public class AClass : IIdentifiableResource
{
    public required string Id { get; set; }

    public string? AProperty { get; set; }
}";

                await Verify.NoDiagnosticExists<ApplicationLayerAnalyzer>(input);
            }

            [Fact]
            public async Task WhenAnyPropertyIsStream_ThenNoAlert()
            {
                const string input = @"
using System;
using System.Collections.Generic;
using System.IO;
using Application.Interfaces.Resources;
namespace ANamespace;
public class AClass : IIdentifiableResource
{
    public required string Id { get; set; }

    public required Stream AProperty { get; set; }
}";

                await Verify.NoDiagnosticExists<ApplicationLayerAnalyzer>(input);
            }

            [Fact]
            public async Task WhenAnyPropertyIsNullableStream_ThenNoAlert()
            {
                const string input = @"
using System;
using System.Collections.Generic;
using System.IO;
using Application.Interfaces.Resources;
namespace ANamespace;
public class AClass : IIdentifiableResource
{
    public required string Id { get; set; }

    public Stream? AProperty { get; set; }
}";

                await Verify.NoDiagnosticExists<ApplicationLayerAnalyzer>(input);
            }

            [Fact]
            public async Task WhenAnyPropertyIsClassInCorrectNamespace_ThenNoAlert()
            {
                const string input = @"
using System;
using System.Collections.Generic;
using Application.Interfaces.Resources;
using Application.Resources.Shared;
namespace ANamespace
{
    public class AClass : IIdentifiableResource
    {
        public required string Id { get; set; }

        public required AnotherClass AProperty { get; set; }
    }
}
namespace Application.Resources.Shared
{
    public class AnotherClass
    {
    }
}";

                await Verify.NoDiagnosticExists<ApplicationLayerAnalyzer>(input);
            }

            [Fact]
            public async Task WhenAnyPropertyIsNullableClassInCorrectNamespace_ThenNoAlert()
            {
                const string input = @"
using System;
using System.Collections.Generic;
using Application.Interfaces.Resources;
using Application.Resources.Shared;
namespace ANamespace
{
    public class AClass : IIdentifiableResource
    {
        public required string Id { get; set; }

        public AnotherClass? AProperty { get; set; }
    }
}
namespace Application.Resources.Shared
{
    public class AnotherClass
    {
    }
}";

                await Verify.NoDiagnosticExists<ApplicationLayerAnalyzer>(input);
            }

            [Fact]
            public async Task WhenAnyPropertyIsClassInOtherNamespace_ThenAlerts()
            {
                const string input = @"
using System;
using System.Collections.Generic;
using Application.Interfaces.Resources;
using AnotherNamespace;
namespace ANamespace
{
    public class AClass : IIdentifiableResource
    {
        public required string Id { get; set; }

        public required AnotherClass AProperty { get; set; }
    }
}
namespace AnotherNamespace
{
    public enum AnotherClass
    {
    }
}";

                await Verify.DiagnosticExists<ApplicationLayerAnalyzer>(
                    ApplicationLayerAnalyzer.Rule014, input, 12, 38, "AProperty", AllTypes,
                    AnalyzerConstants.ResourceTypesNamespace);
            }

            [Fact]
            public async Task WhenAnyPropertyIsEnumInCorrectNamespace_ThenNoAlert()
            {
                const string input = @"
using System;
using System.Collections.Generic;
using Application.Interfaces.Resources;
using Application.Resources.Shared;
namespace ANamespace
{
    public class AClass : IIdentifiableResource
    {
        public required string Id { get; set; }

        public required AnEnum AProperty { get; set; }
    }
}
namespace Application.Resources.Shared
{
    public enum AnEnum
    {
        AValue
    }
}";

                await Verify.NoDiagnosticExists<ApplicationLayerAnalyzer>(input);
            }

            [Fact]
            public async Task WhenAnyPropertyIsNullableEnumInCorrectNamespace_ThenNoAlert()
            {
                const string input = @"
using System;
using System.Collections.Generic;
using Application.Interfaces.Resources;
using Application.Resources.Shared;
namespace ANamespace
{
    public class AClass : IIdentifiableResource
    {
        public required string Id { get; set; }

        public AnEnum? AProperty { get; set; }
    }
}
namespace Application.Resources.Shared
{
    public enum AnEnum
    {
        AValue
    }
}";

                await Verify.NoDiagnosticExists<ApplicationLayerAnalyzer>(input);
            }

            [Fact]
            public async Task WhenAnyPropertyIsEnumInOtherNamespace_ThenAlerts()
            {
                const string input = @"
using System;
using System.Collections.Generic;
using Application.Interfaces.Resources;
using AnotherNamespace;
namespace ANamespace
{
    public class AClass : IIdentifiableResource
    {
        public required string Id { get; set; }

        public required AnEnum AProperty { get; set; }
    }
}
namespace AnotherNamespace
{
    public enum AnEnum
    {
        AValue
    }
}";

                await Verify.DiagnosticExists<ApplicationLayerAnalyzer>(
                    ApplicationLayerAnalyzer.Rule014, input, 12, 32, "AProperty", AllTypes,
                    AnalyzerConstants.ResourceTypesNamespace);
            }

            [Fact]
            public async Task WhenAnyPropertyIsSupportedListOfPrimitive_ThenNoAlert()
            {
                const string input = @"
using System;
using System.Collections.Generic;
using Application.Interfaces.Resources;
namespace ANamespace;
public class AClass : IIdentifiableResource
{
    public required string Id { get; set; }

    public required List<string> AProperty { get; set; }
}";

                await Verify.NoDiagnosticExists<ApplicationLayerAnalyzer>(input);
            }

            [Fact]
            public async Task WhenAnyPropertyIsListOfClassInCorrectNamespace_ThenNoAlert()
            {
                const string input = @"
using System;
using System.Collections.Generic;
using Application.Interfaces.Resources;
using Application.Resources.Shared;
namespace ANamespace
{
    public class AClass : IIdentifiableResource
    {
        public required string Id { get; set; }

        public required List<AnotherClass> AProperty { get; set; }
    }
}
namespace Application.Resources.Shared
{
    public class AnotherClass
    {
    }
}";

                await Verify.NoDiagnosticExists<ApplicationLayerAnalyzer>(input);
            }

            [Fact]
            public async Task WhenAnyPropertyIsNullableListOfClassInCorrectNamespace_ThenNoAlert()
            {
                const string input = @"
using System;
using System.Collections.Generic;
using Application.Interfaces.Resources;
using Application.Resources.Shared;
namespace ANamespace
{
    public class AClass : IIdentifiableResource
    {
        public required string Id { get; set; }

        public List<AnotherClass>? AProperty { get; set; }
    }
}
namespace Application.Resources.Shared
{
    public class AnotherClass
    {
    }
}";

                await Verify.NoDiagnosticExists<ApplicationLayerAnalyzer>(input);
            }

            [Fact]
            public async Task WhenAnyPropertyIsListOfClassInOtherNamespace_ThenAlerts()
            {
                const string input = @"
using System;
using System.Collections.Generic;
using Application.Interfaces.Resources;
using AnotherNamespace;
namespace ANamespace
{
    public class AClass : IIdentifiableResource
    {
        public required string Id { get; set; }

        public required List<AnotherClass> AProperty { get; set; }
    }
}
namespace AnotherNamespace
{
    public enum AnotherClass
    {
    }
}";

                await Verify.DiagnosticExists<ApplicationLayerAnalyzer>(
                    ApplicationLayerAnalyzer.Rule014, input, 12, 44, "AProperty", AllTypes,
                    AnalyzerConstants.ResourceTypesNamespace);
            }

            [Fact]
            public async Task WhenAnyPropertyIsSupportedDictionaryOfPrimitive_ThenNoAlert()
            {
                const string input = @"
using System;
using System.Collections.Generic;
using Application.Interfaces.Resources;
namespace ANamespace;
public class AClass : IIdentifiableResource
{
    public required string Id { get; set; }

    public required Dictionary<string, string> AProperty { get; set; }
}";

                await Verify.NoDiagnosticExists<ApplicationLayerAnalyzer>(input);
            }

            [Fact]
            public async Task WhenAnyPropertyIsDictionaryOfClassInCorrectNamespace_ThenNoAlert()
            {
                const string input = @"
using System;
using System.Collections.Generic;
using Application.Interfaces.Resources;
using Application.Resources.Shared;
namespace ANamespace
{
    public class AClass : IIdentifiableResource
    {
        public required string Id { get; set; }

        public required Dictionary<string, AnotherClass> AProperty { get; set; }
    }
}
namespace Application.Resources.Shared
{
    public class AnotherClass
    {
    }
}";

                await Verify.NoDiagnosticExists<ApplicationLayerAnalyzer>(input);
            }

            [Fact]
            public async Task WhenAnyPropertyIsNullableDictionaryOfClassInCorrectNamespace_ThenNoAlert()
            {
                const string input = @"
using System;
using System.Collections.Generic;
using Application.Interfaces.Resources;
using Application.Resources.Shared;
namespace ANamespace
{
    public class AClass : IIdentifiableResource
    {
        public required string Id { get; set; }

        public Dictionary<string, AnotherClass>? AProperty { get; set; }
    }
}
namespace Application.Resources.Shared
{
    public class AnotherClass
    {
    }
}";

                await Verify.NoDiagnosticExists<ApplicationLayerAnalyzer>(input);
            }

            [Fact]
            public async Task WhenAnyPropertyIsDictionaryOfClassInOtherNamespace_ThenAlerts()
            {
                const string input = @"
using System;
using System.Collections.Generic;
using Application.Interfaces.Resources;
using AnotherNamespace;
namespace ANamespace
{
    public class AClass : IIdentifiableResource
    {
        public required string Id { get; set; }

        public required Dictionary<string, AnotherClass> AProperty { get; set; }
    }
}
namespace AnotherNamespace
{
    public enum AnotherClass
    {
    }
}";

                await Verify.DiagnosticExists<ApplicationLayerAnalyzer>(
                    ApplicationLayerAnalyzer.Rule014, input, 12, 58, "AProperty", AllTypes,
                    AnalyzerConstants.ResourceTypesNamespace);
            }
        }
    }

    [UsedImplicitly]
    public class GivenAReadModel
    {
        [Trait("Category", "Unit.Tooling")]
        public class GivenAnyReadModel
        {
            [Fact]
            public async Task WhenComplete_ThenNoAlert()
            {
                const string input = @"
using System;
using Application.Persistence.Common;
using QueryAny;
using Common;
namespace ANamespace;
[EntityName(""AClass"")]
public class AClass : ReadModelEntity
{
    public Optional<string> AProperty { get; set; }
}";

                await Verify.NoDiagnosticExists<ApplicationLayerAnalyzer>(input);
            }
        }

        [Trait("Category", "Unit.Tooling")]
        public class GivenRule020
        {
            [Fact]
            public async Task WhenIsNotPublic_ThenAlerts()
            {
                const string input = @"
using System;
using Application.Persistence.Common;
using QueryAny;
using Common;
namespace ANamespace;
[EntityName(""AClass"")]
internal class AClass : ReadModelEntity
{
    public Optional<string> AProperty { get; set; }
}";

                await Verify.DiagnosticExists<ApplicationLayerAnalyzer>(
                    ApplicationLayerAnalyzer.Rule020, input, 8, 16, "AClass");
            }
        }

        [Trait("Category", "Unit.Tooling")]
        public class GivenRule021
        {
            [Fact]
            public async Task WhenMissingEntityNameAttribute_ThenAlerts()
            {
                const string input = @"
using System;
using Application.Persistence.Common;
using QueryAny;
using Common;
namespace ANamespace;
public class AClass : ReadModelEntity
{
    public Optional<string> AProperty { get; set; }
}";

                await Verify.DiagnosticExists<ApplicationLayerAnalyzer>(
                    ApplicationLayerAnalyzer.Rule021, input, 7, 14, "AClass");
            }
        }

        [Trait("Category", "Unit.Tooling")]
        public class GivenRule022
        {
            [Fact]
            public async Task WhenHasCtorAndNotParameterless_ThenAlerts()
            {
                const string input = @"
using System;
using Application.Persistence.Common;
using QueryAny;
using Common;
namespace ANamespace;
[EntityName(""AClass"")]
public class AClass : ReadModelEntity
{
    public AClass(string id)
    {
        Id = id;
    }

    public Optional<string> AProperty { get; set; }
}";

                await Verify.DiagnosticExists<ApplicationLayerAnalyzer>(
                    ApplicationLayerAnalyzer.Rule022, input, 8, 14, "AClass");
            }

            [Fact]
            public async Task WhenHasCtorAndPrivate_ThenAlerts()
            {
                const string input = @"
using System;
using Application.Persistence.Common;
using QueryAny;
using Common;
namespace ANamespace;
[EntityName(""AClass"")]
public class AClass : ReadModelEntity
{
    private AClass()
    {
        Id = string.Empty;
    }

    public Optional<string> AProperty { get; set; }
}";

                await Verify.DiagnosticExists<ApplicationLayerAnalyzer>(
                    ApplicationLayerAnalyzer.Rule022, input, 8, 14, "AClass");
            }

            [Fact]
            public async Task WhenHasCtorAndIsParameterless_ThenNoAlert()
            {
                const string input = @"
using System;
using Application.Persistence.Common;
using QueryAny;
using Common;
namespace ANamespace;
[EntityName(""AClass"")]
public class AClass : ReadModelEntity
{
    public AClass()
    {
        Id = string.Empty;
    }

    public Optional<string> AProperty { get; set; }
}";

                await Verify.NoDiagnosticExists<ApplicationLayerAnalyzer>(input);
            }
        }

        [Trait("Category", "Unit.Tooling")]
        public class GivenRule023
        {
            [Fact]
            public async Task WhenAnyPropertyHasNoSetter_ThenAlerts()
            {
                const string input = @"
using System;
using Common;
using Application.Persistence.Common;
using QueryAny;
namespace ANamespace;
[EntityName(""AClass"")]
public class AClass : ReadModelEntity
{
    public Optional<string> AProperty { get; }
}";

                await Verify.DiagnosticExists<ApplicationLayerAnalyzer>(
                    ApplicationLayerAnalyzer.Rule023, input, 10, 29, "AProperty");
            }
        }

        [Trait("Category", "Unit.Tooling")]
        public class GivenRule024
        {
            [Fact]
            public async Task WhenAnyPropertyIsNullable_ThenAlerts()
            {
                const string input = @"
using System;
using Common;
using Application.Persistence.Common;
using QueryAny;
namespace ANamespace;
[EntityName(""AClass"")]
public class AClass : ReadModelEntity
{
    public string? AProperty { get; set; }
}";

                await Verify.DiagnosticExists<ApplicationLayerAnalyzer>(
                    ApplicationLayerAnalyzer.Rule024, input, 10, 20, "AProperty");
            }

            [Fact]
            public async Task WhenAnyPropertyIsOptionalAndNotInitialized_ThenNoAlert()
            {
                const string input = @"
using System;
using Common;
using Application.Persistence.Common;
using QueryAny;
namespace ANamespace;
[EntityName(""AClass"")]
public class AClass : ReadModelEntity
{
    public Optional<string> AProperty { get; set; }
}";

                await Verify.NoDiagnosticExists<ApplicationLayerAnalyzer>(input);
            }

            [Fact]
            public async Task WhenAnyPropertyIsOptionalAndInitialized_ThenNoAlert()
            {
                const string input = @"
using System;
using Common;
using Application.Persistence.Common;
using QueryAny;
using Common;
namespace ANamespace;
[EntityName(""AClass"")]
public class AClass : ReadModelEntity
{

    public Optional<string> AProperty { get; set; } = string.Empty;
}";

                await Verify.NoDiagnosticExists<ApplicationLayerAnalyzer>(input);
            }
        }

        [Trait("Category", "Unit.Tooling")]
        public class GivenRule025
        {
            private const string AllTypes =
                "bool or string or ulong or int or long or double or decimal or System.DateTime or byte";

            [Fact]
            public async Task WhenAnyPropertyIsNotSupportedPrimitive_ThenAlerts()
            {
                const string input = @"
using System;
using Common;
using System.Collections.Generic;
using Application.Persistence.Common;
using QueryAny;
namespace ANamespace;
[EntityName(""AClass"")]
public class AClass : ReadModelEntity
{

    public Optional<char> AProperty { get; set; }
}";

                await Verify.DiagnosticExists<ApplicationLayerAnalyzer>(
                    ApplicationLayerAnalyzer.Rule025, input, 12, 27, "AProperty", AllTypes);
            }

            [Fact]
            public async Task WhenAnyPropertyIsNotSupportedListOfPrimitive_ThenAlerts()
            {
                const string input = @"
using System;
using Common;
using System.Collections.Generic;
using Application.Persistence.Common;
using QueryAny;
namespace ANamespace;
[EntityName(""AClass"")]
public class AClass : ReadModelEntity
{

    public Optional<List<char>> AProperty { get; set; }
}";

                await Verify.DiagnosticExists<ApplicationLayerAnalyzer>(
                    ApplicationLayerAnalyzer.Rule025, input, 12, 33, "AProperty", AllTypes);
            }

            [Fact]
            public async Task WhenAnyPropertyIsNotSupportedDictionaryOfPrimitive_ThenAlerts()
            {
                const string input = @"
using System;
using Common;
using System.Collections.Generic;
using Application.Persistence.Common;
using QueryAny;
namespace ANamespace;
[EntityName(""AClass"")]
public class AClass : ReadModelEntity
{

    public Optional<Dictionary<string, char>> AProperty { get; set; }
}";

                await Verify.DiagnosticExists<ApplicationLayerAnalyzer>(
                    ApplicationLayerAnalyzer.Rule025, input, 12, 47, "AProperty", AllTypes);
            }

            [Fact]
            public async Task WhenAnyPropertyIsNotSupportedDictionaryKeyType_ThenAlerts()
            {
                const string input = @"
using System;
using Common;
using System.Collections.Generic;
using Application.Persistence.Common;
using QueryAny;
namespace ANamespace;
[EntityName(""AClass"")]
public class AClass : ReadModelEntity
{

    public Optional<Dictionary<char, string>> AProperty { get; set; }
}";

                await Verify.DiagnosticExists<ApplicationLayerAnalyzer>(
                    ApplicationLayerAnalyzer.Rule025, input, 12, 47, "AProperty", AllTypes);
            }

            [Fact]
            public async Task WhenAnyPropertyIsSupportedPrimitive_ThenNoAlert()
            {
                const string input = @"
using System;
using Common;
using System.Collections.Generic;
using Application.Persistence.Common;
using QueryAny;
namespace ANamespace;
[EntityName(""AClass"")]
public class AClass : ReadModelEntity
{

    public Optional<string> AProperty { get; set; }
}";

                await Verify.NoDiagnosticExists<ApplicationLayerAnalyzer>(input);
            }

            [Fact]
            public async Task WhenAnyPropertyIsSupportedOptionalPrimitive_ThenNoAlert()
            {
                const string input = @"
using System;
using Common;
using System.Collections.Generic;
using Application.Persistence.Common;
using QueryAny;
namespace ANamespace;
[EntityName(""AClass"")]
public class AClass : ReadModelEntity
{

    public Optional<string> AProperty { get; set; }
}";

                await Verify.NoDiagnosticExists<ApplicationLayerAnalyzer>(input);
            }

            [Fact]
            public async Task WhenAnyPropertyIsSupportedOptionalNullablePrimitive_ThenNoAlert()
            {
                const string input = @"
using System;
using Common;
using System.Collections.Generic;
using Application.Persistence.Common;
using QueryAny;
namespace ANamespace;
[EntityName(""AClass"")]
public class AClass : ReadModelEntity
{

    public Optional<DateTime?> AProperty { get; set; }
}";

                await Verify.NoDiagnosticExists<ApplicationLayerAnalyzer>(input);
            }

            [Fact]
            public async Task WhenAnyPropertyIsValueObject_ThenNoAlert()
            {
                const string input = @"
using System;
using Common;
using System.Collections.Generic;
using Application.Persistence.Common;
using QueryAny;
using Domain.Interfaces.ValueObjects;
namespace ANamespace;
[EntityName(""AClass"")]
public class AClass : ReadModelEntity
{
    public Optional<AValueObject> AProperty { get; set; }
}
public class AValueObject : IValueObject
{
    public string Dehydrate()
    {
        return string.Empty;
    }
}";
                await Verify.NoDiagnosticExists<ApplicationLayerAnalyzer>(input);
            }

            [Fact]
            public async Task WhenAnyPropertyIsOptionalValueObject_ThenNoAlert()
            {
                const string input = @"
using System;
using Common;
using System.Collections.Generic;
using Application.Persistence.Common;
using QueryAny;
using Domain.Interfaces.ValueObjects;
namespace ANamespace;
[EntityName(""AClass"")]
public class AClass : ReadModelEntity
{
    public Optional<AValueObject> AProperty { get; set; }
}
public class AValueObject : IValueObject
{
    public string Dehydrate()
    {
        return string.Empty;
    }
}";
                await Verify.NoDiagnosticExists<ApplicationLayerAnalyzer>(input);
            }

            [Fact]
            public async Task WhenAnyPropertyIsEnum_ThenNoAlert()
            {
                const string input = @"
using System;
using Common;
using System.Collections.Generic;
using Application.Persistence.Common;
using QueryAny;
namespace ANamespace;
[EntityName(""AClass"")]
public class AClass : ReadModelEntity
{
    public Optional<AnEnum> AProperty { get; set; }
}
public enum AnEnum
{
    AValue
}";

                await Verify.NoDiagnosticExists<ApplicationLayerAnalyzer>(input);
            }

            [Fact]
            public async Task WhenAnyPropertyIsSupportedListOfPrimitive_ThenNoAlert()
            {
                const string input = @"
using System;
using Common;
using System.Collections.Generic;
using Application.Persistence.Common;
using QueryAny;
namespace ANamespace;
[EntityName(""AClass"")]
public class AClass : ReadModelEntity
{

    public Optional<List<string>> AProperty { get; set; }
}";

                await Verify.NoDiagnosticExists<ApplicationLayerAnalyzer>(input);
            }

            [Fact]
            public async Task WhenAnyPropertyIsSupportedDictionaryOfPrimitive_ThenNoAlert()
            {
                const string input = @"
using System;
using Common;
using System.Collections.Generic;
using Application.Persistence.Common;
using QueryAny;
namespace ANamespace;
[EntityName(""AClass"")]
public class AClass : ReadModelEntity
{

    public Optional<Dictionary<string, string>> AProperty { get; set; }
}";

                await Verify.NoDiagnosticExists<ApplicationLayerAnalyzer>(input);
            }
        }

        [Trait("Category", "Unit.Tooling")]
        public class GivenRule030
        {
            [Fact]
            public async Task WhenRepositoryInterfaceIsNotDerivedInApplicationNamespace_ThenAlerts()
            {
                const string input = @"
using System;
namespace AnNamespaceEndingWithApplication;
public interface IARepository
{
}";

                await Verify.DiagnosticExists<ApplicationLayerAnalyzer>(
                    ApplicationLayerAnalyzer.Rule030, input, 4, 18, "IARepository");
            }

            [Fact]
            public async Task WhenRepositoryInterfaceIsNotDerivedNotInApplicationNamespace_ThenNoAlert()
            {
                const string input = @"
using System;
namespace ANamespace;
public interface IARepository
{
}";

                await Verify.NoDiagnosticExists<ApplicationLayerAnalyzer>(input);
            }

            [Fact]
            public async Task WhenNonRepositoryInterfaceIsNotDerived_ThenNoAlert()
            {
                const string input = @"
using System;
namespace AnNamespaceEndingWithApplication;
public interface IAInterface
{
}";

                await Verify.NoDiagnosticExists<ApplicationLayerAnalyzer>(input);
            }

            [Fact]
            public async Task WhenRepositoryInterfaceIsDerived_ThenNoAlert()
            {
                const string input = @"
using System;
using Application.Persistence.Interfaces;
namespace AnNamespaceEndingWithApplication;
public interface IARepository : IApplicationRepository
{
}";

                await Verify.NoDiagnosticExists<ApplicationLayerAnalyzer>(input);
            }
        }
    }
}