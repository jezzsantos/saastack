extern alias NonPlatformAnalyzers;
using NonPlatformAnalyzers::JetBrains.Annotations;
using Xunit;
using EventingAnalyzer = NonPlatformAnalyzers::Tools.Analyzers.NonPlatform.EventingAnalyzer;

namespace Tools.Analyzers.NonPlatform.UnitTests;

[UsedImplicitly]
public class EventingAnalyzerSpec
{
    [Trait("Category", "Unit.Tooling")]
    public class GivenAnyRule
    {
        [Fact]
        public async Task WhenInExcludedNamespace_ThenNoAlert()
        {
            const string input = @"
using Infrastructure.Web.Api.Interfaces;
namespace Common;
public sealed class AClass : IWebApiService
{
}";

            await Verify.NoDiagnosticExists<EventingAnalyzer>(input);
        }
    }

    [UsedImplicitly]
    public class GivenAnIntegrationEvent
    {
        [Trait("Category", "Unit.Tooling")]
        public class GivenAnyIntegrationEvent
        {
            [Fact]
            public async Task WhenNoCtor_ThenNoAlert()
            {
                const string input = @"
using System;
using Infrastructure.Eventing.Interfaces.Notifications;
namespace ANamespace;
public sealed class AClass : IIntegrationEvent
{
    public required string RootId { get; set; }

    public required DateTime OccurredUtc { get; set; }
}";

                await Verify.NoDiagnosticExists<EventingAnalyzer>(input);
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
using Infrastructure.Eventing.Interfaces.Notifications;
namespace ANamespace;
internal sealed class AClass : IIntegrationEvent
{
    public required string RootId { get; set; }

    public required DateTime OccurredUtc { get; set; }
}";

                await Verify.DiagnosticExists<EventingAnalyzer>(
                    EventingAnalyzer.Rule010, input, 5, 23, "AClass");
            }
        }

        [Trait("Category", "Unit.Tooling")]
        public class GivenRule011
        {
            [Fact]
            public async Task WhenIsNotSealed_ThenAlerts()
            {
                const string input = @"
using System;
using Infrastructure.Eventing.Interfaces.Notifications;
namespace ANamespace;
public class AClass : IIntegrationEvent
{
    public required string RootId { get; set; }

    public required DateTime OccurredUtc { get; set; }
}";

                await Verify.DiagnosticExists<EventingAnalyzer>(
                    EventingAnalyzer.Rule011, input, 5, 14, "AClass");
            }
        }

        [Trait("Category", "Unit.Tooling")]
        public class GivenRule012
        {
            [Fact]
            public async Task WhenHasCtorAndNotParameterless_ThenAlerts()
            {
                const string input = @"
using System;
using Infrastructure.Eventing.Interfaces.Notifications;
namespace ANamespace;
public sealed class AClass : IIntegrationEvent
{
    public AClass(string rootId, DateTime occurredUtc)
    {
        RootId = rootId;
        OccurredUtc = occurredUtc;
    }

    public required string RootId { get; set; }

    public required DateTime OccurredUtc { get; set; }
}";

                await Verify.DiagnosticExists<EventingAnalyzer>(
                    EventingAnalyzer.Rule012, input, 5, 21, "AClass");
            }

            [Fact]
            public async Task WhenHasCtorAndPrivate_ThenAlerts()
            {
                const string input = @"
using System;
using Infrastructure.Eventing.Interfaces.Notifications;
namespace ANamespace;
public sealed class AClass : IIntegrationEvent
{
    private AClass()
    {
        RootId = string.Empty;
        OccurredUtc = DateTime.UtcNow;
    }

    public required string RootId { get; set; }

    public required DateTime OccurredUtc { get; set; }
}";

                await Verify.DiagnosticExists<EventingAnalyzer>(
                    EventingAnalyzer.Rule012, input, 5, 21, "AClass");
            }

