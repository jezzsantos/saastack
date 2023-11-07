extern alias Analyzers;
using JetBrains.Annotations;
using Xunit;
using DomainDrivenDesignAnalyzer = Analyzers::Tools.Analyzers.Platform.DomainDrivenDesignAnalyzer;
using DomainDrivenDesignCodeFix = Analyzers::Tools.Analyzers.Platform.DomainDrivenDesignCodeFix;

namespace Tools.Analyzers.Platform.UnitTests;

extern alias Analyzers;

[UsedImplicitly]
public class DomainDrivenDesignCodeFixSpec
{
    [UsedImplicitly]
    public class GivenARootAggregate
    {
        [Trait("Category", "Unit")]
        public class GivenRuleSas034
        {
            [Fact]
            public async Task WhenFixingMissingRehydrateMethodAndDehydratable_ThenAddsMethod()
            {
                const string problem = @"
using System;
using System.Collections.Generic;
using Common;
using Domain.Common;
using Domain.Common.Entities;
using Domain.Common.Identity;
using Domain.Common.ValueObjects;
using Domain.Interfaces.Entities;
using Domain.Interfaces.Services;
using QueryAny;
namespace ANamespace;
[EntityName(""AClass"")]
public class AClass : AggregateRootBase
{
    private AClass(IRecorder recorder, IIdentifierFactory idFactory) : base(recorder, idFactory)
    {
    }

    private AClass(Identifier identifier, IDependencyContainer container, IReadOnlyDictionary<string, object?> rehydratingProperties) : base(identifier, container, rehydratingProperties)
    {
    }

    protected override Result<Error> OnStateChanged(IDomainEvent @event, bool isReconstituting)
    {
        return Result.Ok;
    }

    public static AClass Create()
    {
        var root = new AClass(null!, null!);
        root.RaiseCreateEvent(new CreateEvent());
        return root;
    }

    public override Dictionary<string, object?> Dehydrate()
    {
        return base.Dehydrate();
    }
}
public class CreateEvent : IDomainEvent
{
    public string RootId { get; set; } = ""anid"";

    public DateTime OccurredUtc { get; set; } = DateTime.UtcNow;
}";
                const string fix = @"
using System;
using System.Collections.Generic;
using Common;
using Domain.Common;
using Domain.Common.Entities;
using Domain.Common.Identity;
using Domain.Common.ValueObjects;
using Domain.Interfaces.Entities;
using Domain.Interfaces.Services;
using QueryAny;
namespace ANamespace;
[EntityName(""AClass"")]
public class AClass : AggregateRootBase
{
    private AClass(IRecorder recorder, IIdentifierFactory idFactory) : base(recorder, idFactory)
    {
    }

    private AClass(Identifier identifier, IDependencyContainer container, IReadOnlyDictionary<string, object?> rehydratingProperties) : base(identifier, container, rehydratingProperties)
    {
    }

    protected override Result<Error> OnStateChanged(IDomainEvent @event, bool isReconstituting)
    {
        return Result.Ok;
    }

    public static AClass Create()
    {
        var root = new AClass(null!, null!);
        root.RaiseCreateEvent(new CreateEvent());
        return root;
    }

    public override Dictionary<string, object?> Dehydrate()
    {
        return base.Dehydrate();
    }

    public static AggregateRootFactory<AClass> Rehydrate()
    {
        return (identifier, container, properties) => new AClass(identifier, container, properties);
    }
}
public class CreateEvent : IDomainEvent
{
    public string RootId { get; set; } = ""anid"";

    public DateTime OccurredUtc { get; set; } = DateTime.UtcNow;
}";

                await Verify.CodeFixed<DomainDrivenDesignAnalyzer, DomainDrivenDesignCodeFix>(
                    DomainDrivenDesignAnalyzer.Sas034,
                    problem, fix, 14, 14, "AClass");
            }

