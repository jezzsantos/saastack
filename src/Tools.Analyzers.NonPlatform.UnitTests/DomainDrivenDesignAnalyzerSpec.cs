extern alias NonPlatformAnalyzers;
using NonPlatformAnalyzers::Domain.Interfaces.ValueObjects;
using NonPlatformAnalyzers::JetBrains.Annotations;
using Xunit;
using NonPlatform_DomainDrivenDesignAnalyzer = NonPlatformAnalyzers::Tools.Analyzers.NonPlatform.DomainDrivenDesignAnalyzer;

namespace Tools.Analyzers.NonPlatform.UnitTests;

[UsedImplicitly]
public class DomainDrivenDesignAnalyzerSpec
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

            await Verify.NoDiagnosticExists<NonPlatform_DomainDrivenDesignAnalyzer>(input);
        }

        [Fact]
        public async Task WhenNotRootAggregate_ThenNoAlert()
        {
            const string input = @"
namespace ANamespace;
public sealed class AClass
{
}";

            await Verify.NoDiagnosticExists<NonPlatform_DomainDrivenDesignAnalyzer>(input);
        }

        [Fact]
        public async Task WhenNotEntity_ThenNoAlert()
        {
            const string input = @"
namespace ANamespace;
public sealed class AClass
{
}";

            await Verify.NoDiagnosticExists<NonPlatform_DomainDrivenDesignAnalyzer>(input);
        }

        [Fact]
        public async Task WhenNotValueObject_ThenNoAlert()
        {
            const string input = @"
namespace ANamespace;
public sealed class AClass
{
}";

            await Verify.NoDiagnosticExists<NonPlatform_DomainDrivenDesignAnalyzer>(input);
        }
    }

    [UsedImplicitly]
    public class GivenARootAggregate
    {
        [Trait("Category", "Unit.Tooling")]
        public class GivenRule010
        {
            [Fact]
            public async Task WhenHasNoCreateMethod_ThenAlerts()
            {
                const string input = @"
using Common;
using Domain.Common;
using Domain.Common.Entities;
using Domain.Common.Identity;
using Domain.Common.ValueObjects;
using Domain.Interfaces;
using Domain.Interfaces.Entities;
using Domain.Interfaces.ValueObjects;
namespace ANamespace;
public sealed class AClass : AggregateRootBase
{
    private AClass(IRecorder recorder, IIdentifierFactory idFactory) : base(recorder, idFactory)
    {
    }

    private AClass(IRecorder recorder, IIdentifierFactory idFactory, ISingleValueObject<string> identifier) : base(recorder, idFactory, identifier)
    {
    }

    protected override Result<Error> OnStateChanged(IDomainEvent @event, bool isReconstituting)
    {
        return Result.Ok;
    }

    public static AggregateRootFactory<AClass> Rehydrate()
    {
        return (identifier, container, properties) => new AClass(container.GetRequiredService<IRecorder>(),
            container.GetRequiredService<IIdentifierFactory>(), identifier);
    }
}";

                await Verify.DiagnosticExists<NonPlatform_DomainDrivenDesignAnalyzer>(
                    NonPlatform_DomainDrivenDesignAnalyzer.Rule010, input, 11,
                    21,
                    "AClass");
            }

            [Fact]
            public async Task WhenHasCreateInstanceMethod_ThenAlerts()
            {
                const string input = @"
using Common;
using Domain.Common;
using Domain.Common.Entities;
using Domain.Common.Identity;
using Domain.Common.ValueObjects;
using Domain.Interfaces;
using Domain.Interfaces.Entities;
using Domain.Interfaces.ValueObjects;
namespace ANamespace;
public sealed class AClass : AggregateRootBase
{
    private AClass(IRecorder recorder, IIdentifierFactory idFactory) : base(recorder, idFactory)
    {
    }

    private AClass(IRecorder recorder, IIdentifierFactory idFactory, ISingleValueObject<string> identifier) : base(recorder, idFactory, identifier)
    {
    }

    protected override Result<Error> OnStateChanged(IDomainEvent @event, bool isReconstituting)
    {
        return Result.Ok;
    }

    public static AggregateRootFactory<AClass> Rehydrate()
    {
        return (identifier, container, properties) => new AClass(container.GetRequiredService<IRecorder>(),
            container.GetRequiredService<IIdentifierFactory>(), identifier);
    }

    public void Create()
    {
    }
}";

                await Verify.DiagnosticExists<NonPlatform_DomainDrivenDesignAnalyzer>(
                    NonPlatform_DomainDrivenDesignAnalyzer.Rule010, input, 11,
                    21,
                    "AClass");
            }

            [Fact]
            public async Task WhenHasAtLeastOneCreateStaticMethod_ThenNoAlert()
            {
                const string input = @"
using System;
using Common;
using Domain.Common;
using Domain.Common.Entities;
using Domain.Common.Identity;
using Domain.Common.ValueObjects;
using Domain.Interfaces;
using Domain.Interfaces.Entities;
using Domain.Interfaces.ValueObjects;
namespace ANamespace;
public sealed class AClass : AggregateRootBase
{
    private AClass(IRecorder recorder, IIdentifierFactory idFactory) : base(recorder, idFactory)
    {
    }

    private AClass(IRecorder recorder, IIdentifierFactory idFactory, ISingleValueObject<string> identifier) : base(recorder, idFactory, identifier)
    {
    }

    protected override Result<Error> OnStateChanged(IDomainEvent @event, bool isReconstituting)
    {
        return Result.Ok;
    }

    public static AggregateRootFactory<AClass> Rehydrate()
    {
        return (identifier, container, properties) => new AClass(container.GetRequiredService<IRecorder>(),
            container.GetRequiredService<IIdentifierFactory>(), identifier);
    }

    public static AClass Create()
    {
        var root = new AClass(null!, null!);
        root.RaiseCreateEvent(EventOccurred.Create());
        return root;
    }
}
public sealed class EventOccurred : IDomainEvent
{
    public static EventOccurred Create()
    {
        return new EventOccurred
        {
            RootId = ""anid"",
            OccurredUtc = DateTime.UtcNow
        };
    }

    public required string RootId { get; set; }

    public required DateTime OccurredUtc { get; set; }
}";

                await Verify.NoDiagnosticExists<NonPlatform_DomainDrivenDesignAnalyzer>(input);
            }
        }

        [Trait("Category", "Unit.Tooling")]
        public class GivenRule011
        {
            [Fact]
            public async Task WhenCreateReturnsVoid_ThenAlerts()
            {
                const string input = @"
using System;
using Common;
using Domain.Common;
using Domain.Common.Entities;
using Domain.Common.Identity;
using Domain.Common.ValueObjects;
using Domain.Interfaces;
using Domain.Interfaces.Entities;
using Domain.Interfaces.ValueObjects;
namespace ANamespace;
public sealed class AClass : AggregateRootBase
{
    private AClass(IRecorder recorder, IIdentifierFactory idFactory) : base(recorder, idFactory)
    {
    }

    private AClass(IRecorder recorder, IIdentifierFactory idFactory, ISingleValueObject<string> identifier) : base(recorder, idFactory, identifier)
    {
    }

    protected override Result<Error> OnStateChanged(IDomainEvent @event, bool isReconstituting)
    {
        return Result.Ok;
    }

    public static AggregateRootFactory<AClass> Rehydrate()
    {
        return (identifier, container, properties) => new AClass(container.GetRequiredService<IRecorder>(),
            container.GetRequiredService<IIdentifierFactory>(), identifier);
    }

    public static void Create()
    {
        var root = new AClass(null!, null!);
        root.RaiseCreateEvent(EventOccurred.Create());
    }
}
public sealed class EventOccurred : IDomainEvent
{
    public static EventOccurred Create()
    {
        return new EventOccurred
        {
            RootId = ""anid"",
            OccurredUtc = DateTime.UtcNow
        };
    }

    public required string RootId { get; set; }

    public required DateTime OccurredUtc { get; set; }
}";

                await Verify.DiagnosticExists<NonPlatform_DomainDrivenDesignAnalyzer>(
                    NonPlatform_DomainDrivenDesignAnalyzer.Rule011, input, 33,
                    24,
                    "Create", "ANamespace.AClass or Common.Result<ANamespace.AClass, Common.Error>");
            }

            [Fact]
            public async Task WhenCreateReturnsOther_ThenAlerts()
            {
                const string input = @"
using System;
using Common;
using Domain.Common;
using Domain.Common.Entities;
using Domain.Common.Identity;
using Domain.Common.ValueObjects;
using Domain.Interfaces;
using Domain.Interfaces.Entities;
using Domain.Interfaces.ValueObjects;
namespace ANamespace;
public sealed class AClass : AggregateRootBase
{
    private AClass(IRecorder recorder, IIdentifierFactory idFactory) : base(recorder, idFactory)
    {
    }

    private AClass(IRecorder recorder, IIdentifierFactory idFactory, ISingleValueObject<string> identifier) : base(recorder, idFactory, identifier)
    {
    }

    protected override Result<Error> OnStateChanged(IDomainEvent @event, bool isReconstituting)
    {
        return Result.Ok;
    }

    public static AggregateRootFactory<AClass> Rehydrate()
    {
        return (identifier, container, properties) => new AClass(container.GetRequiredService<IRecorder>(),
            container.GetRequiredService<IIdentifierFactory>(), identifier);
    }

    public static string Create()
    {
        var root = new AClass(null!, null!);
        root.RaiseCreateEvent(EventOccurred.Create());
        return string.Empty;
    }
}
public sealed class EventOccurred : IDomainEvent
{
    public static EventOccurred Create()
    {
        return new EventOccurred
        {
            RootId = ""anid"",
            OccurredUtc = DateTime.UtcNow
        };
    }

    public required string RootId { get; set; }

    public required DateTime OccurredUtc { get; set; }
}";

                await Verify.DiagnosticExists<NonPlatform_DomainDrivenDesignAnalyzer>(
                    NonPlatform_DomainDrivenDesignAnalyzer.Rule011, input, 33,
                    26,
                    "Create", "ANamespace.AClass or Common.Result<ANamespace.AClass, Common.Error>");
            }

            [Fact]
            public async Task WhenCreateReturnsNakedClass_ThenNoAlert()
            {
                const string input = @"
using System;
using Common;
using Domain.Common;
using Domain.Common.Entities;
using Domain.Common.Identity;
using Domain.Common.ValueObjects;
using Domain.Interfaces;
using Domain.Interfaces.Entities;
using Domain.Interfaces.ValueObjects;
namespace ANamespace;
public sealed class AClass : AggregateRootBase
{
    private AClass(IRecorder recorder, IIdentifierFactory idFactory) : base(recorder, idFactory)
    {
    }

    private AClass(IRecorder recorder, IIdentifierFactory idFactory, ISingleValueObject<string> identifier) : base(recorder, idFactory, identifier)
    {
    }

    protected override Result<Error> OnStateChanged(IDomainEvent @event, bool isReconstituting)
    {
        return Result.Ok;
    }

    public static AggregateRootFactory<AClass> Rehydrate()
    {
        return (identifier, container, properties) => new AClass(container.GetRequiredService<IRecorder>(),
            container.GetRequiredService<IIdentifierFactory>(), identifier);
    }

    public static AClass Create()
    {
        var root = new AClass(null!, null!);
        root.RaiseCreateEvent(EventOccurred.Create());
        return root;
    }
}
public sealed class EventOccurred : IDomainEvent
{
    public static EventOccurred Create()
    {
        return new EventOccurred
        {
            RootId = ""anid"",
            OccurredUtc = DateTime.UtcNow
        };
    }

    public required string RootId { get; set; }

    public required DateTime OccurredUtc { get; set; }
}";

                await Verify.NoDiagnosticExists<NonPlatform_DomainDrivenDesignAnalyzer>(input);
            }

            [Fact]
            public async Task WhenCreateReturnsResultOfClassOrError_ThenNoAlert()
            {
                const string input = @"
using System;
using Common;
using Domain.Common;
using Domain.Common.Entities;
using Domain.Common.Identity;
using Domain.Common.ValueObjects;
using Domain.Interfaces;
using Domain.Interfaces.Entities;
using Domain.Interfaces.ValueObjects;
namespace ANamespace;
public sealed class AClass : AggregateRootBase
{
    private AClass(IRecorder recorder, IIdentifierFactory idFactory) : base(recorder, idFactory)
    {
    }

    private AClass(IRecorder recorder, IIdentifierFactory idFactory, ISingleValueObject<string> identifier) : base(recorder, idFactory, identifier)
    {
    }

    protected override Result<Error> OnStateChanged(IDomainEvent @event, bool isReconstituting)
    {
        return Result.Ok;
    }

    public static AggregateRootFactory<AClass> Rehydrate()
    {
        return (identifier, container, properties) => new AClass(container.GetRequiredService<IRecorder>(),
            container.GetRequiredService<IIdentifierFactory>(), identifier);
    }

    public static Result<AClass, Error> Create()
    {
        var root = new AClass(null!, null!);
        root.RaiseCreateEvent(EventOccurred.Create());
        return root;
    }
}
public sealed class EventOccurred : IDomainEvent
{
    public static EventOccurred Create()
    {
        return new EventOccurred
        {
            RootId = ""anid"",
            OccurredUtc = DateTime.UtcNow
        };
    }

    public required string RootId { get; set; }

    public required DateTime OccurredUtc { get; set; }
}";

                await Verify.NoDiagnosticExists<NonPlatform_DomainDrivenDesignAnalyzer>(input);
            }
        }

        [Trait("Category", "Unit.Tooling")]
        public class GivenRule012
        {
            [Fact]
            public async Task WhenCreateMethodIsEmpty_ThenAlerts()
            {
                const string input = @"
using Common;
using Domain.Common;
using Domain.Common.Entities;
using Domain.Common.Identity;
using Domain.Common.ValueObjects;
using Domain.Interfaces;
using Domain.Interfaces.Entities;
using Domain.Interfaces.ValueObjects;
namespace ANamespace;
public sealed class AClass : AggregateRootBase
{
    private AClass(IRecorder recorder, IIdentifierFactory idFactory) : base(recorder, idFactory)
    {
    }

    private AClass(IRecorder recorder, IIdentifierFactory idFactory, ISingleValueObject<string> identifier) : base(recorder, idFactory, identifier)
    {
    }

    protected override Result<Error> OnStateChanged(IDomainEvent @event, bool isReconstituting)
    {
        return Result.Ok;
    }

    public static AggregateRootFactory<AClass> Rehydrate()
    {
        return (identifier, container, properties) => new AClass(container.GetRequiredService<IRecorder>(),
            container.GetRequiredService<IIdentifierFactory>(), identifier);
    }

    public static AClass Create()
    {
        return null!;
    }
}";

                await Verify.DiagnosticExists<NonPlatform_DomainDrivenDesignAnalyzer>(
                    NonPlatform_DomainDrivenDesignAnalyzer.Rule012, input, 32,
                    26,
                    "Create", NonPlatform_DomainDrivenDesignAnalyzer.ConstructorMethodCall);
            }

            [Fact]
            public async Task WhenCreateCallsRaiseEvent_ThenNoAlert()
            {
                const string input = @"
using System;
using Common;
using Domain.Common;
using Domain.Common.Entities;
using Domain.Common.Identity;
using Domain.Common.ValueObjects;
using Domain.Interfaces;
using Domain.Interfaces.Entities;
using Domain.Interfaces.ValueObjects;
namespace ANamespace;
public sealed class AClass : AggregateRootBase
{
    private AClass(IRecorder recorder, IIdentifierFactory idFactory) : base(recorder, idFactory)
    {
    }

    private AClass(IRecorder recorder, IIdentifierFactory idFactory, ISingleValueObject<string> identifier) : base(recorder, idFactory, identifier)
    {
    }

    protected override Result<Error> OnStateChanged(IDomainEvent @event, bool isReconstituting)
    {
        return Result.Ok;
    }

    public static AggregateRootFactory<AClass> Rehydrate()
    {
        return (identifier, container, properties) => new AClass(container.GetRequiredService<IRecorder>(),
            container.GetRequiredService<IIdentifierFactory>(), identifier);
    }

    public static AClass Create()
    {
        var root = new AClass(null!, null!);
        root.RaiseCreateEvent(EventOccurred.Create());
        return root;
    }
}
public sealed class EventOccurred : IDomainEvent
{
    public static EventOccurred Create()
    {
        return new EventOccurred
        {
            RootId = ""anid"",
            OccurredUtc = DateTime.UtcNow
        };
    }

    public required string RootId { get; set; }

    public required DateTime OccurredUtc { get; set; }
}";

                await Verify.NoDiagnosticExists<NonPlatform_DomainDrivenDesignAnalyzer>(input);
            }
        }

        [Trait("Category", "Unit.Tooling")]
        public class GivenRule013
        {
            [Fact]
            public async Task WhenNonPrivateConstructor_ThenAlerts()
            {
                const string input = @"
using System;
using Common;
using Domain.Common;
using Domain.Common.Entities;
using Domain.Common.Identity;
using Domain.Common.ValueObjects;
using Domain.Interfaces;
using Domain.Interfaces.Entities;
using Domain.Interfaces.ValueObjects;
namespace ANamespace;
public sealed class AClass : AggregateRootBase
{
    public AClass(IRecorder recorder, IIdentifierFactory idFactory) : base(recorder, idFactory)
    {
    }

    private AClass(IRecorder recorder, IIdentifierFactory idFactory, ISingleValueObject<string> identifier) : base(recorder, idFactory, identifier)
    {
    }

    protected override Result<Error> OnStateChanged(IDomainEvent @event, bool isReconstituting)
    {
        return Result.Ok;
    }

    public static AggregateRootFactory<AClass> Rehydrate()
    {
        return (identifier, container, properties) => new AClass(container.GetRequiredService<IRecorder>(),
            container.GetRequiredService<IIdentifierFactory>(), identifier);
    }

    public static AClass Create()
    {
        var root = new AClass(null!, null!);
        root.RaiseCreateEvent(EventOccurred.Create());
        return root;
    }
}
public sealed class EventOccurred : IDomainEvent
{
    public static EventOccurred Create()
    {
        return new EventOccurred
        {
            RootId = ""anid"",
            OccurredUtc = DateTime.UtcNow
        };
    }

    public required string RootId { get; set; }

    public required DateTime OccurredUtc { get; set; }
}";

                await Verify.DiagnosticExists<NonPlatform_DomainDrivenDesignAnalyzer>(
                    NonPlatform_DomainDrivenDesignAnalyzer.Rule013, input, 14,
                    12, "AClass");
            }

            [Fact]
            public async Task WhenPrivateConstructor_ThenNoAlert()
            {
                const string input = @"
using System;
using Common;
using Domain.Common;
using Domain.Common.Entities;
using Domain.Common.Identity;
using Domain.Common.ValueObjects;
using Domain.Interfaces;
using Domain.Interfaces.Entities;
using Domain.Interfaces.ValueObjects;
namespace ANamespace;
public sealed class AClass : AggregateRootBase
{
    private AClass(IRecorder recorder, IIdentifierFactory idFactory) : base(recorder, idFactory)
    {
    }

    private AClass(IRecorder recorder, IIdentifierFactory idFactory, ISingleValueObject<string> identifier) : base(recorder, idFactory, identifier)
    {
    }

    protected override Result<Error> OnStateChanged(IDomainEvent @event, bool isReconstituting)
    {
        return Result.Ok;
    }

    public static AggregateRootFactory<AClass> Rehydrate()
    {
        return (identifier, container, properties) => new AClass(container.GetRequiredService<IRecorder>(),
            container.GetRequiredService<IIdentifierFactory>(), identifier);
    }

    public static AClass Create()
    {
        var root = new AClass(null!, null!);
        root.RaiseCreateEvent(EventOccurred.Create());
        return root;
    }
}
public sealed class EventOccurred : IDomainEvent
{
    public static EventOccurred Create()
    {
        return new EventOccurred
        {
            RootId = ""anid"",
            OccurredUtc = DateTime.UtcNow
        };
    }

    public required string RootId { get; set; }

    public required DateTime OccurredUtc { get; set; }
}";

                await Verify.NoDiagnosticExists<NonPlatform_DomainDrivenDesignAnalyzer>(input);
            }
        }

        [Trait("Category", "Unit.Tooling")]
        public class GivenRule014
        {
            [Fact]
            public async Task WhenMissingRehydrateMethod_ThenAlerts()
            {
                const string input = @"
using System;
using Common;
using Domain.Common.Entities;
using Domain.Common.Identity;
using Domain.Interfaces.Entities;
namespace ANamespace;
public sealed class AClass : AggregateRootBase
{
    private AClass(IRecorder recorder, IIdentifierFactory idFactory) : base(recorder, idFactory)
    {
    }

    protected override Result<Error> OnStateChanged(IDomainEvent @event, bool isReconstituting)
    {
        return Result.Ok;
    }

    public static AClass Create()
    {
        var root = new AClass(null!, null!);
        root.RaiseCreateEvent(EventOccurred.Create());
        return root;
    }
}
public sealed class EventOccurred : IDomainEvent
{
    public static EventOccurred Create()
    {
        return new EventOccurred
        {
            RootId = ""anid"",
            OccurredUtc = DateTime.UtcNow
        };
    }

    public required string RootId { get; set; }

    public required DateTime OccurredUtc { get; set; }
}";

                await Verify.DiagnosticExists<NonPlatform_DomainDrivenDesignAnalyzer>(
                    NonPlatform_DomainDrivenDesignAnalyzer.Rule014, input, 8,
                    21,
                    "AClass");
            }

            [Fact]
            public async Task WhenHasRehydrateMethod_ThenNoAlert()
            {
                const string input = @"
using System;
using Common;
using Domain.Common;
using Domain.Common.Entities;
using Domain.Common.Identity;
using Domain.Common.ValueObjects;
using Domain.Interfaces;
using Domain.Interfaces.Entities;
using Domain.Interfaces.ValueObjects;
using QueryAny;
namespace ANamespace;
public sealed class AClass : AggregateRootBase
{
    private AClass(IRecorder recorder, IIdentifierFactory idFactory) : base(recorder, idFactory)
    {
    }

    private AClass(IRecorder recorder, IIdentifierFactory idFactory, ISingleValueObject<string> identifier) : base(recorder, idFactory, identifier)
    {
    }

    protected override Result<Error> OnStateChanged(IDomainEvent @event, bool isReconstituting)
    {
        return Result.Ok;
    }

    public static AClass Create()
    {
        var root = new AClass(null!, null!);
        root.RaiseCreateEvent(EventOccurred.Create());
        return root;
    }

    public static AggregateRootFactory<AClass> Rehydrate()
    {
        return (identifier, container, properties) => new AClass(container.GetRequiredService<IRecorder>(),
            container.GetRequiredService<IIdentifierFactory>(), identifier);
    }
}
public sealed class EventOccurred : IDomainEvent
{
    public static EventOccurred Create()
    {
        return new EventOccurred
        {
            RootId = ""anid"",
            OccurredUtc = DateTime.UtcNow
        };
    }

    public required string RootId { get; set; }

    public required DateTime OccurredUtc { get; set; }
}";

                await Verify.NoDiagnosticExists<NonPlatform_DomainDrivenDesignAnalyzer>(input);
            }
        }

        [Trait("Category", "Unit.Tooling")]
        public class GivenRule015
        {
            [Fact]
            public async Task WhenDehydratableAndMissingDehydrateMethod_ThenAlerts()
            {
                const string input = @"
using System;
using Common;
using Domain.Common;
using Domain.Common.Entities;
using Domain.Common.Identity;
using Domain.Common.ValueObjects;
using Domain.Interfaces;
using Domain.Interfaces.Entities;
using Domain.Interfaces.ValueObjects;
using QueryAny;
namespace ANamespace;
[EntityName(""AClass"")]
public sealed class AClass : AggregateRootBase
{
    private AClass(IRecorder recorder, IIdentifierFactory idFactory) : base(recorder, idFactory)
    {
    }

    private AClass(IRecorder recorder, IIdentifierFactory idFactory, ISingleValueObject<string> identifier) : base(recorder, idFactory, identifier)
    {
    }

    protected override Result<Error> OnStateChanged(IDomainEvent @event, bool isReconstituting)
    {
        return Result.Ok;
    }

    public static AClass Create()
    {
        var root = new AClass(null!, null!);
        root.RaiseCreateEvent(EventOccurred.Create());
        return root;
    }

    public static AggregateRootFactory<AClass> Rehydrate()
    {
        return (identifier, container, properties) => new AClass(container.GetRequiredService<IRecorder>(),
            container.GetRequiredService<IIdentifierFactory>(), identifier);
    }
}
public sealed class EventOccurred : IDomainEvent
{
    public static EventOccurred Create()
    {
        return new EventOccurred
        {
            RootId = ""anid"",
            OccurredUtc = DateTime.UtcNow
        };
    }

    public required string RootId { get; set; }

    public required DateTime OccurredUtc { get; set; }
}";

                await Verify.DiagnosticExists<NonPlatform_DomainDrivenDesignAnalyzer>(
                    NonPlatform_DomainDrivenDesignAnalyzer.Rule015, input, 14,
                    21, "AClass");
            }

            [Fact]
            public async Task WhenDehydratableAndHasDehydrateMethod_ThenNoAlert()
            {
                const string input = @"
using System;
using System.Collections.Generic;
using Common;
using Domain.Common;
using Domain.Common.Entities;
using Domain.Common.Identity;
using Domain.Common.ValueObjects;
using Domain.Interfaces;
using Domain.Interfaces.Entities;
using Domain.Interfaces.ValueObjects;
using QueryAny;
namespace ANamespace;
[EntityName(""AClass"")]
public sealed class AClass : AggregateRootBase
{
    private AClass(IRecorder recorder, IIdentifierFactory idFactory) : base(recorder, idFactory)
    {
    }

    private AClass(IRecorder recorder, IIdentifierFactory idFactory, ISingleValueObject<string> identifier) : base(recorder, idFactory, identifier)
    {
    }

    protected override Result<Error> OnStateChanged(IDomainEvent @event, bool isReconstituting)
    {
        return Result.Ok;
    }

    public static AClass Create()
    {
        var root = new AClass(null!, null!);
        root.RaiseCreateEvent(EventOccurred.Create());
        return root;
    }

    public static AggregateRootFactory<AClass> Rehydrate()
    {
        return (identifier, container, properties) => new AClass(container.GetRequiredService<IRecorder>(),
            container.GetRequiredService<IIdentifierFactory>(), identifier);
    }

    public override HydrationProperties Dehydrate()
    {
        return base.Dehydrate();
    }
}
public sealed class EventOccurred : IDomainEvent
{
    public static EventOccurred Create()
    {
        return new EventOccurred
        {
            RootId = ""anid"",
            OccurredUtc = DateTime.UtcNow
        };
    }

    public required string RootId { get; set; }

    public required DateTime OccurredUtc { get; set; }
}";

                await Verify.NoDiagnosticExists<NonPlatform_DomainDrivenDesignAnalyzer>(input);
            }
        }

        [Trait("Category", "Unit.Tooling")]
        public class GivenRule016
        {
            [Fact]
            public async Task WhenDehydratableAndMissingEntityNameAttribute_ThenAlerts()
            {
                const string input = @"
using System;
using System.Collections.Generic;
using Common;
using Domain.Common;
using Domain.Common.Entities;
using Domain.Common.Identity;
using Domain.Common.ValueObjects;
using Domain.Interfaces;
using Domain.Interfaces.Entities;
using Domain.Interfaces.ValueObjects;
using QueryAny;
namespace ANamespace;
public sealed class AClass : AggregateRootBase
{
    private AClass(IRecorder recorder, IIdentifierFactory idFactory) : base(recorder, idFactory)
    {
    }

    private AClass(IRecorder recorder, IIdentifierFactory idFactory, ISingleValueObject<string> identifier) : base(recorder, idFactory, identifier)
    {
    }

    protected override Result<Error> OnStateChanged(IDomainEvent @event, bool isReconstituting)
    {
        return Result.Ok;
    }

    public static AClass Create()
    {
        var root = new AClass(null!, null!);
        root.RaiseCreateEvent(EventOccurred.Create());
        return root;
    }

    public static AggregateRootFactory<AClass> Rehydrate()
    {
        return (identifier, container, properties) => new AClass(container.GetRequiredService<IRecorder>(),
            container.GetRequiredService<IIdentifierFactory>(), identifier);
    }

    public override HydrationProperties Dehydrate()
    {
        return base.Dehydrate();
    }
}
public sealed class EventOccurred : IDomainEvent
{
    public static EventOccurred Create()
    {
        return new EventOccurred
        {
            RootId = ""anid"",
            OccurredUtc = DateTime.UtcNow
        };
    }

    public required string RootId { get; set; }

    public required DateTime OccurredUtc { get; set; }
}";

                await Verify.DiagnosticExists<NonPlatform_DomainDrivenDesignAnalyzer>(
                    NonPlatform_DomainDrivenDesignAnalyzer.Rule016, input, 14,
                    21,
                    "AClass");
            }

            [Fact]
            public async Task WhenDehydratableAndHasEntityNameAttribute_ThenNoAlert()
            {
                const string input = @"
using System;
using System.Collections.Generic;
using Common;
using Domain.Common;
using Domain.Common.Entities;
using Domain.Common.Identity;
using Domain.Common.ValueObjects;
using Domain.Interfaces;
using Domain.Interfaces.Entities;
using Domain.Interfaces.ValueObjects;
using QueryAny;
namespace ANamespace;
[EntityName(""AClass"")]
public sealed class AClass : AggregateRootBase
{
    private AClass(IRecorder recorder, IIdentifierFactory idFactory) : base(recorder, idFactory)
    {
    }

    private AClass(IRecorder recorder, IIdentifierFactory idFactory, ISingleValueObject<string> identifier) : base(recorder, idFactory, identifier)
    {
    }

    protected override Result<Error> OnStateChanged(IDomainEvent @event, bool isReconstituting)
    {
        return Result.Ok;
    }

    public static AClass Create()
    {
        var root = new AClass(null!, null!);
        root.RaiseCreateEvent(EventOccurred.Create());
        return root;
    }

    public static AggregateRootFactory<AClass> Rehydrate()
    {
        return (identifier, container, properties) => new AClass(container.GetRequiredService<IRecorder>(),
            container.GetRequiredService<IIdentifierFactory>(), identifier);
    }

    public override HydrationProperties Dehydrate()
    {
        return base.Dehydrate();
    }
}
public sealed class EventOccurred : IDomainEvent
{
    public static EventOccurred Create()
    {
        return new EventOccurred
        {
            RootId = ""anid"",
            OccurredUtc = DateTime.UtcNow
        };
    }

    public required string RootId { get; set; }

    public required DateTime OccurredUtc { get; set; }
}";

                await Verify.NoDiagnosticExists<NonPlatform_DomainDrivenDesignAnalyzer>(input);
            }
        }

        [Trait("Category", "Unit.Tooling")]
        public class GivenRule017
        {
            [Fact]
            public async Task WhenPropertyHasPublicSetter_ThenAlerts()
            {
                const string input = @"
using System;
using Common;
using Domain.Common;
using Domain.Common.Entities;
using Domain.Common.Identity;
using Domain.Common.ValueObjects;
using Domain.Interfaces;
using Domain.Interfaces.Entities;
using Domain.Interfaces.ValueObjects;
namespace ANamespace;
public sealed class AClass : AggregateRootBase
{
    private AClass(IRecorder recorder, IIdentifierFactory idFactory) : base(recorder, idFactory)
    {
    }

    private AClass(IRecorder recorder, IIdentifierFactory idFactory, ISingleValueObject<string> identifier) : base(recorder, idFactory, identifier)
    {
    }

    protected override Result<Error> OnStateChanged(IDomainEvent @event, bool isReconstituting)
    {
        return Result.Ok;
    }

    public static AClass Create()
    {
        var root = new AClass(null!, null!);
        root.RaiseCreateEvent(EventOccurred.Create());
        return root;
    }

    public static AggregateRootFactory<AClass> Rehydrate()
    {
        return (identifier, container, properties) => new AClass(container.GetRequiredService<IRecorder>(),
            container.GetRequiredService<IIdentifierFactory>(), identifier);
    }

    public string AProperty { get; set; }
}
public sealed class EventOccurred : IDomainEvent
{
    public static EventOccurred Create()
    {
        return new EventOccurred
        {
            RootId = ""anid"",
            OccurredUtc = DateTime.UtcNow
        };
    }

    public required string RootId { get; set; }

    public required DateTime OccurredUtc { get; set; }
}";

                await Verify.DiagnosticExists<NonPlatform_DomainDrivenDesignAnalyzer>(
                    NonPlatform_DomainDrivenDesignAnalyzer.Rule017, input, 40,
                    19, "AProperty");
            }

            [Fact]
            public async Task WhenPropertyHasPrivateSetter_ThenNoAlert()
            {
                const string input = @"
using System;
using Common;
using Domain.Common;
using Domain.Common.Entities;
using Domain.Common.Identity;
using Domain.Common.ValueObjects;
using Domain.Interfaces;
using Domain.Interfaces.Entities;
using Domain.Interfaces.ValueObjects;
namespace ANamespace;
public sealed class AClass : AggregateRootBase
{
    private AClass(IRecorder recorder, IIdentifierFactory idFactory) : base(recorder, idFactory)
    {
    }

    private AClass(IRecorder recorder, IIdentifierFactory idFactory, ISingleValueObject<string> identifier) : base(recorder, idFactory, identifier)
    {
    }

    protected override Result<Error> OnStateChanged(IDomainEvent @event, bool isReconstituting)
    {
        return Result.Ok;
    }

    public static AClass Create()
    {
        var root = new AClass(null!, null!);
        root.RaiseCreateEvent(EventOccurred.Create());
        return root;
    }

    public static AggregateRootFactory<AClass> Rehydrate()
    {
        return (identifier, container, properties) => new AClass(container.GetRequiredService<IRecorder>(),
            container.GetRequiredService<IIdentifierFactory>(), identifier);
    }

    public string AProperty { get; private set; }
}
public sealed class EventOccurred : IDomainEvent
{
    public static EventOccurred Create()
    {
        return new EventOccurred
        {
            RootId = ""anid"",
            OccurredUtc = DateTime.UtcNow
        };
    }

    public required string RootId { get; set; }

    public required DateTime OccurredUtc { get; set; }
}";

                await Verify.NoDiagnosticExists<NonPlatform_DomainDrivenDesignAnalyzer>(input);
            }

            [Fact]
            public async Task WhenPropertyHasArrowFunction_ThenNoAlert()
            {
                const string input = @"
using System;
using Common;
using Domain.Common;
using Domain.Common.Entities;
using Domain.Common.Identity;
using Domain.Common.ValueObjects;
using Domain.Interfaces;
using Domain.Interfaces.Entities;
using Domain.Interfaces.ValueObjects;
namespace ANamespace;
public sealed class AClass : AggregateRootBase
{
    private AClass(IRecorder recorder, IIdentifierFactory idFactory) : base(recorder, idFactory)
    {
    }

    private AClass(IRecorder recorder, IIdentifierFactory idFactory, ISingleValueObject<string> identifier) : base(recorder, idFactory, identifier)
    {
    }

    protected override Result<Error> OnStateChanged(IDomainEvent @event, bool isReconstituting)
    {
        return Result.Ok;
    }

    public static AClass Create()
    {
        var root = new AClass(null!, null!);
        root.RaiseCreateEvent(EventOccurred.Create());
        return root;
    }

    public static AggregateRootFactory<AClass> Rehydrate()
    {
        return (identifier, container, properties) => new AClass(container.GetRequiredService<IRecorder>(),
            container.GetRequiredService<IIdentifierFactory>(), identifier);
    }

    public string AProperty => string.Empty;
}
public sealed class EventOccurred : IDomainEvent
{
    public static EventOccurred Create()
    {
        return new EventOccurred
        {
            RootId = ""anid"",
            OccurredUtc = DateTime.UtcNow
        };
    }

    public required string RootId { get; set; }

    public required DateTime OccurredUtc { get; set; }
}";

                await Verify.NoDiagnosticExists<NonPlatform_DomainDrivenDesignAnalyzer>(input);
            }

            [Fact]
            public async Task WhenPropertyHasNoSetter_ThenNoAlert()
            {
                const string input = @"
using System;
using Common;
using Domain.Common;
using Domain.Common.Entities;
using Domain.Common.Identity;
using Domain.Common.ValueObjects;
using Domain.Interfaces;
using Domain.Interfaces.Entities;
using Domain.Interfaces.ValueObjects;
namespace ANamespace;
public sealed class AClass : AggregateRootBase
{
    private AClass(IRecorder recorder, IIdentifierFactory idFactory) : base(recorder, idFactory)
    {
    }

    private AClass(IRecorder recorder, IIdentifierFactory idFactory, ISingleValueObject<string> identifier) : base(recorder, idFactory, identifier)
    {
    }

    protected override Result<Error> OnStateChanged(IDomainEvent @event, bool isReconstituting)
    {
        return Result.Ok;
    }

    public static AClass Create()
    {
        var root = new AClass(null!, null!);
        root.RaiseCreateEvent(EventOccurred.Create());
        return root;
    }

    public static AggregateRootFactory<AClass> Rehydrate()
    {
        return (identifier, container, properties) => new AClass(container.GetRequiredService<IRecorder>(),
            container.GetRequiredService<IIdentifierFactory>(), identifier);
    }

    public string AProperty { get; }
}
public sealed class EventOccurred : IDomainEvent
{
    public static EventOccurred Create()
    {
        return new EventOccurred
        {
            RootId = ""anid"",
            OccurredUtc = DateTime.UtcNow
        };
    }

    public required string RootId { get; set; }

    public required DateTime OccurredUtc { get; set; }
}";

                await Verify.NoDiagnosticExists<NonPlatform_DomainDrivenDesignAnalyzer>(input);
            }
        }

        [Trait("Category", "Unit.Tooling")]
        public class GivenRule018
        {
            [Fact]
            public async Task WhenClassIsNotSealed_ThenAlerts()
            {
                const string input = @"
using System;
using Common;
using Domain.Common;
using Domain.Common.Entities;
using Domain.Common.Identity;
using Domain.Common.ValueObjects;
using Domain.Interfaces;
using Domain.Interfaces.Entities;
using Domain.Interfaces.ValueObjects;
namespace ANamespace;
public class AClass : AggregateRootBase
{
    private AClass(IRecorder recorder, IIdentifierFactory idFactory) : base(recorder, idFactory)
    {
    }

    private AClass(IRecorder recorder, IIdentifierFactory idFactory, ISingleValueObject<string> identifier) : base(recorder, idFactory, identifier)
    {
    }

    protected override Result<Error> OnStateChanged(IDomainEvent @event, bool isReconstituting)
    {
        return Result.Ok;
    }

    public static AClass Create()
    {
        var root = new AClass(null!, null!);
        root.RaiseCreateEvent(EventOccurred.Create());
        return root;
    }

    public static AggregateRootFactory<AClass> Rehydrate()
    {
        return (identifier, container, properties) => new AClass(container.GetRequiredService<IRecorder>(),
            container.GetRequiredService<IIdentifierFactory>(), identifier);
    }

    public string AProperty { get; }
}
public sealed class EventOccurred : IDomainEvent
{
    public static EventOccurred Create()
    {
        return new EventOccurred
        {
            RootId = ""anid"",
            OccurredUtc = DateTime.UtcNow
        };
    }

    public required string RootId { get; set; }

    public required DateTime OccurredUtc { get; set; }
}";

                await Verify.DiagnosticExists<NonPlatform_DomainDrivenDesignAnalyzer>(
                    NonPlatform_DomainDrivenDesignAnalyzer.Rule018, input, 12,
                    14, "AClass");
            }

            [Fact]
            public async Task WhenClassIsSealed_ThenNoAlert()
            {
                const string input = @"
using System;
using Common;
using Domain.Common;
using Domain.Common.Entities;
using Domain.Common.Identity;
using Domain.Common.ValueObjects;
using Domain.Interfaces;
using Domain.Interfaces.Entities;
using Domain.Interfaces.ValueObjects;
namespace ANamespace;
public sealed class AClass : AggregateRootBase
{
    private AClass(IRecorder recorder, IIdentifierFactory idFactory) : base(recorder, idFactory)
    {
    }

    private AClass(IRecorder recorder, IIdentifierFactory idFactory, ISingleValueObject<string> identifier) : base(recorder, idFactory, identifier)
    {
    }

    protected override Result<Error> OnStateChanged(IDomainEvent @event, bool isReconstituting)
    {
        return Result.Ok;
    }

    public static AClass Create()
    {
        var root = new AClass(null!, null!);
        root.RaiseCreateEvent(EventOccurred.Create());
        return root;
    }

    public static AggregateRootFactory<AClass> Rehydrate()
    {
        return (identifier, container, properties) => new AClass(container.GetRequiredService<IRecorder>(),
            container.GetRequiredService<IIdentifierFactory>(), identifier);
    }

    public string AProperty { get; }
}
public sealed class EventOccurred : IDomainEvent
{
    public static EventOccurred Create()
    {
        return new EventOccurred
        {
            RootId = ""anid"",
            OccurredUtc = DateTime.UtcNow
        };
    }

    public required string RootId { get; set; }

    public required DateTime OccurredUtc { get; set; }
}";

                await Verify.NoDiagnosticExists<NonPlatform_DomainDrivenDesignAnalyzer>(input);
            }
        }
    }

    [UsedImplicitly]
    public class GivenAnEntity
    {
        [Trait("Category", "Unit.Tooling")]
        public class GivenRule020
        {
            [Fact]
            public async Task WhenHasNoCreateMethod_ThenAlerts()
            {
                const string input = @"
using Common;
using Domain.Common;
using Domain.Common.Entities;
using Domain.Common.Identity;
using Domain.Common.ValueObjects;
using Domain.Interfaces.Entities;
namespace ANamespace;
public sealed class AClass : EntityBase
{
    private AClass(IRecorder recorder, IIdentifierFactory idFactory, RootEventHandler rootEventHandler) : base(recorder, idFactory, rootEventHandler)
    {
    }

    protected override Result<Error> OnStateChanged(IDomainEvent @event)
    {
        return Result.Ok;
    }
}";

                await Verify.DiagnosticExists<NonPlatform_DomainDrivenDesignAnalyzer>(
                    NonPlatform_DomainDrivenDesignAnalyzer.Rule020, input, 9,
                    21, "AClass");
            }

            [Fact]
            public async Task WhenHasCreateInstanceMethod_ThenAlerts()
            {
                const string input = @"
using Common;
using Domain.Common;
using Domain.Common.Entities;
using Domain.Common.Identity;
using Domain.Common.ValueObjects;
using Domain.Interfaces.Entities;
namespace ANamespace;
public sealed class AClass : EntityBase
{
    private AClass(IRecorder recorder, IIdentifierFactory idFactory, RootEventHandler rootEventHandler) : base(recorder, idFactory, rootEventHandler)
    {
    }

    protected override Result<Error> OnStateChanged(IDomainEvent @event)
    {
        return Result.Ok;
    }

    public void Create()
    {
    }
}";

                await Verify.DiagnosticExists<NonPlatform_DomainDrivenDesignAnalyzer>(
                    NonPlatform_DomainDrivenDesignAnalyzer.Rule020, input, 9,
                    21,
                    "AClass");
            }

            [Fact]
            public async Task WhenHasAtLeastOneCreateStaticMethod_ThenNoAlert()
            {
                const string input = @"
using System;
using Common;
using Domain.Common;
using Domain.Common.Entities;
using Domain.Common.Identity;
using Domain.Common.ValueObjects;
using Domain.Interfaces.Entities;
namespace ANamespace;
public sealed class AClass : EntityBase
{
    private AClass(IRecorder recorder, IIdentifierFactory idFactory, RootEventHandler rootEventHandler) : base(recorder, idFactory, rootEventHandler)
    {
    }

    protected override Result<Error> OnStateChanged(IDomainEvent @event)
    {
        return Result.Ok;
    }

    public static AClass Create()
    {
        return new AClass(null!, null!, null!);
    }
}";

                await Verify.NoDiagnosticExists<NonPlatform_DomainDrivenDesignAnalyzer>(input);
            }
        }

        [Trait("Category", "Unit.Tooling")]
        public class GivenRule021
        {
            [Fact]
            public async Task WhenCreateReturnsVoid_ThenAlerts()
            {
                const string input = @"
using System;
using Common;
using Domain.Common;
using Domain.Common.Entities;
using Domain.Common.Identity;
using Domain.Common.ValueObjects;
using Domain.Interfaces.Entities;
namespace ANamespace;
public sealed class AClass : EntityBase
{
    private AClass(IRecorder recorder, IIdentifierFactory idFactory, RootEventHandler rootEventHandler) : base(recorder, idFactory, rootEventHandler)
    {
    }

    protected override Result<Error> OnStateChanged(IDomainEvent @event)
    {
        return Result.Ok;
    }

    public static void Create()
    {
    }
}";

                await Verify.DiagnosticExists<NonPlatform_DomainDrivenDesignAnalyzer>(
                    NonPlatform_DomainDrivenDesignAnalyzer.Rule021, input, 21,
                    24, "Create", "ANamespace.AClass or Common.Result<ANamespace.AClass, Common.Error>");
            }

            [Fact]
            public async Task WhenCreateReturnsOther_ThenAlerts()
            {
                const string input = @"
using System;
using Common;
using Domain.Common;
using Domain.Common.Entities;
using Domain.Common.Identity;
using Domain.Common.ValueObjects;
using Domain.Interfaces.Entities;
namespace ANamespace;
public sealed class AClass : EntityBase
{
    private AClass(IRecorder recorder, IIdentifierFactory idFactory, RootEventHandler rootEventHandler) : base(recorder, idFactory, rootEventHandler)
    {
    }

    protected override Result<Error> OnStateChanged(IDomainEvent @event)
    {
        return Result.Ok;
    }

    public static string Create()
    {
        return string.Empty;
    }
}";

                await Verify.DiagnosticExists<NonPlatform_DomainDrivenDesignAnalyzer>(
                    NonPlatform_DomainDrivenDesignAnalyzer.Rule021, input, 21,
                    26, "Create", "ANamespace.AClass or Common.Result<ANamespace.AClass, Common.Error>");
            }

            [Fact]
            public async Task WhenCreateReturnsNakedClass_ThenNoAlert()
            {
                const string input = @"
using System;
using Common;
using Domain.Common;
using Domain.Common.Entities;
using Domain.Common.Identity;
using Domain.Common.ValueObjects;
using Domain.Interfaces.Entities;
namespace ANamespace;
public sealed class AClass : EntityBase
{
    private AClass(IRecorder recorder, IIdentifierFactory idFactory, RootEventHandler rootEventHandler) : base(recorder, idFactory, rootEventHandler)
    {
    }

    protected override Result<Error> OnStateChanged(IDomainEvent @event)
    {
        return Result.Ok;
    }

    public static AClass Create()
    {
        return new AClass(null!, null!, null!);
    }
}";

                await Verify.NoDiagnosticExists<NonPlatform_DomainDrivenDesignAnalyzer>(input);
            }

            [Fact]
            public async Task WhenCreateReturnsResultOfClassOrError_ThenNoAlert()
            {
                const string input = @"
using System;
using Common;
using Domain.Common;
using Domain.Common.Entities;
using Domain.Common.Identity;
using Domain.Common.ValueObjects;
using Domain.Interfaces.Entities;
namespace ANamespace;
public sealed class AClass : EntityBase
{
    private AClass(IRecorder recorder, IIdentifierFactory idFactory, RootEventHandler rootEventHandler) : base(recorder, idFactory, rootEventHandler)
    {
    }

    protected override Result<Error> OnStateChanged(IDomainEvent @event)
    {
        return Result.Ok;
    }

    public static Result<AClass, Error> Create()
    {
        return new AClass(null!, null!, null!);
    }
}";

                await Verify.NoDiagnosticExists<NonPlatform_DomainDrivenDesignAnalyzer>(input);
            }
        }

        [Trait("Category", "Unit.Tooling")]
        public class GivenRule022
        {
            [Fact]
            public async Task WhenNonPrivateConstructor_ThenAlerts()
            {
                const string input = @"
using System;
using Common;
using Domain.Common;
using Domain.Common.Entities;
using Domain.Common.Identity;
using Domain.Common.ValueObjects;
using Domain.Interfaces.Entities;
namespace ANamespace;
public sealed class AClass : EntityBase
{
    public AClass(IRecorder recorder, IIdentifierFactory idFactory, RootEventHandler rootEventHandler) : base(recorder, idFactory, rootEventHandler)
    {
    }

    protected override Result<Error> OnStateChanged(IDomainEvent @event)
    {
        return Result.Ok;
    }

    public static AClass Create()
    {
        return new AClass(null!, null!, null!);
    }
}";

                await Verify.DiagnosticExists<NonPlatform_DomainDrivenDesignAnalyzer>(
                    NonPlatform_DomainDrivenDesignAnalyzer.Rule022, input, 12,
                    12, "AClass");
            }

            [Fact]
            public async Task WhenPrivateConstructor_ThenNoAlert()
            {
                const string input = @"
using System;
using Common;
using Domain.Common;
using Domain.Common.Entities;
using Domain.Common.Identity;
using Domain.Common.ValueObjects;
using Domain.Interfaces.Entities;
namespace ANamespace;
public sealed class AClass : EntityBase
{
    private AClass(IRecorder recorder, IIdentifierFactory idFactory, RootEventHandler rootEventHandler) : base(recorder, idFactory, rootEventHandler)
    {
    }

    protected override Result<Error> OnStateChanged(IDomainEvent @event)
    {
        return Result.Ok;
    }

    public static AClass Create()
    {
        return new AClass(null!, null!, null!);
    }
}";

                await Verify.NoDiagnosticExists<NonPlatform_DomainDrivenDesignAnalyzer>(input);
            }
        }

        [Trait("Category", "Unit.Tooling")]
        public class GivenRule023
        {
            [Fact]
            public async Task WhenDehydratableAndMissingRehydrateMethod_ThenAlerts()
            {
                const string input = @"
using System;
using System.Collections.Generic;
using Common;
using Domain.Common;
using Domain.Common.Entities;
using Domain.Common.Identity;
using Domain.Common.ValueObjects;
using Domain.Interfaces;
using Domain.Interfaces.Entities;
using Domain.Interfaces.Services;
using Domain.Interfaces.ValueObjects;
using QueryAny;
namespace ANamespace;
[EntityName(""AClass"")]
public sealed class AClass : EntityBase
{
    private AClass(IRecorder recorder, IIdentifierFactory idFactory, RootEventHandler rootEventHandler) : base(recorder, idFactory, rootEventHandler)
    {
    }

    private AClass(ISingleValueObject<string> identifier, IDependencyContainer container, HydrationProperties rehydratingProperties) : base(identifier, container, rehydratingProperties)
    {
    }

    protected override Result<Error> OnStateChanged(IDomainEvent @event)
    {
        return Result.Ok;
    }

    public static AClass Create(IRecorder recorder, IIdentifierFactory idFactory, RootEventHandler rootEventHandler)
    {
        return new AClass(recorder, idFactory, rootEventHandler);
    }

    public override HydrationProperties Dehydrate()
    {
        return base.Dehydrate();
    }
}";

                await Verify.DiagnosticExists<NonPlatform_DomainDrivenDesignAnalyzer>(
                    NonPlatform_DomainDrivenDesignAnalyzer.Rule023, input, 16,
                    21, "AClass");
            }

            [Fact]
            public async Task WhenDehydratableAndHasRehydrateMethod_ThenNoAlert()
            {
                const string input = @"
using System;
using System.Collections.Generic;
using Common;
using Domain.Common;
using Domain.Common.Entities;
using Domain.Common.Identity;
using Domain.Common.ValueObjects;
using Domain.Interfaces;
using Domain.Interfaces.Entities;
using Domain.Interfaces.Services;
using Domain.Interfaces.ValueObjects;
using QueryAny;
namespace ANamespace;
[EntityName(""AClass"")]
public sealed class AClass : EntityBase
{
    private AClass(IRecorder recorder, IIdentifierFactory idFactory, RootEventHandler rootEventHandler) : base(recorder, idFactory, rootEventHandler)
    {
    }

    private AClass(ISingleValueObject<string> identifier, IDependencyContainer container, HydrationProperties rehydratingProperties) : base(identifier, container, rehydratingProperties)
    {
    }

    protected override Result<Error> OnStateChanged(IDomainEvent @event)
    {
        return Result.Ok;
    }

    public static AClass Create(IRecorder recorder, IIdentifierFactory idFactory, RootEventHandler rootEventHandler)
    {
        return new AClass(recorder, idFactory, rootEventHandler);
    }

    public override HydrationProperties Dehydrate()
    {
        return base.Dehydrate();
    }

    public static EntityFactory<AClass> Rehydrate()
    {
        return (identifier, container, properties) => new AClass(identifier, container, properties);
    }
}";

                await Verify.NoDiagnosticExists<NonPlatform_DomainDrivenDesignAnalyzer>(input);
            }
        }

        [Trait("Category", "Unit.Tooling")]
        public class GivenRule024
        {
            [Fact]
            public async Task WhenDehydratableAndMissingDehydrateMethod_ThenAlerts()
            {
                const string input = @"
using System;
using System.Collections.Generic;
using Common;
using Domain.Common;
using Domain.Common.Entities;
using Domain.Common.Identity;
using Domain.Common.ValueObjects;
using Domain.Interfaces;
using Domain.Interfaces.Entities;
using Domain.Interfaces.Services;
using Domain.Interfaces.ValueObjects;
using QueryAny;
namespace ANamespace;
[EntityName(""AClass"")]
public sealed class AClass : EntityBase
{
    private AClass(IRecorder recorder, IIdentifierFactory idFactory, RootEventHandler rootEventHandler) : base(recorder, idFactory, rootEventHandler)
    {
    }

    private AClass(ISingleValueObject<string> identifier, IDependencyContainer container, HydrationProperties rehydratingProperties) : base(identifier, container, rehydratingProperties)
    {
    }

    protected override Result<Error> OnStateChanged(IDomainEvent @event)
    {
        return Result.Ok;
    }

    public static AClass Create(IRecorder recorder, IIdentifierFactory idFactory, RootEventHandler rootEventHandler)
    {
        return new AClass(recorder, idFactory, rootEventHandler);
    }

    public static EntityFactory<AClass> Rehydrate()
    {
        return (identifier, container, properties) => new AClass(identifier, container, properties);
    }
}";

                await Verify.DiagnosticExists<NonPlatform_DomainDrivenDesignAnalyzer>(
                    NonPlatform_DomainDrivenDesignAnalyzer.Rule024, input, 16,
                    21, "AClass");
            }

            [Fact]
            public async Task WhenDehydratableAndHasDehydrateMethod_ThenNoAlert()
            {
                const string input = @"
using System;
using System.Collections.Generic;
using Common;
using Domain.Common;
using Domain.Common.Entities;
using Domain.Common.Identity;
using Domain.Common.ValueObjects;
using Domain.Interfaces;
using Domain.Interfaces.Entities;
using Domain.Interfaces.Services;
using Domain.Interfaces.ValueObjects;
using QueryAny;
namespace ANamespace;
[EntityName(""AClass"")]
public sealed class AClass : EntityBase
{
    private AClass(IRecorder recorder, IIdentifierFactory idFactory, RootEventHandler rootEventHandler) : base(recorder, idFactory, rootEventHandler)
    {
    }

    private AClass(ISingleValueObject<string> identifier, IDependencyContainer container, HydrationProperties rehydratingProperties) : base(identifier, container, rehydratingProperties)
    {
    }

    protected override Result<Error> OnStateChanged(IDomainEvent @event)
    {
        return Result.Ok;
    }

    public static AClass Create(IRecorder recorder, IIdentifierFactory idFactory, RootEventHandler rootEventHandler)
    {
        return new AClass(recorder, idFactory, rootEventHandler);
    }

    public override HydrationProperties Dehydrate()
    {
        return base.Dehydrate();
    }

    public static EntityFactory<AClass> Rehydrate()
    {
        return (identifier, container, properties) => new AClass(identifier, container, properties);
    }
}";
                await Verify.NoDiagnosticExists<NonPlatform_DomainDrivenDesignAnalyzer>(input);
            }
        }

        [Trait("Category", "Unit.Tooling")]
        public class GivenRule025
        {
            [Fact]
            public async Task WhenDehydratableAndMissingEntityNameAttribute_ThenAlerts()
            {
                const string input = @"
using System;
using System.Collections.Generic;
using Common;
using Domain.Common;
using Domain.Common.Entities;
using Domain.Common.Identity;
using Domain.Common.ValueObjects;
using Domain.Interfaces;
using Domain.Interfaces.Entities;
using Domain.Interfaces.Services;
using Domain.Interfaces.ValueObjects;
using QueryAny;
namespace ANamespace;
public sealed class AClass : EntityBase
{
    private AClass(IRecorder recorder, IIdentifierFactory idFactory, RootEventHandler rootEventHandler) : base(recorder, idFactory, rootEventHandler)
    {
    }

    private AClass(ISingleValueObject<string> identifier, IDependencyContainer container, HydrationProperties rehydratingProperties) : base(identifier, container, rehydratingProperties)
    {
    }

    protected override Result<Error> OnStateChanged(IDomainEvent @event)
    {
        return Result.Ok;
    }

    public static AClass Create(IRecorder recorder, IIdentifierFactory idFactory, RootEventHandler rootEventHandler)
    {
        return new AClass(recorder, idFactory, rootEventHandler);
    }

    public override HydrationProperties Dehydrate()
    {
        return base.Dehydrate();
    }

    public static EntityFactory<AClass> Rehydrate()
    {
        return (identifier, container, properties) => new AClass(identifier, container, properties);
    }
}";
                await Verify.DiagnosticExists<NonPlatform_DomainDrivenDesignAnalyzer>(
                    NonPlatform_DomainDrivenDesignAnalyzer.Rule025, input, 15,
                    21,
                    "AClass");
            }

            [Fact]
            public async Task WhenDehydratableAndHasEntityNameAttribute_ThenNoAlert()
            {
                const string input = @"
using System;
using System.Collections.Generic;
using Common;
using Domain.Common;
using Domain.Common.Entities;
using Domain.Common.Identity;
using Domain.Common.ValueObjects;
using Domain.Interfaces;
using Domain.Interfaces.Entities;
using Domain.Interfaces.Services;
using Domain.Interfaces.ValueObjects;
using QueryAny;
namespace ANamespace;
[EntityName(""AClass"")]
public sealed class AClass : EntityBase
{
    private AClass(IRecorder recorder, IIdentifierFactory idFactory, RootEventHandler rootEventHandler) : base(recorder, idFactory, rootEventHandler)
    {
    }

    private AClass(ISingleValueObject<string> identifier, IDependencyContainer container, HydrationProperties rehydratingProperties) : base(identifier, container, rehydratingProperties)
    {
    }

    protected override Result<Error> OnStateChanged(IDomainEvent @event)
    {
        return Result.Ok;
    }

    public static AClass Create(IRecorder recorder, IIdentifierFactory idFactory, RootEventHandler rootEventHandler)
    {
        return new AClass(recorder, idFactory, rootEventHandler);
    }

    public override HydrationProperties Dehydrate()
    {
        return base.Dehydrate();
    }

    public static EntityFactory<AClass> Rehydrate()
    {
        return (identifier, container, properties) => new AClass(identifier, container, properties);
    }
}";
                await Verify.NoDiagnosticExists<NonPlatform_DomainDrivenDesignAnalyzer>(input);
            }
        }

        [Trait("Category", "Unit.Tooling")]
        public class GivenRule026
        {
            [Fact]
            public async Task WhenPropertyHasPublicSetter_ThenAlerts()
            {
                const string input = @"
using System;
using Common;
using Domain.Common;
using Domain.Common.Entities;
using Domain.Common.Identity;
using Domain.Common.ValueObjects;
using Domain.Interfaces.Entities;
using Domain.Interfaces.Services;
namespace ANamespace;
public sealed class AClass : EntityBase
{
    private AClass(IRecorder recorder, IIdentifierFactory idFactory, RootEventHandler rootEventHandler) : base(recorder, idFactory, rootEventHandler)
    {
    }

    protected override Result<Error> OnStateChanged(IDomainEvent @event)
    {
        return Result.Ok;
    }

    public static AClass Create(IRecorder recorder, IIdentifierFactory idFactory, RootEventHandler rootEventHandler)
    {
        return new AClass(recorder, idFactory, rootEventHandler);
    }

    public string AProperty { get;set; }
}";

                await Verify.DiagnosticExists<NonPlatform_DomainDrivenDesignAnalyzer>(
                    NonPlatform_DomainDrivenDesignAnalyzer.Rule026, input, 27,
                    19, "AProperty");
            }

            [Fact]
            public async Task WhenPropertyHasPrivateSetter_ThenNoAlert()
            {
                const string input = @"
using System;
using Common;
using Domain.Common;
using Domain.Common.Entities;
using Domain.Common.Identity;
using Domain.Common.ValueObjects;
using Domain.Interfaces.Entities;
using Domain.Interfaces.Services;
namespace ANamespace;
public sealed class AClass : EntityBase
{
    private AClass(IRecorder recorder, IIdentifierFactory idFactory, RootEventHandler rootEventHandler) : base(recorder, idFactory, rootEventHandler)
    {
    }

    protected override Result<Error> OnStateChanged(IDomainEvent @event)
    {
        return Result.Ok;
    }

    public static AClass Create(IRecorder recorder, IIdentifierFactory idFactory, RootEventHandler rootEventHandler)
    {
        return new AClass(recorder, idFactory, rootEventHandler);
    }

    public string AProperty { get; private set; }
}";

                await Verify.NoDiagnosticExists<NonPlatform_DomainDrivenDesignAnalyzer>(input);
            }

            [Fact]
            public async Task WhenPropertyHasArrowFunction_ThenNoAlert()
            {
                const string input = @"
using System;
using Common;
using Domain.Common;
using Domain.Common.Entities;
using Domain.Common.Identity;
using Domain.Common.ValueObjects;
using Domain.Interfaces.Entities;
using Domain.Interfaces.Services;
namespace ANamespace;
public sealed class AClass : EntityBase
{
    private AClass(IRecorder recorder, IIdentifierFactory idFactory, RootEventHandler rootEventHandler) : base(recorder, idFactory, rootEventHandler)
    {
    }

    protected override Result<Error> OnStateChanged(IDomainEvent @event)
    {
        return Result.Ok;
    }

    public static AClass Create(IRecorder recorder, IIdentifierFactory idFactory, RootEventHandler rootEventHandler)
    {
        return new AClass(recorder, idFactory, rootEventHandler);
    }

    public string AProperty => string.Empty;
}";

                await Verify.NoDiagnosticExists<NonPlatform_DomainDrivenDesignAnalyzer>(input);
            }

            [Fact]
            public async Task WhenPropertyHasNoSetter_ThenNoAlert()
            {
                const string input = @"
using System;
using Common;
using Domain.Common;
using Domain.Common.Entities;
using Domain.Common.Identity;
using Domain.Common.ValueObjects;
using Domain.Interfaces.Entities;
using Domain.Interfaces.Services;
namespace ANamespace;
public sealed class AClass : EntityBase
{
    private AClass(IRecorder recorder, IIdentifierFactory idFactory, RootEventHandler rootEventHandler) : base(recorder, idFactory, rootEventHandler)
    {
    }

    protected override Result<Error> OnStateChanged(IDomainEvent @event)
    {
        return Result.Ok;
    }

    public static AClass Create(IRecorder recorder, IIdentifierFactory idFactory, RootEventHandler rootEventHandler)
    {
        return new AClass(recorder, idFactory, rootEventHandler);
    }

    public string AProperty { get; }
}";

                await Verify.NoDiagnosticExists<NonPlatform_DomainDrivenDesignAnalyzer>(input);
            }
        }

        [Trait("Category", "Unit.Tooling")]
        public class GivenRule027
        {
            [Fact]
            public async Task WhenClassIsNotSealed_ThenAlerts()
            {
                const string input = @"
using System;
using Common;
using Domain.Common;
using Domain.Common.Entities;
using Domain.Common.Identity;
using Domain.Common.ValueObjects;
using Domain.Interfaces.Entities;
using Domain.Interfaces.Services;
namespace ANamespace;
public class AClass : EntityBase
{
    private AClass(IRecorder recorder, IIdentifierFactory idFactory, RootEventHandler rootEventHandler) : base(recorder, idFactory, rootEventHandler)
    {
    }

    protected override Result<Error> OnStateChanged(IDomainEvent @event)
    {
        return Result.Ok;
    }

    public static AClass Create(IRecorder recorder, IIdentifierFactory idFactory, RootEventHandler rootEventHandler)
    {
        return new AClass(recorder, idFactory, rootEventHandler);
    }

    public string AProperty { get; }
}";

                await Verify.DiagnosticExists<NonPlatform_DomainDrivenDesignAnalyzer>(
                    NonPlatform_DomainDrivenDesignAnalyzer.Rule027, input, 11,
                    14, "AClass");
            }

            [Fact]
            public async Task WhenClassIsSealed_ThenNoAlert()
            {
                const string input = @"
using System;
using Common;
using Domain.Common;
using Domain.Common.Entities;
using Domain.Common.Identity;
using Domain.Common.ValueObjects;
using Domain.Interfaces.Entities;
using Domain.Interfaces.Services;
namespace ANamespace;
public sealed class AClass : EntityBase
{
    private AClass(IRecorder recorder, IIdentifierFactory idFactory, RootEventHandler rootEventHandler) : base(recorder, idFactory, rootEventHandler)
    {
    }

    protected override Result<Error> OnStateChanged(IDomainEvent @event)
    {
        return Result.Ok;
    }

    public static AClass Create(IRecorder recorder, IIdentifierFactory idFactory, RootEventHandler rootEventHandler)
    {
        return new AClass(recorder, idFactory, rootEventHandler);
    }

    public string AProperty { get; }
}";

                await Verify.NoDiagnosticExists<NonPlatform_DomainDrivenDesignAnalyzer>(input);
            }
        }
    }

    [UsedImplicitly]
    public class GivenAValueObject
    {
        [Trait("Category", "Unit.Tooling")]
        public class GivenRule030
        {
            [Fact]
            public async Task WhenHasNoCreateMethod_ThenAlerts()
            {
                const string input = @"
using System.Collections.Generic;
using Common;
using Domain.Common;
using Domain.Common.Entities;
using Domain.Common.Identity;
using Domain.Common.ValueObjects;
using Domain.Interfaces;
using Domain.Interfaces.Entities;
namespace ANamespace;
public sealed class AClass : ValueObjectBase<AClass>
{
    private AClass(string avalue)
    {
        AProperty = avalue;
    }

    protected override IEnumerable<object?> GetAtomicValues()
    {
        return new object[] { AProperty };
    }

    public static ValueObjectFactory<AClass> Rehydrate()
    {
        return (property, container) => new AClass(null!);
    }

    public string AProperty { get;}
}";

                await Verify.DiagnosticExists<NonPlatform_DomainDrivenDesignAnalyzer>(
                    NonPlatform_DomainDrivenDesignAnalyzer.Rule030, input, 11,
                    21, "AClass");
            }

            [Fact]
            public async Task WhenHasCreateInstanceMethod_ThenAlerts()
            {
                const string input = @"
using System.Collections.Generic;
using Common;
using Domain.Common;
using Domain.Common.Entities;
using Domain.Common.Identity;
using Domain.Common.ValueObjects;
using Domain.Interfaces;
using Domain.Interfaces.Entities;
namespace ANamespace;
public sealed class AClass : ValueObjectBase<AClass>
{
    private AClass(string avalue)
    {
        AProperty = avalue;
    }

    public AClass Create()
    {
        return null!;
    }

    protected override IEnumerable<object?> GetAtomicValues()
    {
        return new object[] { AProperty };
    }

    public static ValueObjectFactory<AClass> Rehydrate()
    {
        return (property, container) => new AClass(null!);
    }

    public string AProperty { get;}
}";

                await Verify.DiagnosticExists<NonPlatform_DomainDrivenDesignAnalyzer>(
                    NonPlatform_DomainDrivenDesignAnalyzer.Rule030, input, 11,
                    21, "AClass");
            }

            [Fact]
            public async Task WhenHasAtLeastOneCreateStaticMethod_ThenNoAlert()
            {
                const string input = @"
using System;
using System.Collections.Generic;
using Common;
using Domain.Common;
using Domain.Common.Entities;
using Domain.Common.Identity;
using Domain.Common.ValueObjects;
using Domain.Interfaces;
using Domain.Interfaces.Entities;
namespace ANamespace;
public sealed class AClass : ValueObjectBase<AClass>
{
    private AClass(string avalue)
    {
        AProperty = avalue;
    }

    public static AClass Create()
    {
        return new AClass(null!);
    }

    protected override IEnumerable<object?> GetAtomicValues()
    {
        return new object[] { AProperty };
    }

    public static ValueObjectFactory<AClass> Rehydrate()
    {
        return (property, container) => new AClass(null!);
    }

    public string AProperty { get;}
}";

                await Verify.NoDiagnosticExists<NonPlatform_DomainDrivenDesignAnalyzer>(input);
            }
        }

        [Trait("Category", "Unit.Tooling")]
        public class GivenRule031
        {
            [Fact]
            public async Task WhenCreateReturnsVoid_ThenAlerts()
            {
                const string input = @"
using System;
using System.Collections.Generic;
using Common;
using Domain.Common;
using Domain.Common.Entities;
using Domain.Common.Identity;
using Domain.Common.ValueObjects;
using Domain.Interfaces;
using Domain.Interfaces.Entities;
namespace ANamespace;
public sealed class AClass : ValueObjectBase<AClass>
{
    private AClass(string avalue)
    {
        AProperty = avalue;
    }

    public static void Create()
    {
    }

    protected override IEnumerable<object?> GetAtomicValues()
    {
        return new object[] { AProperty };
    }

    public static ValueObjectFactory<AClass> Rehydrate()
    {
        return (property, container) => new AClass(null!);
    }

    public string AProperty { get;}
}";

                await Verify.DiagnosticExists<NonPlatform_DomainDrivenDesignAnalyzer>(
                    NonPlatform_DomainDrivenDesignAnalyzer.Rule031, input, 19,
                    24, "Create", "ANamespace.AClass or Common.Result<ANamespace.AClass, Common.Error>");
            }

            [Fact]
            public async Task WhenCreateReturnsOther_ThenAlerts()
            {
                const string input = @"
using System;
using System.Collections.Generic;
using Common;
using Domain.Common;
using Domain.Common.Entities;
using Domain.Common.Identity;
using Domain.Common.ValueObjects;
using Domain.Interfaces;
using Domain.Interfaces.Entities;
namespace ANamespace;
public sealed class AClass : ValueObjectBase<AClass>
{
    private AClass(string avalue)
    {
        AProperty = avalue;
    }

    public static string Create()
    {
        return string.Empty;
    }

    protected override IEnumerable<object?> GetAtomicValues()
    {
        return new object[] { AProperty };
    }

    public static ValueObjectFactory<AClass> Rehydrate()
    {
        return (property, container) => new AClass(null!);
    }

    public string AProperty { get;}
}";

                await Verify.DiagnosticExists<NonPlatform_DomainDrivenDesignAnalyzer>(
                    NonPlatform_DomainDrivenDesignAnalyzer.Rule031, input, 19,
                    26, "Create", "ANamespace.AClass or Common.Result<ANamespace.AClass, Common.Error>");
            }

            [Fact]
            public async Task WhenCreateReturnsNakedClass_ThenNoAlert()
            {
                const string input = @"
using System;
using System.Collections.Generic;
using Common;
using Domain.Common;
using Domain.Common.Entities;
using Domain.Common.Identity;
using Domain.Common.ValueObjects;
using Domain.Interfaces;
using Domain.Interfaces.Entities;
namespace ANamespace;
public sealed class AClass : ValueObjectBase<AClass>
{
    private AClass(string avalue)
    {
        AProperty = avalue;
    }

    public static AClass Create()
    {
        return new AClass(null!);
    }

    protected override IEnumerable<object?> GetAtomicValues()
    {
        return new object[] { AProperty };
    }

    public static ValueObjectFactory<AClass> Rehydrate()
    {
        return (property, container) => new AClass(null!);
    }

    public string AProperty { get;}
}";

                await Verify.NoDiagnosticExists<NonPlatform_DomainDrivenDesignAnalyzer>(input);
            }

            [Fact]
            public async Task WhenCreateReturnsResultOfClassOrError_ThenNoAlert()
            {
                const string input = @"
using System;
using System.Collections.Generic;
using Common;
using Domain.Common;
using Domain.Common.Entities;
using Domain.Common.Identity;
using Domain.Common.ValueObjects;
using Domain.Interfaces;
using Domain.Interfaces.Entities;
namespace ANamespace;
public sealed class AClass : ValueObjectBase<AClass>
{
    private AClass(string avalue)
    {
        AProperty = avalue;
    }

    public static Result<AClass, Error> Create()
    {
        return new AClass(null!);
    }

    protected override IEnumerable<object?> GetAtomicValues()
    {
        return new object[] { AProperty };
    }

    public static ValueObjectFactory<AClass> Rehydrate()
    {
        return (property, container) => new AClass(null!);
    }

    public string AProperty { get;}
}";

                await Verify.NoDiagnosticExists<NonPlatform_DomainDrivenDesignAnalyzer>(input);
            }
        }

        [Trait("Category", "Unit.Tooling")]
        public class GivenRule032
        {
            [Fact]
            public async Task WhenNonPrivateConstructor_ThenAlerts()
            {
                const string input = @"
using System;
using System.Collections.Generic;
using Common;
using Domain.Common;
using Domain.Common.Entities;
using Domain.Common.Identity;
using Domain.Common.ValueObjects;
using Domain.Interfaces;
using Domain.Interfaces.Entities;
namespace ANamespace;
public sealed class AClass : ValueObjectBase<AClass>
{
    public AClass(string avalue)
    {
        AProperty = avalue;
    }

    public static AClass Create()
    {
        return new AClass(null!);
    }

    protected override IEnumerable<object?> GetAtomicValues()
    {
        return new object[] { AProperty };
    }

    public static ValueObjectFactory<AClass> Rehydrate()
    {
        return (property, container) => new AClass(null!);
    }

    public string AProperty { get;}
}";

                await Verify.DiagnosticExists<NonPlatform_DomainDrivenDesignAnalyzer>(
                    NonPlatform_DomainDrivenDesignAnalyzer.Rule032, input, 14,
                    12, "AClass");
            }

            [Fact]
            public async Task WhenPrivateConstructor_ThenNoAlert()
            {
                const string input = @"
using System;
using System.Collections.Generic;
using Common;
using Domain.Common;
using Domain.Common.Entities;
using Domain.Common.Identity;
using Domain.Common.ValueObjects;
using Domain.Interfaces;
using Domain.Interfaces.Entities;
namespace ANamespace;
public sealed class AClass : ValueObjectBase<AClass>
{
    private AClass(string avalue)
    {
        AProperty = avalue;
    }

    public static AClass Create()
    {
        return new AClass(null!);
    }

    protected override IEnumerable<object?> GetAtomicValues()
    {
        return new object[] { AProperty };
    }

    public static ValueObjectFactory<AClass> Rehydrate()
    {
        return (property, container) => new AClass(null!);
    }

    public string AProperty { get;}
}";

                await Verify.NoDiagnosticExists<NonPlatform_DomainDrivenDesignAnalyzer>(input);
            }
        }

        [Trait("Category", "Unit.Tooling")]
        public class GivenRule033
        {
            [Fact]
            public async Task WhenMissingRehydrateMethod_ThenAlerts()
            {
                const string input = @"
using System;
using System.Collections.Generic;
using Common;
using Domain.Common;
using Domain.Common.Entities;
using Domain.Common.Identity;
using Domain.Common.ValueObjects;
using Domain.Interfaces.Entities;
using Domain.Interfaces.Services;
namespace ANamespace;
public sealed class AClass : ValueObjectBase<AClass>
{
    private AClass(string avalue)
    {
        AProperty = avalue;
    }

    public static AClass Create()
    {
        return new AClass(null!);
    }

    protected override IEnumerable<object?> GetAtomicValues()
    {
        return new object[] { AProperty };
    }

    public string AProperty { get;}
}";

                await Verify.DiagnosticExists<NonPlatform_DomainDrivenDesignAnalyzer>(
                    NonPlatform_DomainDrivenDesignAnalyzer.Rule033, input, 12,
                    21, "AClass");
            }

            [Fact]
            public async Task WhenHasRehydrateMethod_ThenNoAlert()
            {
                const string input = @"
using System;
using System.Collections.Generic;
using Common;
using Domain.Common;
using Domain.Common.Entities;
using Domain.Common.Identity;
using Domain.Common.ValueObjects;
using Domain.Interfaces;
using Domain.Interfaces.Entities;
using Domain.Interfaces.Services;
namespace ANamespace;
public sealed class AClass : ValueObjectBase<AClass>
{
    private AClass(string avalue)
    {
        AProperty = avalue;
    }

    public static AClass Create()
    {
        return new AClass(null!);
    }

    protected override IEnumerable<object?> GetAtomicValues()
    {
        return new object[] { AProperty };
    }

    public static ValueObjectFactory<AClass> Rehydrate()
    {
        return (property, container) => new AClass(null!);
    }

    public string AProperty { get;}
}";

                await Verify.NoDiagnosticExists<NonPlatform_DomainDrivenDesignAnalyzer>(input);
            }
        }

        [Trait("Category", "Unit.Tooling")]
        public class GivenRule034
        {
            [Fact]
            public async Task WhenPropertyHasPublicSetter_ThenAlerts()
            {
                const string input = @"
using System;
using System.Collections.Generic;
using Common;
using Domain.Common;
using Domain.Common.Entities;
using Domain.Common.Identity;
using Domain.Common.ValueObjects;
using Domain.Interfaces;
using Domain.Interfaces.Entities;
using Domain.Interfaces.Services;
namespace ANamespace;
public sealed class AClass : ValueObjectBase<AClass>
{
    private AClass(string avalue)
    {
        AProperty = avalue;
    }

    public static AClass Create()
    {
        return new AClass(null!);
    }

    protected override IEnumerable<object?> GetAtomicValues()
    {
        return new object[] { AProperty };
    }

    public static ValueObjectFactory<AClass> Rehydrate()
    {
        return (property, container) => new AClass(null!);
    }

    public string AProperty { get;set; }
}";

                await Verify.DiagnosticExists<NonPlatform_DomainDrivenDesignAnalyzer>(
                    NonPlatform_DomainDrivenDesignAnalyzer.Rule034, input, 35,
                    19, "AProperty");
            }

            [Fact]
            public async Task WhenPropertyHasPrivateSetter_ThenNoAlert()
            {
                const string input = @"
using System;
using System.Collections.Generic;
using Common;
using Domain.Common;
using Domain.Common.Entities;
using Domain.Common.Identity;
using Domain.Common.ValueObjects;
using Domain.Interfaces;
using Domain.Interfaces.Entities;
using Domain.Interfaces.Services;
namespace ANamespace;
public sealed class AClass : ValueObjectBase<AClass>
{
    private AClass(string avalue)
    {
        AProperty = avalue;
    }

    public static AClass Create()
    {
        return new AClass(null!);
    }

    protected override IEnumerable<object?> GetAtomicValues()
    {
        return new object[] { AProperty };
    }

    public static ValueObjectFactory<AClass> Rehydrate()
    {
        return (property, container) => new AClass(null!);
    }

    public string AProperty { get; private set; }
}";

                await Verify.NoDiagnosticExists<NonPlatform_DomainDrivenDesignAnalyzer>(input);
            }

            [Fact]
            public async Task WhenPropertyHasArrowFunction_ThenNoAlert()
            {
                const string input = @"
using System;
using System.Collections.Generic;
using Common;
using Domain.Common;
using Domain.Common.Entities;
using Domain.Common.Identity;
using Domain.Common.ValueObjects;
using Domain.Interfaces;
using Domain.Interfaces.Entities;
using Domain.Interfaces.Services;
namespace ANamespace;
public sealed class AClass : ValueObjectBase<AClass>
{
    private AClass(string avalue)
    {
    }

    public static AClass Create()
    {
        return new AClass(null!);
    }

    protected override IEnumerable<object?> GetAtomicValues()
    {
        return new object[] { AProperty };
    }

    public static ValueObjectFactory<AClass> Rehydrate()
    {
        return (property, container) => new AClass(null!);
    }

    public string AProperty => string.Empty;
}";

                await Verify.NoDiagnosticExists<NonPlatform_DomainDrivenDesignAnalyzer>(input);
            }

            [Fact]
            public async Task WhenPropertyHasNoSetter_ThenNoAlert()
            {
                const string input = @"
using System;
using System.Collections.Generic;
using Common;
using Domain.Common;
using Domain.Common.Entities;
using Domain.Common.Identity;
using Domain.Common.ValueObjects;
using Domain.Interfaces;
using Domain.Interfaces.Entities;
using Domain.Interfaces.Services;
namespace ANamespace;
public sealed class AClass : ValueObjectBase<AClass>
{
    private AClass(string avalue)
    {
        AProperty = avalue;
    }

    public static AClass Create()
    {
        return new AClass(null!);
    }

    protected override IEnumerable<object?> GetAtomicValues()
    {
        return new object[] { AProperty };
    }

    public static ValueObjectFactory<AClass> Rehydrate()
    {
        return (property, container) => new AClass(null!);
    }

    public string AProperty { get; }
}";

                await Verify.NoDiagnosticExists<NonPlatform_DomainDrivenDesignAnalyzer>(input);
            }
        }

        [Trait("Category", "Unit.Tooling")]
        public class GivenRule035
        {
            [Fact]
            public async Task WhenMethodReturnsVoid_ThenAlerts()
            {
                const string input = @"
using System;
using System.Collections.Generic;
using Common;
using Domain.Common;
using Domain.Common.Entities;
using Domain.Common.Identity;
using Domain.Common.ValueObjects;
using Domain.Interfaces;
using Domain.Interfaces.Entities;
using Domain.Interfaces.Services;
namespace ANamespace;
public sealed class AClass : ValueObjectBase<AClass>
{
    private AClass(string avalue)
    {
        AProperty = avalue;
    }

    public static AClass Create()
    {
        return new AClass(null!);
    }

    protected override IEnumerable<object?> GetAtomicValues()
    {
        return new object[] { AProperty };
    }

    public static ValueObjectFactory<AClass> Rehydrate()
    {
        return (property, container) => new AClass(null!);
    }

    public string AProperty { get; }

    public void AMethod()
    {
    }
}";

                await Verify.DiagnosticExists<NonPlatform_DomainDrivenDesignAnalyzer>(
                    NonPlatform_DomainDrivenDesignAnalyzer.Rule035, input, 37,
                    17, "AMethod",
                    "ANamespace.AClass or Common.Result<ANamespace.AClass, Common.Error>",
                    nameof(SkipImmutabilityCheckAttribute));
            }

            [Fact]
            public async Task WhenMethodReturnsOther_ThenAlerts()
            {
                const string input = @"
using System;
using System.Collections.Generic;
using Common;
using Domain.Common;
using Domain.Common.Entities;
using Domain.Common.Identity;
using Domain.Common.ValueObjects;
using Domain.Interfaces;
using Domain.Interfaces.Entities;
using Domain.Interfaces.Services;
namespace ANamespace;
public sealed class AClass : ValueObjectBase<AClass>
{
    private AClass(string avalue)
    {
        AProperty = avalue;
    }

    public static AClass Create()
    {
        return new AClass(null!);
    }

    protected override IEnumerable<object?> GetAtomicValues()
    {
        return new object[] { AProperty };
    }

    public static ValueObjectFactory<AClass> Rehydrate()
    {
        return (property, container) => new AClass(null!);
    }

    public string AProperty { get; }

    public string AMethod()
    {
        return string.Empty;
    }
}";

                await Verify.DiagnosticExists<NonPlatform_DomainDrivenDesignAnalyzer>(
                    NonPlatform_DomainDrivenDesignAnalyzer.Rule035, input, 37,
                    19, "AMethod", "ANamespace.AClass or Common.Result<ANamespace.AClass, Common.Error>",
                    nameof(SkipImmutabilityCheckAttribute));
            }

            [Fact]
            public async Task WhenMethodReturnsVoidAndSkipped_ThenNoAlert()
            {
                const string input = @"
using System;
using System.Collections.Generic;
using Common;
using Domain.Common;
using Domain.Common.Entities;
using Domain.Common.Identity;
using Domain.Common.ValueObjects;
using Domain.Interfaces;
using Domain.Interfaces.Entities;
using Domain.Interfaces.Services;
using Domain.Interfaces.ValueObjects;
namespace ANamespace;
public sealed class AClass : ValueObjectBase<AClass>
{
    private AClass(string avalue)
    {
        AProperty = avalue;
    }

    public static AClass Create()
    {
        return new AClass(null!);
    }

    protected override IEnumerable<object?> GetAtomicValues()
    {
        return new object[] { AProperty };
    }

    public static ValueObjectFactory<AClass> Rehydrate()
    {
        return (property, container) => new AClass(null!);
    }

    public string AProperty { get; }

    [SkipImmutabilityCheck]
    public void AMethod()
    {
    }
}";

                await Verify.NoDiagnosticExists<NonPlatform_DomainDrivenDesignAnalyzer>(input);
            }

            [Fact]
            public async Task WhenMethodReturnsOtherAndSkipped_ThenNoAlert()
            {
                const string input = @"
using System;
using System.Collections.Generic;
using Common;
using Domain.Common;
using Domain.Common.Entities;
using Domain.Common.Identity;
using Domain.Common.ValueObjects;
using Domain.Interfaces;
using Domain.Interfaces.Entities;
using Domain.Interfaces.Services;
using Domain.Interfaces.ValueObjects;
namespace ANamespace;
public sealed class AClass : ValueObjectBase<AClass>
{
    private AClass(string avalue)
    {
        AProperty = avalue;
    }

    public static AClass Create()
    {
        return new AClass(null!);
    }

    protected override IEnumerable<object?> GetAtomicValues()
    {
        return new object[] { AProperty };
    }

    public static ValueObjectFactory<AClass> Rehydrate()
    {
        return (property, container) => new AClass(null!);
    }

    public string AProperty { get; }

    [SkipImmutabilityCheck]
    public string AMethod()
    {
        return string.Empty;
    }
}";

                await Verify.NoDiagnosticExists<NonPlatform_DomainDrivenDesignAnalyzer>(input);
            }

            [Fact]
            public async Task WhenMethodReturnsInstance_ThenNoAlert()
            {
                const string input = @"
using System;
using System.Collections.Generic;
using Common;
using Domain.Common;
using Domain.Common.Entities;
using Domain.Common.Identity;
using Domain.Common.ValueObjects;
using Domain.Interfaces;
using Domain.Interfaces.Entities;
using Domain.Interfaces.Services;
namespace ANamespace;
public sealed class AClass : ValueObjectBase<AClass>
{
    private AClass(string avalue)
    {
        AProperty = avalue;
    }

    public static AClass Create()
    {
        return new AClass(null!);
    }

    protected override IEnumerable<object?> GetAtomicValues()
    {
        return new object[] { AProperty };
    }

    public static ValueObjectFactory<AClass> Rehydrate()
    {
        return (property, container) => new AClass(null!);
    }

    public string AProperty { get; }

    public AClass AMethod()
    {
        return null!;
    }
}";

                await Verify.NoDiagnosticExists<NonPlatform_DomainDrivenDesignAnalyzer>(input);
            }

            [Fact]
            public async Task WhenMethodReturnsResultOfInstance_ThenNoAlert()
            {
                const string input = @"
using System;
using System.Collections.Generic;
using Common;
using Domain.Common;
using Domain.Common.Entities;
using Domain.Common.Identity;
using Domain.Common.ValueObjects;
using Domain.Interfaces;
using Domain.Interfaces.Entities;
using Domain.Interfaces.Services;
namespace ANamespace;
public sealed class AClass : ValueObjectBase<AClass>
{
    private AClass(string avalue)
    {
        AProperty = avalue;
    }

    public static AClass Create()
    {
        return new AClass(null!);
    }

    protected override IEnumerable<object?> GetAtomicValues()
    {
        return new object[] { AProperty };
    }

    public static ValueObjectFactory<AClass> Rehydrate()
    {
        return (property, container) => new AClass(null!);
    }

    public string AProperty { get; }

    public Result<AClass, Error> AMethod()
    {
        return null!;
    }
}";

                await Verify.NoDiagnosticExists<NonPlatform_DomainDrivenDesignAnalyzer>(input);
            }
        }

        [Trait("Category", "Unit.Tooling")]
        public class GivenRule036
        {
            [Fact]
            public async Task WhenIsNotSealed_ThenAlerts()
            {
                const string input = @"
using System;
using System.Collections.Generic;
using Common;
using Domain.Common;
using Domain.Common.Entities;
using Domain.Common.Identity;
using Domain.Common.ValueObjects;
using Domain.Interfaces;
using Domain.Interfaces.Entities;
using Domain.Interfaces.Services;
namespace ANamespace;
public class AClass : ValueObjectBase<AClass>
{
    private AClass(string avalue)
    {
        AProperty = avalue;
    }

    public static AClass Create()
    {
        return new AClass(null!);
    }

    protected override IEnumerable<object?> GetAtomicValues()
    {
        return new object[] { AProperty };
    }

    public static ValueObjectFactory<AClass> Rehydrate()
    {
        return (property, container) => new AClass(null!);
    }

    public string AProperty { get; }

    public Result<AClass, Error> AMethod()
    {
        return null!;
    }
}";

                await Verify.DiagnosticExists<NonPlatform_DomainDrivenDesignAnalyzer>(
                    NonPlatform_DomainDrivenDesignAnalyzer.Rule036, input, 13,
                    14, "AClass");
            }

            [Fact]
            public async Task WhenIsSealed_ThenNoAlert()
            {
                const string input = @"
using System;
using System.Collections.Generic;
using Common;
using Domain.Common;
using Domain.Common.Entities;
using Domain.Common.Identity;
using Domain.Common.ValueObjects;
using Domain.Interfaces;
using Domain.Interfaces.Entities;
using Domain.Interfaces.Services;
namespace ANamespace;
public sealed class AClass : ValueObjectBase<AClass>
{
    private AClass(string avalue)
    {
        AProperty = avalue;
    }

    public static AClass Create()
    {
        return new AClass(null!);
    }

    protected override IEnumerable<object?> GetAtomicValues()
    {
        return new object[] { AProperty };
    }

    public static ValueObjectFactory<AClass> Rehydrate()
    {
        return (property, container) => new AClass(null!);
    }

    public string AProperty { get; }

    public Result<AClass, Error> AMethod()
    {
        return null!;
    }
}";

                await Verify.NoDiagnosticExists<NonPlatform_DomainDrivenDesignAnalyzer>(input);
            }
        }

        [Trait("Category", "Unit.Tooling")]
        public class GivenRule037
        {
            [Fact]
            public async Task WhenPropertyIsNullable_ThenAlerts()
            {
                const string input = @"
using System;
using System.Collections.Generic;
using Common;
using Domain.Common;
using Domain.Common.Entities;
using Domain.Common.Identity;
using Domain.Common.ValueObjects;
using Domain.Interfaces;
using Domain.Interfaces.Entities;
using Domain.Interfaces.Services;
namespace ANamespace;
public sealed class AClass : ValueObjectBase<AClass>
{
    private AClass(string avalue)
    {
        AProperty = avalue;
    }

    public static AClass Create()
    {
        return new AClass(null!);
    }

    protected override IEnumerable<object?> GetAtomicValues()
    {
        return new object[] { AProperty, AnotherProperty };
    }

    public static ValueObjectFactory<AClass> Rehydrate()
    {
        return (property, container) => new AClass(null!);
    }

    public string AProperty { get; }

    public string? AnotherProperty { get; }

    public Result<AClass, Error> AMethod()
    {
        return null!;
    }
}";

                await Verify.DiagnosticExists<NonPlatform_DomainDrivenDesignAnalyzer>(
                    NonPlatform_DomainDrivenDesignAnalyzer.Rule037, input, 37,
                    20, "AnotherProperty", "String");
            }

            [Fact]
            public async Task WhenPropertyIsOptional_ThenNoAlert()
            {
                const string input = @"
using System;
using System.Collections.Generic;
using Common;
using Domain.Common;
using Domain.Common.Entities;
using Domain.Common.Identity;
using Domain.Common.ValueObjects;
using Domain.Interfaces;
using Domain.Interfaces.Entities;
using Domain.Interfaces.Services;
namespace ANamespace;
public sealed class AClass : ValueObjectBase<AClass>
{
    private AClass(string avalue)
    {
        AProperty = avalue;
    }

    public static AClass Create()
    {
        return new AClass(null!);
    }

    protected override IEnumerable<object?> GetAtomicValues()
    {
        return new object[] { AProperty, AnotherProperty  };
    }

    public static ValueObjectFactory<AClass> Rehydrate()
    {
        return (property, container) => new AClass(null!);
    }

    public string AProperty { get; }

    public Optional<string> AnotherProperty { get; }

    public Result<AClass, Error> AMethod()
    {
        return null!;
    }
}";

                await Verify.NoDiagnosticExists<NonPlatform_DomainDrivenDesignAnalyzer>(input);
            }
        }

        [Trait("Category", "Unit.Tooling")]
        public class GivenRule038
        {
            [Fact]
            public async Task WhenSingleValueObject_ThenNoAlert()
            {
                const string input = @"
using System;
using System.Collections.Generic;
using Common;
using Domain.Common;
using Domain.Common.Entities;
using Domain.Common.Identity;
using Domain.Common.ValueObjects;
using Domain.Interfaces;
using Domain.Interfaces.Entities;
using Domain.Interfaces.Services;
namespace ANamespace;
public sealed class AClass : SingleValueObjectBase<AClass, string>
{
    public static Result<AClass, Error> Create(string avalue)
    {
        return new AClass(avalue);
    }

    private AClass(string avalue) : base(avalue)
    {
        AProperty = avalue;
    }

    public string Name => Value;

    public string AProperty { get; }

    public static ValueObjectFactory<AClass> Rehydrate()
    {
        return (property, _) =>
        {
            var parts = RehydrateToList(property, true);
            return new AClass(parts[0]!);
        };
    }
}";

                await Verify.NoDiagnosticExists<NonPlatform_DomainDrivenDesignAnalyzer>(input);
            }

            [Fact]
            public async Task WhenGetAtomicValuesWithGetterBody_ThenNoAlert()
            {
                const string input = @"
using System;
using System.Collections.Generic;
using Common;
using Domain.Common;
using Domain.Common.Entities;
using Domain.Common.Identity;
using Domain.Common.ValueObjects;
using Domain.Interfaces;
using Domain.Interfaces.Entities;
using Domain.Interfaces.Services;
namespace ANamespace;
public sealed class AClass : ValueObjectBase<AClass>
{
    private AClass(string avalue)
    {
        AProperty = avalue;
    }

    public static AClass Create()
    {
        return new AClass(null!);
    }

    protected override IEnumerable<object?> GetAtomicValues()
    {
        return new[] { AProperty };
    }

    public static ValueObjectFactory<AClass> Rehydrate()
    {
        return (property, container) => new AClass(null!);
    }

    public string AProperty { get; }

    public string AnotherProperty 
    { 
        get
        {
            return ""avalue"";
        }
    }

    public Result<AClass, Error> AMethod()
    {
        return null!;
    }
}";

                await Verify.NoDiagnosticExists<NonPlatform_DomainDrivenDesignAnalyzer>(input);
            }

            [Fact]
            public async Task WhenGetAtomicValuesWithGetterArrow_ThenNoAlert()
            {
                const string input = @"
using System;
using System.Collections.Generic;
using Common;
using Domain.Common;
using Domain.Common.Entities;
using Domain.Common.Identity;
using Domain.Common.ValueObjects;
using Domain.Interfaces;
using Domain.Interfaces.Entities;
using Domain.Interfaces.Services;
namespace ANamespace;
public sealed class AClass : ValueObjectBase<AClass>
{
    private AClass(string avalue)
    {
        AProperty = avalue;
    }

    public static AClass Create()
    {
        return new AClass(null!);
    }

    protected override IEnumerable<object?> GetAtomicValues()
    {
        return new[] { AProperty };
    }

    public static ValueObjectFactory<AClass> Rehydrate()
    {
        return (property, container) => new AClass(null!);
    }

    public string AProperty { get; }

    public string AnotherProperty => ""avalue"";

    public Result<AClass, Error> AMethod()
    {
        return null!;
    }
}";

                await Verify.NoDiagnosticExists<NonPlatform_DomainDrivenDesignAnalyzer>(input);
            }

            [Fact]
            public async Task WhenGetAtomicValuesIsMissingProperty_ThenAlerts()
            {
                const string input = @"
using System;
using System.Collections.Generic;
using Common;
using Domain.Common;
using Domain.Common.Entities;
using Domain.Common.Identity;
using Domain.Common.ValueObjects;
using Domain.Interfaces;
using Domain.Interfaces.Entities;
using Domain.Interfaces.Services;
namespace ANamespace;
public sealed class AClass : ValueObjectBase<AClass>
{
    private AClass(string avalue)
    {
        AProperty = avalue;
    }

    public static AClass Create()
    {
        return new AClass(null!);
    }

    protected override IEnumerable<object?> GetAtomicValues()
    {
        return new[] { AProperty };
    }

    public static ValueObjectFactory<AClass> Rehydrate()
    {
        return (property, container) => new AClass(null!);
    }

    public string AProperty { get; }

    public string AnotherProperty { get; }

    public Result<AClass, Error> AMethod()
    {
        return null!;
    }
}";

                await Verify.DiagnosticExists<NonPlatform_DomainDrivenDesignAnalyzer>(
                    NonPlatform_DomainDrivenDesignAnalyzer.Rule038, input, 25,
                    45, "GetAtomicValues", "AnotherProperty");
            }

            [Fact]
            public async Task WhenGetAtomicValuesHasAllPropertiesWithMemberFunction_ThenNoAlert()
            {
                const string input = @"
using System;
using System.Collections.Generic;
using Common;
using Domain.Common;
using Domain.Common.Entities;
using Domain.Common.Identity;
using Domain.Common.ValueObjects;
using Domain.Interfaces;
using Domain.Interfaces.Entities;
using Domain.Interfaces.Services;
namespace ANamespace;
public sealed class AClass : ValueObjectBase<AClass>
{
    private AClass(string avalue)
    {
        AProperty = avalue;
    }

    public static AClass Create()
    {
        return new AClass(null!);
    }

    protected override IEnumerable<object?> GetAtomicValues()
    {
        return new[] { AnotherProperty.ToString(), AProperty, AnEnum.ToString() };
    }

    public static ValueObjectFactory<AClass> Rehydrate()
    {
        return (property, container) => new AClass(null!);
    }

    public string AProperty { get; }

    public string AnotherProperty { get; }

    public AnEnum AnEnum { get; }

    public Result<AClass, Error> AMethod()
    {
        return null!;
    }
}
public enum AnEnum
{
    AValue
}
";

                await Verify.NoDiagnosticExists<NonPlatform_DomainDrivenDesignAnalyzer>(input);
            }

            [Fact]
            public async Task WhenGetAtomicValuesHasAllPropertiesWithExtensionMethod_ThenNoAlert()
            {
                const string input = @"
using System;
using System.Collections.Generic;
using Common;
using Domain.Common;
using Domain.Common.Entities;
using Domain.Common.Identity;
using Domain.Common.ValueObjects;
using Domain.Interfaces;
using Domain.Interfaces.Entities;
using Domain.Interfaces.Services;
namespace ANamespace;
public sealed class AClass : ValueObjectBase<AClass>
{
    private AClass(string avalue)
    {
        AProperty = avalue;
    }

    public static AClass Create()
    {
        return new AClass(null!);
    }

    protected override IEnumerable<object?> GetAtomicValues()
    {
        return new[] { AnotherProperty.Convert(), AProperty };
    }

    public static ValueObjectFactory<AClass> Rehydrate()
    {
        return (property, container) => new AClass(null!);
    }

    public string AProperty { get; }

    public string AnotherProperty { get; }

    public Result<AClass, Error> AMethod()
    {
        return null!;
    }
}
public static class ExtensionMethods
{
    public static string Convert(this string value)
    {
        return ""avalue"";
    }
}";

                await Verify.NoDiagnosticExists<NonPlatform_DomainDrivenDesignAnalyzer>(input);
            }

            [Fact]
            public async Task WhenGetAtomicValuesHasAllPropertiesWithStaticMethod_ThenNoAlert()
            {
                const string input = @"
using System;
using System.Collections.Generic;
using Common;
using Domain.Common;
using Domain.Common.Entities;
using Domain.Common.Identity;
using Domain.Common.ValueObjects;
using Domain.Interfaces;
using Domain.Interfaces.Entities;
using Domain.Interfaces.Services;
namespace ANamespace;
public sealed class AClass : ValueObjectBase<AClass>
{
    private AClass(string avalue)
    {
        AProperty = avalue;
    }

    public static AClass Create()
    {
        return new AClass(null!);
    }

    protected override IEnumerable<object?> GetAtomicValues()
    {
        return new[] { StaticMethods.Convert(AnotherProperty), AProperty };
    }

    public static ValueObjectFactory<AClass> Rehydrate()
    {
        return (property, container) => new AClass(null!);
    }

    public string AProperty { get; }

    public string AnotherProperty { get; }

    public Result<AClass, Error> AMethod()
    {
        return null!;
    }
}
public static class StaticMethods
{
    public static string Convert(string value)
    {
        return ""avalue"";
    }
}";

                await Verify.NoDiagnosticExists<NonPlatform_DomainDrivenDesignAnalyzer>(input);
            }

            [Fact]
            public async Task WhenGetAtomicValuesHasAllPropertiesWithMemberAccess_ThenNoAlert()
            {
                const string input = @"
using System;
using System.Collections.Generic;
using Common;
using Domain.Common;
using Domain.Common.Entities;
using Domain.Common.Identity;
using Domain.Common.ValueObjects;
using Domain.Interfaces;
using Domain.Interfaces.Entities;
using Domain.Interfaces.Services;
namespace ANamespace;
public sealed class AClass : ValueObjectBase<AClass>
{
    private AClass(string avalue)
    {
        AProperty = avalue;
    }

    public static AClass Create()
    {
        return new AClass(null!);
    }

    protected override IEnumerable<object?> GetAtomicValues()
    {
        return new[] { AnotherProperty.AValue, AProperty };
    }

    public static ValueObjectFactory<AClass> Rehydrate()
    {
        return (property, container) => new AClass(null!);
    }

    public string AProperty { get; }

    public AType AnotherProperty { get; }

    public Result<AClass, Error> AMethod()
    {
        return null!;
    }
}
public class AType
{
    public string AValue { get;set; }
}
";

                await Verify.NoDiagnosticExists<NonPlatform_DomainDrivenDesignAnalyzer>(input);
            }

            [Fact]
            public async Task WhenGetAtomicValuesHasAllPropertiesAsRaw_ThenNoAlert()
            {
                const string input = @"
using System;
using System.Collections.Generic;
using Common;
using Domain.Common;
using Domain.Common.Entities;
using Domain.Common.Identity;
using Domain.Common.ValueObjects;
using Domain.Interfaces;
using Domain.Interfaces.Entities;
using Domain.Interfaces.Services;
namespace ANamespace;
public sealed class AClass : ValueObjectBase<AClass>
{
    private AClass(string avalue)
    {
        AProperty = avalue;
    }

    public static AClass Create()
    {
        return new AClass(null!);
    }

    protected override IEnumerable<object?> GetAtomicValues()
    {
        return new[] { AProperty, AnotherProperty };
    }

    public static ValueObjectFactory<AClass> Rehydrate()
    {
        return (property, container) => new AClass(null!);
    }

    public string AProperty { get; }

    public string AnotherProperty { get; }

    public Result<AClass, Error> AMethod()
    {
        return null!;
    }
}";

                await Verify.NoDiagnosticExists<NonPlatform_DomainDrivenDesignAnalyzer>(input);
            }
        }
    }

    [UsedImplicitly]
    public class GivenADomainEvent
    {
        [Trait("Category", "Unit.Tooling")]
        public class GivenAnyDomainEvent
        {
            [Fact]
            public async Task WhenNoCtor_ThenNoAlert()
            {
                const string input = @"
using System;
using Domain.Interfaces.Entities;
namespace ANamespace;
public sealed class AClassed : IDomainEvent
{
    public static AClassed Create()
    {
        return new AClassed
        {
            RootId = string.Empty,
            OccurredUtc = DateTime.UtcNow
        };
    }

    public required string RootId { get; set; }

    public required DateTime OccurredUtc { get; set; }
}";

                await Verify.NoDiagnosticExists<NonPlatform_DomainDrivenDesignAnalyzer>(input);
            }
        }

        [Trait("Category", "Unit.Tooling")]
        public class GivenRule040
        {
            [Fact]
            public async Task WhenIsNotPublic_ThenAlerts()
            {
                const string input = @"
using System;
using Domain.Interfaces.Entities;
namespace ANamespace;
internal sealed class AClassed : IDomainEvent
{
    public static AClassed Create()
    {
        return new AClassed
        {
            RootId = string.Empty,
            OccurredUtc = DateTime.UtcNow
        };
    }

    public required string RootId { get; set; }

    public required DateTime OccurredUtc { get; set; }
}";

                await Verify.DiagnosticExists<NonPlatform_DomainDrivenDesignAnalyzer>(
                    NonPlatform_DomainDrivenDesignAnalyzer.Rule040, input, 5, 23, "AClassed");
            }
        }

        [Trait("Category", "Unit.Tooling")]
        public class GivenRule041
        {
            [Fact]
            public async Task WhenIsNotSealed_ThenAlerts()
            {
                const string input = @"
using System;
using Domain.Interfaces.Entities;
namespace ANamespace;
public class AClassed : IDomainEvent
{
    public static AClassed Create()
    {
        return new AClassed
        {
            RootId = string.Empty,
            OccurredUtc = DateTime.UtcNow
        };
    }

    public required string RootId { get; set; }

    public required DateTime OccurredUtc { get; set; }
}";

                await Verify.DiagnosticExists<NonPlatform_DomainDrivenDesignAnalyzer>(
                    NonPlatform_DomainDrivenDesignAnalyzer.Rule041, input, 5, 14, "AClassed");
            }
        }

        [Trait("Category", "Unit.Tooling")]
        public class GivenRule042
        {
            [Fact]
            public async Task WhenHasCtorAndNotParameterless_ThenAlerts()
            {
                const string input = @"
using System;
using Domain.Interfaces.Entities;
namespace ANamespace;
public sealed class AClassed : IDomainEvent
{
    public AClassed(string rootId, DateTime occurredUtc)
    {
        RootId = rootId;
        OccurredUtc = occurredUtc;
    }

    public static AClassed Create()
    {
        return new AClassed(string.Empty, DateTime.UtcNow)
        {
            RootId = string.Empty,
            OccurredUtc = DateTime.UtcNow
        };
    }

    public required string RootId { get; set; }

    public required DateTime OccurredUtc { get; set; }
}";

                await Verify.DiagnosticExists<NonPlatform_DomainDrivenDesignAnalyzer>(
                    NonPlatform_DomainDrivenDesignAnalyzer.Rule042, input, 5, 21, "AClassed");
            }

            [Fact]
            public async Task WhenHasCtorAndPrivate_ThenAlerts()
            {
                const string input = @"
using System;
using Domain.Interfaces.Entities;
namespace ANamespace;
public sealed class AClassed : IDomainEvent
{
    private AClassed()
    {
        RootId = string.Empty;
        OccurredUtc = DateTime.UtcNow;
    }

    public static AClassed Create()
    {
        return new AClassed
        {
            RootId = string.Empty,
            OccurredUtc = DateTime.UtcNow
        };
    }

    public required string RootId { get; set; }

    public required DateTime OccurredUtc { get; set; }
}";

                await Verify.DiagnosticExists<NonPlatform_DomainDrivenDesignAnalyzer>(
                    NonPlatform_DomainDrivenDesignAnalyzer.Rule042, input, 5, 21, "AClassed");
            }

            [Fact]
            public async Task WhenHasCtorAndIsParameterless_ThenNoAlert()
            {
                const string input = @"
using System;
using Domain.Interfaces.Entities;
namespace ANamespace;
public sealed class AClassed : IDomainEvent
{
    public AClassed()
    {
        RootId = string.Empty;
        OccurredUtc = DateTime.UtcNow;
    }

    public static AClassed Create()
    {
        return new AClassed
        {
            RootId = string.Empty,
            OccurredUtc = DateTime.UtcNow
        };
    }

    public required string RootId { get; set; }

    public required DateTime OccurredUtc { get; set; }
}";

                await Verify.NoDiagnosticExists<NonPlatform_DomainDrivenDesignAnalyzer>(input);
            }
        }

        [Trait("Category", "Unit.Tooling")]
        public class GivenRule043
        {
            [Fact]
            public async Task WhenNotNamedInThePastTense_ThenAlerts()
            {
                const string input = @"
using System;
using Domain.Interfaces.Entities;
namespace ANamespace;
public sealed class AClass : IDomainEvent
{
    public static AClass Create()
    {
        return new AClass
        {
            RootId = string.Empty,
            OccurredUtc = DateTime.UtcNow
        };
    }

    public required string RootId { get; set; }

    public required DateTime OccurredUtc { get; set; }
}";

                await Verify.DiagnosticExists<NonPlatform_DomainDrivenDesignAnalyzer>(
                    NonPlatform_DomainDrivenDesignAnalyzer.Rule043, input, 5, 21, "AClass");
            }

            [Fact]
            public async Task WhenNotNamedInThePastTenseAndVersioned_ThenAlerts()
            {
                const string input = @"
using System;
using Domain.Interfaces.Entities;
namespace ANamespace;
public sealed class AClassV2 : IDomainEvent
{
    public static AClassV2 Create()
    {
        return new AClassV2
        {
            RootId = string.Empty,
            OccurredUtc = DateTime.UtcNow
        };
    }

    public required string RootId { get; set; }

    public required DateTime OccurredUtc { get; set; }
}";

                await Verify.DiagnosticExists<NonPlatform_DomainDrivenDesignAnalyzer>(
                    NonPlatform_DomainDrivenDesignAnalyzer.Rule043, input, 5, 21, "AClassV2");
            }

            [Fact]
            public async Task WhenNamedInThePastTenseAndVersioned_ThenNoAlert()
            {
                const string input = @"
using System;
using Domain.Interfaces.Entities;
namespace ANamespace;
public sealed class AClassedV2 : IDomainEvent
{
    public static AClassedV2 Create()
    {
        return new AClassedV2
        {
            RootId = string.Empty,
            OccurredUtc = DateTime.UtcNow
        };
    }

    public required string RootId { get; set; }

    public required DateTime OccurredUtc { get; set; }
}";

                await Verify.NoDiagnosticExists<NonPlatform_DomainDrivenDesignAnalyzer>(input);
            }

            [Fact]
            public async Task WhenNamedInThePastTense_ThenNoAlert()
            {
                const string input = @"
using System;
using Domain.Interfaces.Entities;
namespace ANamespace;
public sealed class AClassed : IDomainEvent
{
    public static AClassed Create()
    {
        return new AClassed
        {
            RootId = string.Empty,
            OccurredUtc = DateTime.UtcNow
        };
    }

    public required string RootId { get; set; }

    public required DateTime OccurredUtc { get; set; }
}";

                await Verify.NoDiagnosticExists<NonPlatform_DomainDrivenDesignAnalyzer>(input);
            }
        }

        [Trait("Category", "Unit.Tooling")]
        public class GivenRule045
        {
            [Fact]
            public async Task WhenCreateFactoryReturnsWrongType_ThenAlerts()
            {
                const string input = @"
using System;
using Domain.Interfaces.Entities;
namespace ANamespace;
public sealed class AClassed : IDomainEvent
{
    public static string Create()
    {
        return string.Empty;
    }

    public required string RootId { get; set; }

    public required DateTime OccurredUtc { get; set; }
}";

                await Verify.DiagnosticExists<NonPlatform_DomainDrivenDesignAnalyzer>(
                    NonPlatform_DomainDrivenDesignAnalyzer.Rule045, input, 7, 26, "Create", "ANamespace.AClassed");
            }
        }

        [Trait("Category", "Unit.Tooling")]
        public class GivenRule046
        {
            [Fact]
            public async Task WhenAnyPropertyHasNoSetter_ThenAlerts()
            {
                const string input = @"
using System;
using Domain.Interfaces.Entities;
namespace ANamespace;
public sealed class AClassed : IDomainEvent
{
    public static AClassed Create()
    {
        return new AClassed
        {
            RootId = string.Empty,
            OccurredUtc = DateTime.UtcNow
        };
    }

    public required string RootId { get; set; }

    public required DateTime OccurredUtc { get; set; }

    public string? AProperty { get; }
}";

                await Verify.DiagnosticExists<NonPlatform_DomainDrivenDesignAnalyzer>(
                    NonPlatform_DomainDrivenDesignAnalyzer.Rule046, input, 20, 20, "AProperty", null);
            }
        }

        [Trait("Category", "Unit.Tooling")]
        public class GivenRule047
        {
            [Fact]
            public async Task WhenAnyPropertyReferenceTypeIsNotRequiredAndNotInitializedAndNotNullable_ThenAlerts()
            {
                const string input = @"
using System;
using Domain.Interfaces.Entities;
namespace ANamespace;
public sealed class AClassed : IDomainEvent
{
    public static AClassed Create()
    {
        return new AClassed
        {
            RootId = string.Empty,
            OccurredUtc = DateTime.UtcNow
        };
    }

    public required string RootId { get; set; }

    public required DateTime OccurredUtc { get; set; }

    public string AProperty { get; set; }
}";

                await Verify.DiagnosticExists<NonPlatform_DomainDrivenDesignAnalyzer>(
                    NonPlatform_DomainDrivenDesignAnalyzer.Rule047, input, 20, 19, "AProperty");
            }

            [Fact]
            public async Task WhenAnyPropertyReferenceTypeIsRequiredAndNotInitializedAndNotNullable_ThenNoAlert()
            {
                const string input = @"
using System;
using Domain.Interfaces.Entities;
namespace ANamespace;
public sealed class AClassed : IDomainEvent
{
    public static AClassed Create()
    {
        return new AClassed
        {
            AProperty = string.Empty,
            RootId = string.Empty,
            OccurredUtc = DateTime.UtcNow
        };
    }

    public required string RootId { get; set; }

    public required DateTime OccurredUtc { get; set; }

    public required string AProperty { get; set; }
}";

                await Verify.NoDiagnosticExists<NonPlatform_DomainDrivenDesignAnalyzer>(input);
            }

            [Fact]
            public async Task WhenAnyPropertyReferenceTypeIsInitializedAndNotRequiredAndNotNullable_ThenNoAlert()
            {
                const string input = @"
using System;
using Domain.Interfaces.Entities;
namespace ANamespace;
public sealed class AClassed : IDomainEvent
{
    public static AClassed Create()
    {
        return new AClassed
        {
            RootId = string.Empty,
            OccurredUtc = DateTime.UtcNow
        };
    }

    public required string RootId { get; set; }

    public required DateTime OccurredUtc { get; set; }

    public string AProperty { get; set; } = string.Empty;
}";

                await Verify.NoDiagnosticExists<NonPlatform_DomainDrivenDesignAnalyzer>(input);
            }

            [Fact]
            public async Task WhenAnyPropertyReferenceTypeIsNullableAndNotRequiredAndNotInitialized_ThenNoAlert()
            {
                const string input = @"
using System;
using Domain.Interfaces.Entities;
namespace ANamespace;
public sealed class AClassed : IDomainEvent
{
    public static AClassed Create()
    {
        return new AClassed
        {
            RootId = string.Empty,
            OccurredUtc = DateTime.UtcNow
        };
    }

    public required string RootId { get; set; }

    public required DateTime OccurredUtc { get; set; }

    public string? AProperty { get; set; }
}";

                await Verify.NoDiagnosticExists<NonPlatform_DomainDrivenDesignAnalyzer>(input);
            }

            [Fact]
            public async Task WhenAnyPropertyReferenceTypeIsNullableAndRequiredAndNotInitialized_ThenNoAlert()
            {
                const string input = @"
using System;
using Domain.Interfaces.Entities;
namespace ANamespace;
public sealed class AClassed : IDomainEvent
{
    public static AClassed Create()
    {
        return new AClassed
        {
            AProperty = string.Empty,
            RootId = string.Empty,
            OccurredUtc = DateTime.UtcNow
        };
    }

    public required string RootId { get; set; }

    public required DateTime OccurredUtc { get; set; }

    public string? AProperty { get; set; }
}";

                await Verify.NoDiagnosticExists<NonPlatform_DomainDrivenDesignAnalyzer>(input);
            }

            [Fact]
            public async Task WhenAnyPropertyReferenceTypeIsNullableAndInitializedAndNotRequired_ThenNoAlert()
            {
                const string input = @"
using System;
using Domain.Interfaces.Entities;
namespace ANamespace;
public sealed class AClassed : IDomainEvent
{
    public static AClassed Create()
    {
        return new AClassed
        {
            RootId = string.Empty,
            OccurredUtc = DateTime.UtcNow
        };
    }

    public required string RootId { get; set; }

    public required DateTime OccurredUtc { get; set; }

    public string? AProperty { get; set; } = string.Empty;
}";

                await Verify.NoDiagnosticExists<NonPlatform_DomainDrivenDesignAnalyzer>(input);
            }

            [Fact]
            public async Task WhenAnyPropertyReferenceTypeIsInitializedAndRequiredAndNotNullable_ThenNoAlert()
            {
                const string input = @"
using System;
using Domain.Interfaces.Entities;
namespace ANamespace;
public sealed class AClassed : IDomainEvent
{
    public static AClassed Create()
    {
        return new AClassed
        {
            AProperty = string.Empty,
            RootId = string.Empty,
            OccurredUtc = DateTime.UtcNow
        };
    }

    public required string RootId { get; set; }

    public required DateTime OccurredUtc { get; set; }

    public required string AProperty { get; set; } = string.Empty;
}";

                await Verify.NoDiagnosticExists<NonPlatform_DomainDrivenDesignAnalyzer>(input);
            }

            [Fact]
            public async Task WhenAnyPropertyReferenceTypeIsInitializedAndNullableAndNotRequired_ThenNoAlert()
            {
                const string input = @"
using System;
using Domain.Interfaces.Entities;
namespace ANamespace;
public sealed class AClassed : IDomainEvent
{
    public static AClassed Create()
    {
        return new AClassed
        {
            RootId = string.Empty,
            OccurredUtc = DateTime.UtcNow
        };
    }

    public required string RootId { get; set; }

    public required DateTime OccurredUtc { get; set; }

    public string? AProperty { get; set; } = string.Empty;
}";

                await Verify.NoDiagnosticExists<NonPlatform_DomainDrivenDesignAnalyzer>(input);
            }

            [Fact]
            public async Task WhenAnyPropertyReferenceTypeIsInitializedAndRequiredAndNullable_ThenNoAlert()
            {
                const string input = @"
using System;
using Domain.Interfaces.Entities;
namespace ANamespace;
public sealed class AClassed : IDomainEvent
{
    public static AClassed Create()
    {
        return new AClassed
        {
            AProperty = string.Empty,
            RootId = string.Empty,
            OccurredUtc = DateTime.UtcNow
        };
    }

    public required string RootId { get; set; }

    public required DateTime OccurredUtc { get; set; }

    public string? AProperty { get; set; } = string.Empty;
}";

                await Verify.NoDiagnosticExists<NonPlatform_DomainDrivenDesignAnalyzer>(input);
            }

            [Fact]
            public async Task WhenAnyPropertyValueTypeIsNotRequiredAndNotInitializedAndNotNullable_ThenAlerts()
            {
                const string input = @"
using System;
using Domain.Interfaces.Entities;
namespace ANamespace;
public sealed class AClassed : IDomainEvent
{
    public static AClassed Create()
    {
        return new AClassed
        {
            RootId = string.Empty,
            OccurredUtc = DateTime.UtcNow
        };
    }

    public required string RootId { get; set; }

    public required DateTime OccurredUtc { get; set; }

    public DateTime AProperty { get; set; }
}";

                await Verify.DiagnosticExists<NonPlatform_DomainDrivenDesignAnalyzer>(
                    NonPlatform_DomainDrivenDesignAnalyzer.Rule047, input, 20, 21, "AProperty");
            }

            [Fact]
            public async Task WhenAnyPropertyValueTypeIsRequiredAndNotInitializedAndNotNullable_ThenNoAlert()
            {
                const string input = @"
using System;
using Domain.Interfaces.Entities;
namespace ANamespace;
public sealed class AClassed : IDomainEvent
{
    public static AClassed Create()
    {
        return new AClassed
        {
            AProperty = DateTime.UtcNow,
            RootId = string.Empty,
            OccurredUtc = DateTime.UtcNow
        };
    }

    public required string RootId { get; set; }

    public required DateTime OccurredUtc { get; set; }

    public required DateTime AProperty { get; set; }
}";

                await Verify.NoDiagnosticExists<NonPlatform_DomainDrivenDesignAnalyzer>(input);
            }

            [Fact]
            public async Task WhenAnyPropertyValueTypeIsInitializedAndNotRequiredAndNotNullable_ThenNoAlert()
            {
                const string input = @"
using System;
using Domain.Interfaces.Entities;
namespace ANamespace;
public sealed class AClassed : IDomainEvent
{
    public static AClassed Create()
    {
        return new AClassed
        {
            RootId = string.Empty,
            OccurredUtc = DateTime.UtcNow
        };
    }

    public required string RootId { get; set; }

    public required DateTime OccurredUtc { get; set; }

    public DateTime AProperty { get; set; } = DateTime.UtcNow;
}";

                await Verify.NoDiagnosticExists<NonPlatform_DomainDrivenDesignAnalyzer>(input);
            }

            [Fact]
            public async Task WhenAnyPropertyValueTypeIsNullableAndNotRequiredAndNotInitialized_ThenNoAlert()
            {
                const string input = @"
using System;
using Domain.Interfaces.Entities;
namespace ANamespace;
public sealed class AClassed : IDomainEvent
{
    public static AClassed Create()
    {
        return new AClassed
        {
            RootId = string.Empty,
            OccurredUtc = DateTime.UtcNow
        };
    }

    public required string RootId { get; set; }

    public required DateTime OccurredUtc { get; set; }

    public DateTime? AProperty { get; set; }
}";

                await Verify.NoDiagnosticExists<NonPlatform_DomainDrivenDesignAnalyzer>(input);
            }

            [Fact]
            public async Task WhenAnyPropertyValueTypeIsNullableAndRequiredAndNotInitialized_ThenNoAlert()
            {
                const string input = @"
using System;
using Domain.Interfaces.Entities;
namespace ANamespace;
public sealed class AClassed : IDomainEvent
{
    public static AClassed Create()
    {
        return new AClassed
        {
            AProperty = DateTime.UtcNow,
            RootId = string.Empty,
            OccurredUtc = DateTime.UtcNow
        };
    }

    public required string RootId { get; set; }

    public required DateTime OccurredUtc { get; set; }

    public required DateTime? AProperty { get; set; }
}";

                await Verify.NoDiagnosticExists<NonPlatform_DomainDrivenDesignAnalyzer>(input);
            }

            [Fact]
            public async Task WhenAnyPropertyValueTypeIsNullableAndInitializedAndNotRequired_ThenNoAlert()
            {
                const string input = @"
using System;
using Domain.Interfaces.Entities;
namespace ANamespace;
public sealed class AClassed : IDomainEvent
{
    public static AClassed Create()
    {
        return new AClassed
        {
            RootId = string.Empty,
            OccurredUtc = DateTime.UtcNow
        };
    }

    public required string RootId { get; set; }

    public required DateTime OccurredUtc { get; set; }

    public DateTime? AProperty { get; set; } = DateTime.UtcNow;
}";

                await Verify.NoDiagnosticExists<NonPlatform_DomainDrivenDesignAnalyzer>(input);
            }

            [Fact]
            public async Task WhenAnyPropertyValueTypeIsInitializedAndRequiredAndNotNullable_ThenNoAlert()
            {
                const string input = @"
using System;
using Domain.Interfaces.Entities;
namespace ANamespace;
public sealed class AClassed : IDomainEvent
{
    public static AClassed Create()
    {
        return new AClassed
        {
            AProperty = DateTime.UtcNow,
            RootId = string.Empty,
            OccurredUtc = DateTime.UtcNow
        };
    }

    public required string RootId { get; set; }

    public required DateTime OccurredUtc { get; set; }

    public required DateTime AProperty { get; set; } = DateTime.UtcNow;
}";

                await Verify.NoDiagnosticExists<NonPlatform_DomainDrivenDesignAnalyzer>(input);
            }

            [Fact]
            public async Task WhenAnyPropertyValueTypeIsInitializedAndNullableAndNotRequired_ThenNoAlert()
            {
                const string input = @"
using System;
using Domain.Interfaces.Entities;
namespace ANamespace;
public sealed class AClassed : IDomainEvent
{
    public static AClassed Create()
    {
        return new AClassed
        {
            RootId = string.Empty,
            OccurredUtc = DateTime.UtcNow
        };
    }

    public required string RootId { get; set; }

    public required DateTime OccurredUtc { get; set; }

    public DateTime? AProperty { get; set; } = DateTime.UtcNow;
}";

                await Verify.NoDiagnosticExists<NonPlatform_DomainDrivenDesignAnalyzer>(input);
            }

            [Fact]
            public async Task WhenAnyPropertyValueTypeIsInitializedAndRequiredAndNullable_ThenNoAlert()
            {
                const string input = @"
using System;
using Domain.Interfaces.Entities;
namespace ANamespace;
public sealed class AClassed : IDomainEvent
{
    public static AClassed Create()
    {
        return new AClassed
        {
            AProperty = DateTime.UtcNow,
            RootId = string.Empty,
            OccurredUtc = DateTime.UtcNow
        };
    }

    public required string RootId { get; set; }

    public required DateTime OccurredUtc { get; set; }

    public required DateTime? AProperty { get; set; } = DateTime.UtcNow;
}";

                await Verify.NoDiagnosticExists<NonPlatform_DomainDrivenDesignAnalyzer>(input);
            }

            [Fact]
            public async Task WhenAnyPropertyEnumTypeIsNotRequiredAndNotInitializedAndNotNullable_ThenNoAlert()
            {
                const string input = @"
using System;
using Domain.Interfaces.Entities;
namespace ANamespace;
public sealed class AClassed : IDomainEvent
{
    public static AClassed Create()
    {
        return new AClassed
        {
            RootId = string.Empty,
            OccurredUtc = DateTime.UtcNow
        };
    }

    public required string RootId { get; set; }

    public required DateTime OccurredUtc { get; set; }

    public AnEnum AProperty { get; set; }
}
public enum AnEnum
{
    AValue
}";

                await Verify.NoDiagnosticExists<NonPlatform_DomainDrivenDesignAnalyzer>(input);
            }

            [Fact]
            public async Task WhenAnyPropertyEnumValueTypeIsRequiredAndNotInitializedAndNotNullable_ThenNoAlert()
            {
                const string input = @"
using System;
using Domain.Interfaces.Entities;
namespace ANamespace;
public sealed class AClassed : IDomainEvent
{
    public static AClassed Create()
    {
        return new AClassed
        {
            AProperty = AnEnum.AValue,
            RootId = string.Empty,
            OccurredUtc = DateTime.UtcNow
        };
    }

    public required string RootId { get; set; }

    public required DateTime OccurredUtc { get; set; }

    public required AnEnum AProperty { get; set; }
}
public enum AnEnum
{
    AValue
}";

                await Verify.NoDiagnosticExists<NonPlatform_DomainDrivenDesignAnalyzer>(input);
            }

            [Fact]
            public async Task WhenAnyPropertyEnumTypeIsInitializedAndNotRequiredAndNotNullable_ThenNoAlert()
            {
                const string input = @"
using System;
using Domain.Interfaces.Entities;
namespace ANamespace;
public sealed class AClassed : IDomainEvent
{
    public static AClassed Create()
    {
        return new AClassed
        {
            RootId = string.Empty,
            OccurredUtc = DateTime.UtcNow
        };
    }

    public required string RootId { get; set; }

    public required DateTime OccurredUtc { get; set; }

    public AnEnum AProperty { get; set; } = AnEnum.AValue;
}
public enum AnEnum
{
    AValue
}";

                await Verify.NoDiagnosticExists<NonPlatform_DomainDrivenDesignAnalyzer>(input);
            }

            [Fact]
            public async Task WhenAnyPropertyEnumTypeIsNullableAndNotRequiredAndNotInitialized_ThenNoAlert()
            {
                const string input = @"
using System;
using Domain.Interfaces.Entities;
namespace ANamespace;
public sealed class AClassed : IDomainEvent
{
    public static AClassed Create()
    {
        return new AClassed
        {
            RootId = string.Empty,
            OccurredUtc = DateTime.UtcNow
        };
    }

    public required string RootId { get; set; }

    public required DateTime OccurredUtc { get; set; }

    public AnEnum? AProperty { get; set; }
}
public enum AnEnum
{
    AValue
}";

                await Verify.NoDiagnosticExists<NonPlatform_DomainDrivenDesignAnalyzer>(input);
            }

            [Fact]
            public async Task WhenAnyPropertyEnumTypeIsNullableAndRequiredAndNotInitialized_ThenNoAlert()
            {
                const string input = @"
using System;
using Domain.Interfaces.Entities;
namespace ANamespace;
public sealed class AClassed : IDomainEvent
{
    public static AClassed Create()
    {
        return new AClassed
        {
            AProperty = AnEnum.AValue,
            RootId = string.Empty,
            OccurredUtc = DateTime.UtcNow
        };
    }

    public required string RootId { get; set; }

    public required DateTime OccurredUtc { get; set; }

    public required AnEnum? AProperty { get; set; }
}
public enum AnEnum
{
    AValue
}";

                await Verify.NoDiagnosticExists<NonPlatform_DomainDrivenDesignAnalyzer>(input);
            }

            [Fact]
            public async Task WhenAnyPropertyEnumTypeIsNullableAndInitializedAndNotRequired_ThenNoAlert()
            {
                const string input = @"
using System;
using Domain.Interfaces.Entities;
namespace ANamespace;
public sealed class AClassed : IDomainEvent
{
    public static AClassed Create()
    {
        return new AClassed
        {
            RootId = string.Empty,
            OccurredUtc = DateTime.UtcNow
        };
    }

    public required string RootId { get; set; }

    public required DateTime OccurredUtc { get; set; }

    public AnEnum? AProperty { get; set; } = AnEnum.AValue;
}
public enum AnEnum
{
    AValue
}";

                await Verify.NoDiagnosticExists<NonPlatform_DomainDrivenDesignAnalyzer>(input);
            }

            [Fact]
            public async Task WhenAnyPropertyEnumTypeIsInitializedAndRequiredAndNotNullable_ThenNoAlert()
            {
                const string input = @"
using System;
using Domain.Interfaces.Entities;
namespace ANamespace;
public sealed class AClassed : IDomainEvent
{
    public static AClassed Create()
    {
        return new AClassed
        {
            AProperty = AnEnum.AValue,
            RootId = string.Empty,
            OccurredUtc = DateTime.UtcNow
        };
    }

    public required string RootId { get; set; }

    public required DateTime OccurredUtc { get; set; }

    public required AnEnum AProperty { get; set; } = AnEnum.AValue;
}
public enum AnEnum
{
    AValue
}";

                await Verify.NoDiagnosticExists<NonPlatform_DomainDrivenDesignAnalyzer>(input);
            }

            [Fact]
            public async Task WhenAnyPropertyEnumTypeIsInitializedAndNullableAndNotRequired_ThenNoAlert()
            {
                const string input = @"
using System;
using Domain.Interfaces.Entities;
namespace ANamespace;
public sealed class AClassed : IDomainEvent
{
    public static AClassed Create()
    {
        return new AClassed
        {
            RootId = string.Empty,
            OccurredUtc = DateTime.UtcNow
        };
    }

    public required string RootId { get; set; }

    public required DateTime OccurredUtc { get; set; }

    public AnEnum? AProperty { get; set; } = AnEnum.AValue;
}
public enum AnEnum
{
    AValue
}";

                await Verify.NoDiagnosticExists<NonPlatform_DomainDrivenDesignAnalyzer>(input);
            }

            [Fact]
            public async Task WhenAnyPropertyEnumTypeIsInitializedAndRequiredAndNullable_ThenNoAlert()
            {
                const string input = @"
using System;
using Domain.Interfaces.Entities;
namespace ANamespace;
public sealed class AClassed : IDomainEvent
{
    public static AClassed Create()
    {
        return new AClassed
        {
            AProperty = AnEnum.AValue,
            RootId = string.Empty,
            OccurredUtc = DateTime.UtcNow
        };
    }

    public required string RootId { get; set; }

    public required DateTime OccurredUtc { get; set; }

    public required AnEnum? AProperty { get; set; } = AnEnum.AValue;
}
public enum AnEnum
{
    AValue
}";

                await Verify.NoDiagnosticExists<NonPlatform_DomainDrivenDesignAnalyzer>(input);
            }
        }

        [Trait("Category", "Unit.Tooling")]
        public class GivenRule048
        {
            [Fact]
            public async Task WhenAnyPropertyReferenceTypeIsOptional_ThenAlerts()
            {
                const string input = @"
using System;
using Domain.Interfaces.Entities;
using Common;
namespace ANamespace;
public sealed class AClassed : IDomainEvent
{
    public static AClassed Create()
    {
        return new AClassed
        {
            AProperty = string.Empty,
            RootId = string.Empty,
            OccurredUtc = DateTime.UtcNow
        };
    }

    public required string RootId { get; set; }

    public required DateTime OccurredUtc { get; set; }

    public required Optional<string> AProperty { get; set; }
}";

                await Verify.DiagnosticExists<NonPlatform_DomainDrivenDesignAnalyzer>(input,
                    (NonPlatform_DomainDrivenDesignAnalyzer.Rule048, 22, 38, "AProperty", null),
                    (NonPlatform_DomainDrivenDesignAnalyzer.Rule049, 22, 38, "AProperty", [GivenRule049.AllTypes]));
            }

            [Fact]
            public async Task WhenAnyPropertyReferenceTypeIsNullable_ThenNoAlert()
            {
                const string input = @"
using System;
using Domain.Interfaces.Entities;
namespace ANamespace;
public sealed class AClassed : IDomainEvent
{
    public static AClassed Create()
    {
        return new AClassed
        {
            AProperty = string.Empty,
            RootId = string.Empty,
            OccurredUtc = DateTime.UtcNow
        };
    }

    public required string RootId { get; set; }

    public required DateTime OccurredUtc { get; set; }

    public string? AProperty { get; set; }
}";

                await Verify.NoDiagnosticExists<NonPlatform_DomainDrivenDesignAnalyzer>(input);
            }
        }

        [Trait("Category", "Unit.Tooling")]
        public class GivenRule049
        {
            public const string AllTypes =
                "bool or string or ulong or int or long or double or decimal or System.DateTime or byte";

            [Fact]
            public async Task WhenAnyPropertyIsNotSupportedPrimitive_ThenAlerts()
            {
                const string input = @"
using System;
using Domain.Interfaces.Entities;
namespace ANamespace;
public sealed class AClassed : IDomainEvent
{
    public static AClassed Create()
    {
        return new AClassed
        {
            AProperty = 'a',
            RootId = string.Empty,
            OccurredUtc = DateTime.UtcNow
        };
    }

    public required string RootId { get; set; }

    public required DateTime OccurredUtc { get; set; }

    public required char AProperty { get; set; }
}";

                await Verify.DiagnosticExists<NonPlatform_DomainDrivenDesignAnalyzer>(
                    NonPlatform_DomainDrivenDesignAnalyzer.Rule049, input, 21, 26, "AProperty", AllTypes);
            }

            [Fact]
            public async Task WhenAnyPropertyIsNotSupportedListOfPrimitive_ThenAlerts()
            {
                const string input = @"
using System;
using System.Collections.Generic;
using Domain.Interfaces.Entities;
namespace ANamespace;
public sealed class AClassed : IDomainEvent
{
    public static AClassed Create()
    {
        return new AClassed
        {
            AProperty = new List<char>(),
            RootId = string.Empty,
            OccurredUtc = DateTime.UtcNow
        };
    }

    public required string RootId { get; set; }

    public required DateTime OccurredUtc { get; set; }

    public required List<char> AProperty { get; set; }
}";

                await Verify.DiagnosticExists<NonPlatform_DomainDrivenDesignAnalyzer>(
                    NonPlatform_DomainDrivenDesignAnalyzer.Rule049, input, 22, 32, "AProperty", AllTypes);
            }

            [Fact]
            public async Task WhenAnyPropertyIsNotSupportedDictionaryOfPrimitive_ThenAlerts()
            {
                const string input = @"
using System;
using System.Collections.Generic;
using Domain.Interfaces.Entities;
namespace ANamespace;
public sealed class AClassed : IDomainEvent
{
    public static AClassed Create()
    {
        return new AClassed
        {
            AProperty = new Dictionary<string, char>(),
            RootId = string.Empty,
            OccurredUtc = DateTime.UtcNow
        };
    }

    public required string RootId { get; set; }

    public required DateTime OccurredUtc { get; set; }

    public required Dictionary<string, char> AProperty { get; set; }
}";

                await Verify.DiagnosticExists<NonPlatform_DomainDrivenDesignAnalyzer>(
                    NonPlatform_DomainDrivenDesignAnalyzer.Rule049, input, 22, 46, "AProperty", AllTypes);
            }

            [Fact]
            public async Task WhenAnyPropertyIsNotSupportedDictionaryKeyType_ThenAlerts()
            {
                const string input = @"
using System;
using System.Collections.Generic;
using Domain.Interfaces.Entities;
namespace ANamespace;
public sealed class AClassed : IDomainEvent
{
    public static AClassed Create()
    {
        return new AClassed
        {
            AProperty = new Dictionary<char, string>(),
            RootId = string.Empty,
            OccurredUtc = DateTime.UtcNow
        };
    }

    public required string RootId { get; set; }

    public required DateTime OccurredUtc { get; set; }

    public required Dictionary<char, string> AProperty { get; set; }
}";

                await Verify.DiagnosticExists<NonPlatform_DomainDrivenDesignAnalyzer>(
                    NonPlatform_DomainDrivenDesignAnalyzer.Rule049, input, 22, 46, "AProperty", AllTypes);
            }

            [Fact]
            public async Task WhenAnyPropertyIsSupportedPrimitive_ThenNoAlert()
            {
                const string input = @"
using System;
using Domain.Interfaces.Entities;
namespace ANamespace;
public sealed class AClassed : IDomainEvent
{
    public static AClassed Create()
    {
        return new AClassed
        {
            AProperty = string.Empty,
            RootId = string.Empty,
            OccurredUtc = DateTime.UtcNow
        };
    }

    public required string RootId { get; set; }

    public required DateTime OccurredUtc { get; set; }

    public required string AProperty { get; set; }
}";

                await Verify.NoDiagnosticExists<NonPlatform_DomainDrivenDesignAnalyzer>(input);
            }

            [Fact]
            public async Task WhenAnyPropertyIsEnum_ThenNoAlert()
            {
                const string input = @"
using System;
using Domain.Interfaces.Entities;
namespace ANamespace;
public sealed class AClassed : IDomainEvent
{
    public static AClassed Create()
    {
        return new AClassed
        {
            RootId = string.Empty,
            OccurredUtc = DateTime.UtcNow
        };
    }

    public required string RootId { get; set; }

    public required DateTime OccurredUtc { get; set; }

    public AnEnum AProperty { get; set; }
}
public enum AnEnum
{
    AValue
}
";

                await Verify.NoDiagnosticExists<NonPlatform_DomainDrivenDesignAnalyzer>(input);
            }

            [Fact]
            public async Task WhenAnyPropertyIsSupportedListOfPrimitive_ThenNoAlert()
            {
                const string input = @"
using System;
using System.Collections.Generic;
using Domain.Interfaces.Entities;
namespace ANamespace;
public sealed class AClassed : IDomainEvent
{
    public static AClassed Create()
    {
        return new AClassed
        {
            AProperty = new List<string>(),
            RootId = string.Empty,
            OccurredUtc = DateTime.UtcNow
        };
    }

    public required string RootId { get; set; }

    public required DateTime OccurredUtc { get; set; }

    public required List<string> AProperty { get; set; }
}";

                await Verify.NoDiagnosticExists<NonPlatform_DomainDrivenDesignAnalyzer>(input);
            }

            [Fact]
            public async Task WhenAnyPropertyIsSupportedDictionaryOfPrimitive_ThenNoAlert()
            {
                const string input = @"
using System;
using System.Collections.Generic;
using Domain.Interfaces.Entities;
namespace ANamespace;
public sealed class AClassed : IDomainEvent
{
    public static AClassed Create()
    {
        return new AClassed
        {
            AProperty = new Dictionary<string, int>(),
            RootId = string.Empty,
            OccurredUtc = DateTime.UtcNow
        };
    }

    public required string RootId { get; set; }

    public required DateTime OccurredUtc { get; set; }

    public required Dictionary<string, int> AProperty { get; set; }
}";

                await Verify.NoDiagnosticExists<NonPlatform_DomainDrivenDesignAnalyzer>(input);
            }

            [Fact]
            public async Task WhenAnyPropertyIsSupportedListOfDomainEventType_ThenNoAlert()
            {
                const string input = @"
using System;
using System.Collections.Generic;
using Domain.Interfaces.Entities;
namespace ANamespace
{
    public sealed class AClassed : IDomainEvent
    {
        public static AClassed Create()
        {
            return new AClassed
            {
                AProperty = new List<Domain.Events.Shared.AType>(),
                RootId = string.Empty,
                OccurredUtc = DateTime.UtcNow
            };
        }

        public required string RootId { get; set; }

        public required DateTime OccurredUtc { get; set; }

        public required List<Domain.Events.Shared.AType> AProperty { get; set; }
    }
}
namespace Domain.Events.Shared
{
    public class AType
    {
    }
}";

                await Verify.NoDiagnosticExists<NonPlatform_DomainDrivenDesignAnalyzer>(input);
            }

            [Fact]
            public async Task WhenAnyPropertyIsSupportedDictionaryOfDomainEventType_ThenNoAlert()
            {
                const string input = @"
using System;
using System.Collections.Generic;
using Domain.Interfaces.Entities;
namespace ANamespace
{
    public sealed class AClassed : IDomainEvent
    {
        public static AClassed Create()
        {
            return new AClassed
            {
                AProperty = new Dictionary<string, Domain.Events.Shared.AType>(),
                RootId = string.Empty,
                OccurredUtc = DateTime.UtcNow
            };
        }

        public required string RootId { get; set; }

        public required DateTime OccurredUtc { get; set; }

        public required Dictionary<string, Domain.Events.Shared.AType> AProperty { get; set; }
    }
}
namespace Domain.Events.Shared
{
    public class AType
    {
    }
}";

                await Verify.NoDiagnosticExists<NonPlatform_DomainDrivenDesignAnalyzer>(input);
            }

            [Fact]
            public async Task WhenAnyPropertyIsNotADto_ThenAlerts()
            {
                const string input = @"
using System;
using System.Collections.Generic;
using Domain.Interfaces.Entities;
namespace ANamespace;
public sealed class AClassed : IDomainEvent
{
    public static AClassed Create()
    {
        return new AClassed
        {
            AProperty = new ADto(),
            RootId = string.Empty,
            OccurredUtc = DateTime.UtcNow
        };
    }

    public required string RootId { get; set; }

    public required DateTime OccurredUtc { get; set; }

    public required ADto AProperty { get; set; }
}
public class ADto
{
    public string AProperty { get; }
}";

                await Verify.DiagnosticExists<NonPlatform_DomainDrivenDesignAnalyzer>(
                    NonPlatform_DomainDrivenDesignAnalyzer.Rule049, input, 22, 26, "AProperty", AllTypes);
            }

            [Fact]
            public async Task WhenAnyPropertyIsADto_ThenNoAlert()
            {
                const string input = @"
using System;
using System.Collections.Generic;
using Domain.Interfaces.Entities;
using AnOtherNamespace;
namespace ANamespace
{
    public sealed class AClassed : IDomainEvent
    {
        public static AClassed Create()
        {
            return new AClassed
            {
                AProperty = new ADto { AProperty1 = string.Empty },
                RootId = string.Empty,
                OccurredUtc = DateTime.UtcNow
            };
        }

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

                await Verify.NoDiagnosticExists<NonPlatform_DomainDrivenDesignAnalyzer>(input);
            }
        }
    }
}