            [Fact]
            public async Task WhenHasCtorAndIsParameterless_ThenNoAlert()
            {
                const string input = @"
using System;
using Infrastructure.Eventing.Interfaces.Notifications;
namespace ANamespace;
public sealed class AClass : IIntegrationEvent
{
    public AClass()
    {
        RootId = string.Empty;
        OccurredUtc = DateTime.UtcNow;
    }

    public required string RootId { get; set; }

    public required DateTime OccurredUtc { get; set; }
}";

                await Verify.NoDiagnosticExists<EventingAnalyzer>(input);
            }
        }

        [Trait("Category", "Unit.Tooling")]
        public class GivenRule013
        {
            [Fact]
            public async Task WhenAnyPropertyHasNoSetter_ThenAlerts()
            {
                const string input = @"
using System;
using Infrastructure.Eventing.Interfaces.Notifications;
namespace ANamespace;
public sealed class AClass : IIntegrationEvent
{
    public required string RootId { get; set; }

    public required DateTime OccurredUtc { get; set; }

    public string? AProperty { get; }
}";

                await Verify.DiagnosticExists<EventingAnalyzer>(
                    EventingAnalyzer.Rule013, input, 11, 20, "AProperty", null);
            }
        }

        [Trait("Category", "Unit.Tooling")]
        public class GivenRule014
        {
            [Fact]
            public async Task WhenAnyPropertyReferenceTypeIsNotRequiredAndNotInitializedAndNotNullable_ThenAlerts()
            {
                const string input = @"
using System;
using Infrastructure.Eventing.Interfaces.Notifications;
namespace ANamespace;
public sealed class AClass : IIntegrationEvent
{
    public required string RootId { get; set; }

    public required DateTime OccurredUtc { get; set; }

    public string AProperty { get; set; }
}";

                await Verify.DiagnosticExists<EventingAnalyzer>(
                    EventingAnalyzer.Rule014, input, 11, 19, "AProperty");
            }

            [Fact]
            public async Task WhenAnyPropertyReferenceTypeIsRequiredAndNotInitializedAndNotNullable_ThenNoAlert()
            {
                const string input = @"
using System;
using Infrastructure.Eventing.Interfaces.Notifications;
namespace ANamespace;
public sealed class AClass : IIntegrationEvent
{
    public required string RootId { get; set; }

    public required DateTime OccurredUtc { get; set; }

    public required string AProperty { get; set; }
}";

                await Verify.NoDiagnosticExists<EventingAnalyzer>(input);
            }

            [Fact]
            public async Task WhenAnyPropertyReferenceTypeIsInitializedAndNotRequiredAndNotNullable_ThenNoAlert()
            {
                const string input = @"
using System;
using Infrastructure.Eventing.Interfaces.Notifications;
namespace ANamespace;
public sealed class AClass : IIntegrationEvent
{
    public required string RootId { get; set; }

    public required DateTime OccurredUtc { get; set; }

    public string AProperty { get; set; } = string.Empty;
}";

                await Verify.NoDiagnosticExists<EventingAnalyzer>(input);
            }

            [Fact]
            public async Task WhenAnyPropertyReferenceTypeIsNullableAndNotRequiredAndNotInitialized_ThenNoAlert()
            {
                const string input = @"
using System;
using Infrastructure.Eventing.Interfaces.Notifications;
namespace ANamespace;
public sealed class AClass : IIntegrationEvent
{
    public required string RootId { get; set; }

    public required DateTime OccurredUtc { get; set; }

    public string? AProperty { get; set; }
}";

                await Verify.NoDiagnosticExists<EventingAnalyzer>(input);
            }