            [Fact]
            public async Task WhenFixingMissingRehydrateMethodAndNotDehydratable_ThenAddsMethod()
            {
                const string problem = @"
using System;
using Common;
using Domain.Common;
using Domain.Common.Entities;
using Domain.Common.Identity;
using Domain.Common.ValueObjects;
using Domain.Interfaces.Entities;
namespace ANamespace;
public class AClass : AggregateRootBase
{
    private AClass(IRecorder recorder, IIdentifierFactory idFactory) : base(recorder, idFactory)
    {
    }

    private AClass(IRecorder recorder, IIdentifierFactory idFactory, Identifier identifier) : base(recorder, idFactory, identifier)
    {
    }

    protected override Result<Error> OnStateChanged(IDomainEvent @event, bool isReconstituting)
    {
        return Result.Ok;
    }

    public static AClass Create()
    {
        var root = new AClass(null!, null!);
        root.RaiseCreateEvent(new CreateEvent());
        return root;
    }
}
public class CreateEvent : IDomainEvent
{
    public string RootId { get; set; } = ""anid"";

    public DateTime OccurredUtc { get; set; } = DateTime.UtcNow;
}";
                const string fix = @"
using System;
using Common;
using Domain.Common;
using Domain.Common.Entities;
using Domain.Common.Identity;
using Domain.Common.ValueObjects;
using Domain.Interfaces.Entities;
namespace ANamespace;
public class AClass : AggregateRootBase
{
    private AClass(IRecorder recorder, IIdentifierFactory idFactory) : base(recorder, idFactory)
    {
    }

    private AClass(IRecorder recorder, IIdentifierFactory idFactory, Identifier identifier) : base(recorder, idFactory, identifier)
    {
    }

    protected override Result<Error> OnStateChanged(IDomainEvent @event, bool isReconstituting)
    {
        return Result.Ok;
    }

    public static AClass Create()
    {
        var root = new AClass(null!, null!);
        root.RaiseCreateEvent(new CreateEvent());
        return root;
    }

    public static AggregateRootFactory<AClass> Rehydrate()
    {
        return (identifier, container, properties) => new AClass(container.Resolve<IRecorder>(), container.Resolve<IIdentifierFactory>(), identifier);
    }
}
public class CreateEvent : IDomainEvent
{
    public string RootId { get; set; } = ""anid"";

    public DateTime OccurredUtc { get; set; } = DateTime.UtcNow;
}";

                await Verify.CodeFixed<DomainDrivenDesignAnalyzer, DomainDrivenDesignCodeFix>(
                    DomainDrivenDesignAnalyzer.Sas034,
                    problem, fix, 10, 14, "AClass");
            }
        }
    }

    [UsedImplicitly]
    public class GivenAnEntity
    {
        [Trait("Category", "Unit")]
        public class GivenRuleSas043
        {
            [Fact]
            public async Task WhenFixingMissingRehydrateMethodAndDehydratable_ThenAddsMethod()
            {
                const string problem = @"
using System;
using System.Collections.Generic;
using Common;
using Domain.Common;
using Domain.Common.Entities;
using Domain.Common.Identity;
using Domain.Common.ValueObjects;
using Domain.Interfaces.Entities;
using Domain.Interfaces.Services;
using QueryAny;
namespace ANamespace;
[EntityName(""AClass"")]
public class AClass : EntityBase
{
    private AClass(IRecorder recorder, IIdentifierFactory idFactory, RootEventHandler rootEventHandler) : base(recorder, idFactory, rootEventHandler)
    {
    }

    private AClass(Identifier identifier, IDependencyContainer container, IReadOnlyDictionary<string, object?> rehydratingProperties) : base(identifier, container, rehydratingProperties)
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

    public override Dictionary<string, object?> Dehydrate()
    {
        return base.Dehydrate();
    }
}";
                const string fix = @"
using System;
using System.Collections.Generic;
using Common;
using Domain.Common;
using Domain.Common.Entities;
using Domain.Common.Identity;
using Domain.Common.ValueObjects;
using Domain.Interfaces.Entities;
using Domain.Interfaces.Services;
using QueryAny;
namespace ANamespace;
[EntityName(""AClass"")]
public class AClass : EntityBase
{
    private AClass(IRecorder recorder, IIdentifierFactory idFactory, RootEventHandler rootEventHandler) : base(recorder, idFactory, rootEventHandler)
    {
    }

    private AClass(Identifier identifier, IDependencyContainer container, IReadOnlyDictionary<string, object?> rehydratingProperties) : base(identifier, container, rehydratingProperties)
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

    public override Dictionary<string, object?> Dehydrate()
    {
        return base.Dehydrate();
    }

    public static EntityFactory<AClass> Rehydrate()
    {
        return (identifier, container, properties) => new AClass(identifier, container, properties);
    }
}";

                await Verify.CodeFixed<DomainDrivenDesignAnalyzer, DomainDrivenDesignCodeFix>(
                    DomainDrivenDesignAnalyzer.Sas043,
                    problem, fix, 14, 14, "AClass");
            }
        }
    }

