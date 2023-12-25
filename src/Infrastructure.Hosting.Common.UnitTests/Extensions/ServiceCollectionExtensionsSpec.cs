using FluentAssertions;
using Infrastructure.Hosting.Common.Extensions;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Infrastructure.Hosting.Common.UnitTests.Extensions;

[Trait("Category", "Unit")]
public class ServiceCollectionExtensionsSpec
{
    [Fact]
    public void WhenRegisterUnsharedWithTwoInterfaces_ThenRegistersTwoWithTheSameInstance()
    {
        var services = new ServiceCollection();

        services.RegisterUnshared<ITestInterface1, ITestInterface2, TestContainerClass>(
            _ => new TestContainerClass());
        using var provider = services.BuildServiceProvider();

        var interface1 = provider.ResolveForUnshared<ITestInterface1>();
        var interface2 = provider.ResolveForUnshared<ITestInterface2>();

        interface1.Should().BeOfType<TestContainerClass>();
        interface1.Should().BeSameAs(interface2);
    }

    [Fact]
    public void WhenRegisterUnsharedWithThreeInterfaces_ThenRegistersThreeWithTheSameInstance()
    {
        var services = new ServiceCollection();

        services.RegisterUnshared<ITestInterface1, ITestInterface2, ITestInterface3, TestContainerClass>(
            _ => new TestContainerClass());
        using var provider = services.BuildServiceProvider();

        var interface1 = provider.ResolveForUnshared<ITestInterface1>();
        var interface2 = provider.ResolveForUnshared<ITestInterface2>();
        var interface3 = provider.ResolveForUnshared<ITestInterface3>();

        interface1.Should().BeOfType<TestContainerClass>();
        interface1.Should().BeSameAs(interface2);
        interface2.Should().BeSameAs(interface3);
        interface3.Should().BeSameAs(interface1);
    }

    [Fact]
    public void WhenRegisterUnsharedWithFourInterfaces_ThenRegistersFourWithTheSameInstance()
    {
        var services = new ServiceCollection();

        services
            .RegisterUnshared<ITestInterface1, ITestInterface2, ITestInterface3, ITestInterface4, TestContainerClass>(
                _ => new TestContainerClass());
        using var provider = services.BuildServiceProvider();

        var interface1 = provider.ResolveForUnshared<ITestInterface1>();
        var interface2 = provider.ResolveForUnshared<ITestInterface2>();
        var interface3 = provider.ResolveForUnshared<ITestInterface3>();
        var interface4 = provider.ResolveForUnshared<ITestInterface4>();

        interface1.Should().BeOfType<TestContainerClass>();
        interface1.Should().BeSameAs(interface2);
        interface2.Should().BeSameAs(interface3);
        interface3.Should().BeSameAs(interface4);
        interface4.Should().BeSameAs(interface1);
    }

    [Fact]
    public void WhenRegisterUnsharedWithFiveInterfaces_ThenRegistersFiveWithTheSameInstance()
    {
        var services = new ServiceCollection();

        services
            .RegisterUnshared<ITestInterface1, ITestInterface2, ITestInterface3, ITestInterface4, ITestInterface5,
                TestContainerClass>(
                _ => new TestContainerClass());
        using var provider = services.BuildServiceProvider();

        var interface1 = provider.ResolveForUnshared<ITestInterface1>();
        var interface2 = provider.ResolveForUnshared<ITestInterface2>();
        var interface3 = provider.ResolveForUnshared<ITestInterface3>();
        var interface4 = provider.ResolveForUnshared<ITestInterface4>();
        var interface5 = provider.ResolveForUnshared<ITestInterface5>();

        interface1.Should().BeOfType<TestContainerClass>();
        interface1.Should().BeSameAs(interface2);
        interface2.Should().BeSameAs(interface3);
        interface3.Should().BeSameAs(interface4);
        interface4.Should().BeSameAs(interface5);
        interface5.Should().BeSameAs(interface1);
    }

    [Fact]
    public void ResolveForPlatformAndNotRegistered_ThenThrows()
    {
        var services = new ServiceCollection();
        var container = services.BuildServiceProvider();

        container
            .Invoking(x => x.ResolveForPlatform<ITestInterface1>())
            .Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void ResolveForPlatformAndRegisteredViaFactory_ThenReturnsService()
    {
        var services = new ServiceCollection();
        var instance = new TestContainerClass();
        services.RegisterPlatform<ITestInterface1>(_ => instance);
        var container = services.BuildServiceProvider();

        var result = container.ResolveForPlatform<ITestInterface1>();

        result.Should().NotBeNull();
    }

    [Fact]
    public void WhenRegisterPlatformWithFourInterfaces_ThenRegistersFourWithTheSameInstance()
    {
        var services = new ServiceCollection();

        services
            .RegisterPlatform<ITestInterface1, ITestInterface2, ITestInterface3, ITestInterface4, TestContainerClass>(
                _ => new TestContainerClass());
        using var provider = services.BuildServiceProvider();

        var interface1 = provider.ResolveForPlatform<ITestInterface1>();
        var interface2 = provider.ResolveForPlatform<ITestInterface2>();
        var interface3 = provider.ResolveForPlatform<ITestInterface3>();
        var interface4 = provider.ResolveForPlatform<ITestInterface4>();

        interface1.Should().BeOfType<TestContainerClass>();
        interface1.Should().BeSameAs(interface2);
        interface2.Should().BeSameAs(interface3);
        interface3.Should().BeSameAs(interface4);
        interface4.Should().BeSameAs(interface1);
    }
}

public interface ITestInterface1
{
}

public interface ITestInterface2
{
}

public interface ITestInterface3
{
}

public interface ITestInterface4
{
}

public interface ITestInterface5
{
}

public class TestContainerClass : ITestInterface1, ITestInterface2, ITestInterface3, ITestInterface4, ITestInterface5,
    IDisposable
{
    public void Dispose()
    {
    }
}