            [Fact]
            public async Task WhenAnyPropertyReferenceTypeIsNullableAndRequiredAndNotInitialized_ThenNoAlert()
            {
                const string input = @"
using System;
using Infrastructure.Eventing.Interfaces.Notifications;
namespace ANamespace;
public sealed class AClass : IIntegrationEvent
{
    public required string RootId { get; set; }

    public required DateTime OccurredUtc { get; set; }

    public string? AProperty { get; set; }
}";

                await Verify.NoDiagnosticExists<EventingAnalyzer>(input);
            }

            [Fact]
            public async Task WhenAnyPropertyReferenceTypeIsNullableAndInitializedAndNotRequired_ThenNoAlert()
            {
                const string input = @"
using System;
using Infrastructure.Eventing.Interfaces.Notifications;
namespace ANamespace;
public sealed class AClass : IIntegrationEvent
{
    public required string RootId { get; set; }

    public required DateTime OccurredUtc { get; set; }

    public string? AProperty { get; set; } = string.Empty;
}";

                await Verify.NoDiagnosticExists<EventingAnalyzer>(input);
            }

            [Fact]
            public async Task WhenAnyPropertyReferenceTypeIsInitializedAndRequiredAndNotNullable_ThenNoAlert()
            {
                const string input = @"
using System;
using Infrastructure.Eventing.Interfaces.Notifications;
namespace ANamespace;
public sealed class AClass : IIntegrationEvent
{
    public required string RootId { get; set; }

    public required DateTime OccurredUtc { get; set; }

    public required string AProperty { get; set; } = string.Empty;
}";

                await Verify.NoDiagnosticExists<EventingAnalyzer>(input);
            }

            [Fact]
            public async Task WhenAnyPropertyReferenceTypeIsInitializedAndNullableAndNotRequired_ThenNoAlert()
            {
                const string input = @"
using System;
using Infrastructure.Eventing.Interfaces.Notifications;
namespace ANamespace;
public sealed class AClass : IIntegrationEvent
{
    public required string RootId { get; set; }

    public required DateTime OccurredUtc { get; set; }

    public string? AProperty { get; set; } = string.Empty;
}";

                await Verify.NoDiagnosticExists<EventingAnalyzer>(input);
            }

            [Fact]
            public async Task WhenAnyPropertyReferenceTypeIsInitializedAndRequiredAndNullable_ThenNoAlert()
            {
                const string input = @"
using System;
using Infrastructure.Eventing.Interfaces.Notifications;
namespace ANamespace;
public sealed class AClass : IIntegrationEvent
{
    public required string RootId { get; set; }

    public required DateTime OccurredUtc { get; set; }

    public string? AProperty { get; set; } = string.Empty;
}";

                await Verify.NoDiagnosticExists<EventingAnalyzer>(input);
            }

            [Fact]
            public async Task WhenAnyPropertyValueTypeIsNotRequiredAndNotInitializedAndNotNullable_ThenAlerts()
            {
                const string input = @"
using System;
using Infrastructure.Eventing.Interfaces.Notifications;
namespace ANamespace;
public sealed class AClass : IIntegrationEvent
{
    public required string RootId { get; set; }

    public required DateTime OccurredUtc { get; set; }

    public DateTime AProperty { get; set; }
}";

                await Verify.DiagnosticExists<EventingAnalyzer>(
                    EventingAnalyzer.Rule014, input, 11, 21, "AProperty");
            }

            [Fact]
            public async Task WhenAnyPropertyValueTypeIsRequiredAndNotInitializedAndNotNullable_ThenNoAlert()
            {
                const string input = @"
using System;
using Infrastructure.Eventing.Interfaces.Notifications;
namespace ANamespace;
public sealed class AClass : IIntegrationEvent
{
    public required string RootId { get; set; }

    public required DateTime OccurredUtc { get; set; }

    public required DateTime AProperty { get; set; }
}";

                await Verify.NoDiagnosticExists<EventingAnalyzer>(input);
            }