    [UsedImplicitly]
    public class GivenAValueObject
    {
        [Trait("Category", "Unit")]
        public class GivenRuleSas053
        {
            [Fact]
            public async Task WhenFixingMissingRehydrateMethodAndSingleValueObject_ThenAddsMethod()
            {
                const string problem = @"
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
public class AClass : SingleValueObjectBase<AClass, string>
{
    private AClass(string avalue1): base(avalue1)
    {
        AProperty = avalue1;
    }

    public static AClass Create()
    {
        return new AClass(null!);
    }

    public string AProperty { get;}
}";
                const string fix = @"
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
public class AClass : SingleValueObjectBase<AClass, string>
{
    private AClass(string avalue1): base(avalue1)
    {
        AProperty = avalue1;
    }

    public static AClass Create()
    {
        return new AClass(null!);
    }

    public string AProperty { get;}

    public static ValueObjectFactory<AClass> Rehydrate()
    {
        return (property, container) =>
        {
            var parts = RehydrateToList(property, true);
            return new AClass(parts[0]);
        };
    }
}";

                await Verify.CodeFixed<DomainDrivenDesignAnalyzer, DomainDrivenDesignCodeFix>(
                    DomainDrivenDesignAnalyzer.Sas053,
                    problem, fix, 12, 14, "AClass");
            }

            [Fact]
            public async Task WhenFixingMissingRehydrateMethodAndMultiValueObject_ThenAddsMethod()
            {
                const string problem = @"
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
public class AClass : ValueObjectBase<AClass>
{
    private AClass(string avalue1, string avalue2, string avalue3)
    {
        AProperty = avalue1;
    }

    public static AClass Create()
    {
        return new AClass(null!, null!, null!);
    }

    protected override IEnumerable<object?> GetAtomicValues()
    {
        return new object[] { AProperty };
    }

    public string AProperty { get;}
}";
                const string fix = @"
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
public class AClass : ValueObjectBase<AClass>
{
    private AClass(string avalue1, string avalue2, string avalue3)
    {
        AProperty = avalue1;
    }

    public static AClass Create()
    {
        return new AClass(null!, null!, null!);
    }

    protected override IEnumerable<object?> GetAtomicValues()
    {
        return new object[] { AProperty };
    }

    public string AProperty { get;}

    public static ValueObjectFactory<AClass> Rehydrate()
    {
        return (property, container) =>
        {
            var parts = RehydrateToList(property, false);
            return new AClass(parts[0], parts[1], parts[2]);
        };
    }
}";

                await Verify.CodeFixed<DomainDrivenDesignAnalyzer, DomainDrivenDesignCodeFix>(
                    DomainDrivenDesignAnalyzer.Sas053,
                    problem, fix, 12, 14, "AClass");
            }
        }
    }
}