using System.Reflection;
using FluentAssertions;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Infrastructure.WebApi.Common.UnitTests;

[Trait("Category", "Unit")]
public class SubDomainModulesSpec
{
    private readonly SubDomainModules _modules = new();

    [Fact]
    public void WhenRegisterAndNullModule_ThenThrows()
    {
        _modules
            .Invoking(x => x.Register(null!))
            .Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void WhenRegisterAndNullApiAssembly_ThenThrows()
    {
        _modules
            .Invoking(x => x.Register(new TestModule { ApiAssembly = null! }))
            .Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void WhenRegisterAndNullAggregatePrefixes_ThenThrows()
    {
        _modules
            .Invoking(x => x.Register(new TestModule { AggregatePrefixes = null! }))
            .Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void WhenRegisterAndNullMinimalApiRegistrationFunction_ThenThrows()
    {
        _modules
            .Invoking(x => x.Register(new TestModule
                { ApiAssembly = typeof(SubDomainModulesSpec).Assembly, RegisterServicesFunction = (_, _) => { } }))
            .Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void WhenRegisterAndNullRegisterServicesFunction_ThenRegisters()
    {
        _modules.Register(new TestModule
        {
            ApiAssembly = typeof(SubDomainModulesSpec).Assembly, MinimalApiRegistrationFunction = _ => { },
            AggregatePrefixes = new Dictionary<Type, string>(),
            RegisterServicesFunction = null!
        });

        _modules.ApiAssemblies.Should().ContainSingle();
    }

    [Fact]
    public void WhenRegisterServicesAndNoModules_ThenAppliedToAllModules()
    {
        var configuration = new ConfigurationManager();
        var services = new ServiceCollection();

        _modules.RegisterServices(configuration, services);
    }

    [Fact]
    public void WhenRegisterServices_ThenAppliedToAllModules()
    {
        var configuration = new ConfigurationManager();
        var services = new ServiceCollection();
        var wasCalled = false;
        _modules.Register(new TestModule
        {
            ApiAssembly = typeof(SubDomainModulesSpec).Assembly,
            AggregatePrefixes = new Dictionary<Type, string>(),
            MinimalApiRegistrationFunction = _ => { },
            RegisterServicesFunction = (_, _) => { wasCalled = true; }
        });

        _modules.RegisterServices(configuration, services);

        wasCalled.Should().BeTrue();
    }

    [Fact]
    public void WhenConfigureHostAndNoModules_ThenAppliedToAllModules()
    {
        var app = WebApplication.Create();

        _modules.ConfigureHost(app);
    }

    [Fact]
    public void WhenConfigureHost_ThenAppliedToAllModules()
    {
        var app = WebApplication.Create();
        var wasCalled = false;
        _modules.Register(new TestModule
        {
            ApiAssembly = typeof(SubDomainModulesSpec).Assembly,
            AggregatePrefixes = new Dictionary<Type, string>(),
            MinimalApiRegistrationFunction = _ => { wasCalled = true; },
            RegisterServicesFunction = (_, _) => { }
        });

        _modules.ConfigureHost(app);

        wasCalled.Should().BeTrue();
    }
}

public class TestModule : ISubDomainModule
{
    public Assembly ApiAssembly { get; init; } = null!;

    public Dictionary<Type, string> AggregatePrefixes { get; init; } = null!;

    public Action<WebApplication> MinimalApiRegistrationFunction { get; init; } = null!;

    public Action<ConfigurationManager, IServiceCollection>? RegisterServicesFunction { get; init; }
}