            [Fact]
            public async Task WhenAnyPropertyValueTypeIsInitializedAndNotRequiredAndNotNullable_ThenNoAlert()
            {
                const string input = @"
using System;
using Infrastructure.Eventing.Interfaces.Notifications;
namespace ANamespace;
public sealed class AClass : IIntegrationEvent
{
    public required string RootId { get; set; }

    public required DateTime OccurredUtc { get; set; }

    public DateTime AProperty { get; set; } = DateTime.UtcNow;
}";

                await Verify.NoDiagnosticExists<EventingAnalyzer>(input);
            }

            [Fact]
            public async Task WhenAnyPropertyValueTypeIsNullableAndNotRequiredAndNotInitialized_ThenNoAlert()
            {
                const string input = @"
using System;
using Infrastructure.Eventing.Interfaces.Notifications;
namespace ANamespace;
public sealed class AClass : IIntegrationEvent
{
    public required string RootId { get; set; }

    public required DateTime OccurredUtc { get; set; }

    public DateTime? AProperty { get; set; }
}";

                await Verify.NoDiagnosticExists<EventingAnalyzer>(input);
            }

            [Fact]
            public async Task WhenAnyPropertyValueTypeIsNullableAndRequiredAndNotInitialized_ThenNoAlert()
            {
                const string input = @"
using System;
using Infrastructure.Eventing.Interfaces.Notifications;
namespace ANamespace;
public sealed class AClass : IIntegrationEvent
{
    public required string RootId { get; set; }

    public required DateTime OccurredUtc { get; set; }

    public required DateTime? AProperty { get; set; }
}";

                await Verify.NoDiagnosticExists<EventingAnalyzer>(input);
            }

            [Fact]
            public async Task WhenAnyPropertyValueTypeIsNullableAndInitializedAndNotRequired_ThenNoAlert()
            {
                const string input = @"
using System;
using Infrastructure.Eventing.Interfaces.Notifications;
namespace ANamespace;
public sealed class AClass : IIntegrationEvent
{
    public required string RootId { get; set; }

    public required DateTime OccurredUtc { get; set; }

    public DateTime? AProperty { get; set; } = DateTime.UtcNow;
}";

                await Verify.NoDiagnosticExists<EventingAnalyzer>(input);
            }

            [Fact]
            public async Task WhenAnyPropertyValueTypeIsInitializedAndRequiredAndNotNullable_ThenNoAlert()
            {
                const string input = @"
using System;
using Infrastructure.Eventing.Interfaces.Notifications;
namespace ANamespace;
public sealed class AClass : IIntegrationEvent
{
    public required string RootId { get; set; }

    public required DateTime OccurredUtc { get; set; }

    public required DateTime AProperty { get; set; } = DateTime.UtcNow;
}";

                await Verify.NoDiagnosticExists<EventingAnalyzer>(input);
            }

            [Fact]
            public async Task WhenAnyPropertyValueTypeIsInitializedAndNullableAndNotRequired_ThenNoAlert()
            {
                const string input = @"
using System;
using Infrastructure.Eventing.Interfaces.Notifications;
namespace ANamespace;
public sealed class AClass : IIntegrationEvent
{
    public required string RootId { get; set; }

    public required DateTime OccurredUtc { get; set; }

    public DateTime? AProperty { get; set; } = DateTime.UtcNow;
}";

                await Verify.NoDiagnosticExists<EventingAnalyzer>(input);
            }

            [Fact]
            public async Task WhenAnyPropertyValueTypeIsInitializedAndRequiredAndNullable_ThenNoAlert()
            {
                const string input = @"
using System;
using Infrastructure.Eventing.Interfaces.Notifications;
namespace ANamespace;
public sealed class AClass : IIntegrationEvent
{
    public required string RootId { get; set; }

    public required DateTime OccurredUtc { get; set; }

    public required DateTime? AProperty { get; set; } = DateTime.UtcNow;
}";

                await Verify.NoDiagnosticExists<EventingAnalyzer>(input);
            }
        }

