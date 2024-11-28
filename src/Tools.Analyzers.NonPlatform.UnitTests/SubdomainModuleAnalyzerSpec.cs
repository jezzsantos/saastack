extern alias NonPlatformAnalyzers;
using NonPlatformAnalyzers::JetBrains.Annotations;
using Xunit;
using SubdomainModuleAnalyzer = NonPlatformAnalyzers::Tools.Analyzers.NonPlatform.SubdomainModuleAnalyzer;

namespace Tools.Analyzers.NonPlatform.UnitTests;

[UsedImplicitly]
public class SubdomainModuleAnalyzerSpec
{
    [UsedImplicitly]
    public class GivenAnySubdomainModule
    {
        [Trait("Category", "Unit.Tooling")]
        public class GivenAnyRule
        {
            [Fact]
            public async Task WhenDomainAssembly_ThenNoAlert()
            {
                const string input = @"
using System;
using System.Collections.Generic;
using System.Reflection;
using Infrastructure.Web.Hosting.Common;
namespace ANamespace;
public class AClass : ISubdomainModule
{
    public Assembly InfrastructureAssembly => null!;

    public Assembly? DomainAssembly => null;

    public Dictionary<Type, string> EntityPrefixes => new Dictionary<Type, string>();
}";

                await Verify.NoDiagnosticExists<SubdomainModuleAnalyzer>(input);
            }
        }

        [Trait("Category", "Unit.Tooling")]
        public class GivenRule010
        {
            [Fact]
            public async Task WhenNoAggregatesInDomainAssembly_ThenNoAlert()
            {
                const string input = @"
using System;
using System.Collections.Generic;
using System.Reflection;
using Infrastructure.Web.Hosting.Common;
namespace ANamespace;
public class AClass : ISubdomainModule
{
    public Assembly InfrastructureAssembly => null!;

    public Assembly? DomainAssembly => typeof(AnAggregateRoot).Assembly;

    public Dictionary<Type, string> EntityPrefixes => new Dictionary<Type, string>();
}

public class AnAggregateRoot
{
}
";

                await Verify.NoDiagnosticExists<SubdomainModuleAnalyzer>(input);
            }

            [Fact]
            public async Task WhenAggregateRootNotRegistered_ThenAlerts()
            {
                const string input = @"
using System;
using System.Collections.Generic;
using System.Reflection;
using Infrastructure.Web.Hosting.Common;
using Domain.Interfaces.Entities;
namespace ANamespace;
public class AClass : ISubdomainModule
{
    public Assembly InfrastructureAssembly => null!;

    public Assembly? DomainAssembly => typeof(AnAggregateRoot).Assembly;

    public Dictionary<Type, string> EntityPrefixes => new Dictionary<Type, string>();
}

public class AnAggregateRoot : IAggregateRoot
{
}
";

                await Verify.DiagnosticExists<SubdomainModuleAnalyzer>(
                    SubdomainModuleAnalyzer.Rule010, input, 14, 37, "EntityPrefixes", "AnAggregateRoot");
            }

            [Fact]
            public async Task WhenAggregateRootIsRegistered_ThenNoAlert()
            {
                const string input = @"
using System;
using System.Collections.Generic;
using System.Reflection;
using Infrastructure.Web.Hosting.Common;
using Domain.Interfaces.Entities;
namespace ANamespace;
public class AClass : ISubdomainModule
{
    public Assembly InfrastructureAssembly => null!;

    public Assembly? DomainAssembly => typeof(AnAggregateRoot).Assembly;

    public Dictionary<Type, string> EntityPrefixes => new Dictionary<Type, string>
    {
        { typeof(AnAggregateRoot), ""aprefix"" }
    };
}

public class AnAggregateRoot : IAggregateRoot
{
}
";

                await Verify.NoDiagnosticExists<SubdomainModuleAnalyzer>(input);
            }

            [Fact]
            public async Task WhenNoEntitiesInDomainAssembly_ThenNoAlert()
            {
                const string input = @"
using System;
using System.Collections.Generic;
using System.Reflection;
using Infrastructure.Web.Hosting.Common;
namespace ANamespace;
public class AClass : ISubdomainModule
{
    public Assembly InfrastructureAssembly => null!;

    public Assembly? DomainAssembly => typeof(AnEntity).Assembly;

    public Dictionary<Type, string> EntityPrefixes => new Dictionary<Type, string>();
}

public class AnEntity
{
}
";

                await Verify.NoDiagnosticExists<SubdomainModuleAnalyzer>(input);
            }

            [Fact]
            public async Task WhenEntityNotRegistered_ThenAlerts()
            {
                const string input = @"
using System;
using System.Collections.Generic;
using System.Reflection;
using Infrastructure.Web.Hosting.Common;
using Domain.Interfaces.Entities;
namespace ANamespace;
public class AClass : ISubdomainModule
{
    public Assembly InfrastructureAssembly => null!;

    public Assembly? DomainAssembly => typeof(AnEntity).Assembly;

    public Dictionary<Type, string> EntityPrefixes => new Dictionary<Type, string>();
}

public class AnEntity : IEntity
{
}
";

                await Verify.DiagnosticExists<SubdomainModuleAnalyzer>(
                    SubdomainModuleAnalyzer.Rule010, input, 14, 37, "EntityPrefixes", "AnEntity");
            }

            [Fact]
            public async Task WhenEntityIsRegistered_ThenNoAlert()
            {
                const string input = @"
using System;
using System.Collections.Generic;
using System.Reflection;
using Infrastructure.Web.Hosting.Common;
using Domain.Interfaces.Entities;
namespace ANamespace;
public class AClass : ISubdomainModule
{
    public Assembly InfrastructureAssembly => null!;

    public Assembly? DomainAssembly => typeof(AnEntity).Assembly;

    public Dictionary<Type, string> EntityPrefixes => new Dictionary<Type, string>
    {
        { typeof(AnEntity), ""aprefix"" }
    };
}

public class AnEntity : IEntity
{
}
";

                await Verify.NoDiagnosticExists<SubdomainModuleAnalyzer>(input);
            }

            [Fact]
            public async Task WhenAggregateAndEntityIsRegistered_ThenNoAlert()
            {
                const string input = @"
using System;
using System.Collections.Generic;
using System.Reflection;
using Infrastructure.Web.Hosting.Common;
using Domain.Interfaces.Entities;
namespace ANamespace;
public class AClass : ISubdomainModule
{
    public Assembly InfrastructureAssembly => null!;

    public Assembly? DomainAssembly => typeof(AnEntity).Assembly;

    public Dictionary<Type, string> EntityPrefixes => new Dictionary<Type, string>
    {
        { typeof(AnEntity), ""aprefix"" },
        { typeof(AnAggregateRoot), ""aprefix"" }
    };
}

public class AnEntity : IEntity
{
}
public class AnAggregateRoot : IAggregateRoot
{
}
";

                await Verify.NoDiagnosticExists<SubdomainModuleAnalyzer>(input);
            }
        }
    }
}