        [Trait("Category", "Unit.Tooling")]
        public class GivenRule015
        {
            [Fact]
            public async Task WhenAnyPropertyReferenceTypeIsOptional_ThenAlerts()
            {
                const string input = @"
using System;
using Infrastructure.Eventing.Interfaces.Notifications;
using Common;
namespace ANamespace;
public sealed class AClass : IIntegrationEvent
{
    public required string RootId { get; set; }

    public required DateTime OccurredUtc { get; set; }

    public required Optional<string> AProperty { get; set; }
}";

                await Verify.DiagnosticExists<EventingAnalyzer>(input,
                    (EventingAnalyzer.Rule015, 12, 38, "AProperty", null),
                    (EventingAnalyzer.Rule016, 12, 38, "AProperty", [GivenRule016.AllTypes]));
            }

            [Fact]
            public async Task WhenAnyPropertyReferenceTypeIsNullable_ThenNoAlert()
            {
                const string input = @"
using System;
using Infrastructure.Eventing.Interfaces.Notifications;
namespace ANamespace;
public sealed class AClass : IIntegrationEvent
{
    public required string RootId { get; set; }

    public required DateTime OccurredUtc { get; set; }

    public string? AProperty { get; set; }
}";

                await Verify.NoDiagnosticExists<EventingAnalyzer>(input);
            }
        }

        [Trait("Category", "Unit.Tooling")]
        public class GivenRule016
        {
            public const string AllTypes =
                "bool or string or ulong or int or long or double or decimal or System.DateTime or byte";

            [Fact]
            public async Task WhenAnyPropertyIsNotSupportedPrimitive_ThenAlerts()
            {
                const string input = @"
using System;
using Infrastructure.Eventing.Interfaces.Notifications;
namespace ANamespace;
public sealed class AClass : IIntegrationEvent
{
    public required string RootId { get; set; }

    public required DateTime OccurredUtc { get; set; }

    public required char AProperty { get; set; }
}";

                await Verify.DiagnosticExists<EventingAnalyzer>(
                    EventingAnalyzer.Rule016, input, 11, 26, "AProperty", AllTypes);
            }

            [Fact]
            public async Task WhenAnyPropertyIsNotSupportedListOfPrimitive_ThenAlerts()
            {
                const string input = @"
using System;
using System.Collections.Generic;
using Infrastructure.Eventing.Interfaces.Notifications;
namespace ANamespace;
public sealed class AClass : IIntegrationEvent
{
    public required string RootId { get; set; }

    public required DateTime OccurredUtc { get; set; }

    public required List<char> AProperty { get; set; }
}";

                await Verify.DiagnosticExists<EventingAnalyzer>(
                    EventingAnalyzer.Rule016, input, 12, 32, "AProperty", AllTypes);
            }

            [Fact]
            public async Task WhenAnyPropertyIsNotSupportedDictionaryOfPrimitive_ThenAlerts()
            {
                const string input = @"
using System;
using System.Collections.Generic;
using Infrastructure.Eventing.Interfaces.Notifications;
namespace ANamespace;
public sealed class AClass : IIntegrationEvent
{
    public required string RootId { get; set; }

    public required DateTime OccurredUtc { get; set; }

    public required Dictionary<string, char> AProperty { get; set; }
}";

                await Verify.DiagnosticExists<EventingAnalyzer>(
                    EventingAnalyzer.Rule016, input, 12, 46, "AProperty", AllTypes);
            }

            [Fact]
            public async Task WhenAnyPropertyIsNotSupportedDictionaryKeyType_ThenAlerts()
            {
                const string input = @"
using System;
using System.Collections.Generic;
using Infrastructure.Eventing.Interfaces.Notifications;
namespace ANamespace;
public sealed class AClass : IIntegrationEvent
{
    public required string RootId { get; set; }

    public required DateTime OccurredUtc { get; set; }

    public required Dictionary<char, string> AProperty { get; set; }
}";

                await Verify.DiagnosticExists<EventingAnalyzer>(
                    EventingAnalyzer.Rule016, input, 12, 46, "AProperty", AllTypes);
            }

            [Fact]
            public async Task WhenAnyPropertyIsSupportedPrimitive_ThenNoAlert()
            {
                const string input = @"
using System;
using Infrastructure.Eventing.Interfaces.Notifications;
namespace ANamespace;
public sealed class AClass : IIntegrationEvent
{
    public required string RootId { get; set; }

    public required DateTime OccurredUtc { get; set; }

    public required string AProperty { get; set; }
}";

                await Verify.NoDiagnosticExists<EventingAnalyzer>(input);
            }

            [Fact]
            public async Task WhenAnyPropertyIsEnum_ThenAlerts()
            {
                const string input = @"
using System;
using Infrastructure.Eventing.Interfaces.Notifications;
namespace ANamespace;
public sealed class AClass : IIntegrationEvent
{
    public required string RootId { get; set; }

    public required DateTime OccurredUtc { get; set; }

    public required AnEnum AProperty { get; set; }
}
public enum AnEnum
{
    AValue
}
";

                await Verify.DiagnosticExists<EventingAnalyzer>(
                    EventingAnalyzer.Rule016, input, 11, 28, "AProperty", AllTypes);
            }

            [Fact]
            public async Task WhenAnyPropertyIsSupportedListOfPrimitive_ThenNoAlert()
            {
                const string input = @"
using System;
using System.Collections.Generic;
using Infrastructure.Eventing.Interfaces.Notifications;
namespace ANamespace;
public sealed class AClass : IIntegrationEvent
{
    public required string RootId { get; set; }

    public required DateTime OccurredUtc { get; set; }

    public required List<string> AProperty { get; set; }
}";

                await Verify.NoDiagnosticExists<EventingAnalyzer>(input);
            }

            [Fact]
            public async Task WhenAnyPropertyIsSupportedDictionaryOfPrimitive_ThenNoAlert()
            {
                const string input = @"
using System;
using System.Collections.Generic;
using Infrastructure.Eventing.Interfaces.Notifications;
namespace ANamespace;
public sealed class AClass : IIntegrationEvent
{
    public required string RootId { get; set; }

    public required DateTime OccurredUtc { get; set; }

    public required Dictionary<string, int> AProperty { get; set; }
}";

                await Verify.NoDiagnosticExists<EventingAnalyzer>(input);
            }

            [Fact]
            public async Task WhenAnyPropertyIsNotADto_ThenAlerts()
            {
                const string input = @"
using System;
using System.Collections.Generic;
using Infrastructure.Eventing.Interfaces.Notifications;
namespace ANamespace;
public sealed class AClass : IIntegrationEvent
{
    public required string RootId { get; set; }

    public required DateTime OccurredUtc { get; set; }

    public required ADto AProperty { get; set; }
}
public class ADto
{
    public string AProperty { get; }
}";

                await Verify.DiagnosticExists<EventingAnalyzer>(
                    EventingAnalyzer.Rule016, input, 12, 26, "AProperty", AllTypes);
            }

            [Fact]
            public async Task WhenAnyPropertyIsADto_ThenNoAlert()
            {
                const string input = @"
using System;
using System.Collections.Generic;
using Infrastructure.Eventing.Interfaces.Notifications;
using AnOtherNamespace;
namespace ANamespace
{
    public sealed class AClass : IIntegrationEvent
    {
        public required string RootId { get; set; }

        public required DateTime OccurredUtc { get; set; }

        public required ADto AProperty { get; set; }
    }
}
namespace AnOtherNamespace
{
    public class ADto
    {
        public required string AProperty1 { get; set; }

        public string? AProperty2 { get; set; }
    }
}";

                await Verify.NoDiagnosticExists<EventingAnalyzer>(input);
            }
        }
    }
}