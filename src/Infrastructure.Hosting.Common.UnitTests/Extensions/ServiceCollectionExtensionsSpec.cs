using FluentAssertions;
using Infrastructure.Hosting.Common.Extensions;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Infrastructure.Hosting.Common.UnitTests.Extensions;

[Trait("Category", "Unit")]
public class ServiceCollectionExtensionsSpec
{
    [Fact]
    public void WhenGetRequiredServiceWithTwoInterfaces_ThenRegistersTwoWithTheSameInstance()
    {
        var services = new ServiceCollection();

        services.AddSingleton<ITestInterface1, ITestInterface2, TestContainerClass>(
            _ => new TestContainerClass());
        using var provider = services.BuildServiceProvider();

        var interface1 = provider.GetRequiredService<ITestInterface1>();
        var interface2 = provider.GetRequiredService<ITestInterface2>();

        interface1.Should().BeOfType<TestContainerClass>();
        interface1.Should().Be(interface2);
    }

    [Fact]
    public void WhenGetRequiredServiceWithThreeInterfaces_ThenRegistersThreeWithTheSameInstance()
    {
        var services = new ServiceCollection();

        services.AddSingleton<ITestInterface1, ITestInterface2, ITestInterface3, TestContainerClass>(
            _ => new TestContainerClass());
        using var provider = services.BuildServiceProvider();

        var interface1 = provider.GetRequiredService<ITestInterface1>();
        var interface2 = provider.GetRequiredService<ITestInterface2>();
        var interface3 = provider.GetRequiredService<ITestInterface3>();

        interface1.Should().BeOfType<TestContainerClass>();
        interface1.Should().Be(interface2);
        interface2.Should().Be(interface3);
        interface3.Should().Be(interface1);
    }

    [Fact]
    public void WhenGetRequiredServiceWithFourInterfaces_ThenRegistersFourWithTheSameInstance()
    {
        var services = new ServiceCollection();

        services
            .AddSingleton<ITestInterface1, ITestInterface2, ITestInterface3, ITestInterface4, TestContainerClass>(
                _ => new TestContainerClass());
        using var provider = services.BuildServiceProvider();

        var interface1 = provider.GetRequiredService<ITestInterface1>();
        var interface2 = provider.GetRequiredService<ITestInterface2>();
        var interface3 = provider.GetRequiredService<ITestInterface3>();
        var interface4 = provider.GetRequiredService<ITestInterface4>();

        interface1.Should().BeOfType<TestContainerClass>();
        interface1.Should().Be(interface2);
        interface2.Should().Be(interface3);
        interface3.Should().Be(interface4);
        interface4.Should().Be(interface1);
    }

    [Fact]
    public void WhenGetRequiredServiceWithFiveInterfaces_ThenRegistersFiveWithTheSameInstance()
    {
        var services = new ServiceCollection();

        services
            .AddSingleton<ITestInterface1, ITestInterface2, ITestInterface3, ITestInterface4, ITestInterface5,
                TestContainerClass>(
                _ => new TestContainerClass());
        using var provider = services.BuildServiceProvider();

        var interface1 = provider.GetRequiredService<ITestInterface1>();
        var interface2 = provider.GetRequiredService<ITestInterface2>();
        var interface3 = provider.GetRequiredService<ITestInterface3>();
        var interface4 = provider.GetRequiredService<ITestInterface4>();
        var interface5 = provider.GetRequiredService<ITestInterface5>();

        interface1.Should().BeOfType<TestContainerClass>();
        interface1.Should().Be(interface2);
        interface2.Should().Be(interface3);
        interface3.Should().Be(interface4);
        interface4.Should().Be(interface5);
        interface5.Should().Be(interface1);
    }

    [Fact]
    public void WhenGetRequiredServiceForPlatformAndNotRegistered_ThenThrows()
    {
        var services = new ServiceCollection();
        var container = services.BuildServiceProvider();

        container
            .Invoking(x => x.GetRequiredServiceForPlatform<ITestInterface1>())
            .Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void WhenGetRequiredServiceForPlatformAndRegisteredViaFactory_ThenReturnsService()
    {
        var services = new ServiceCollection();
        var instance = new TestContainerClass();
        services.AddForPlatform<ITestInterface1>(_ => instance);
        var container = services.BuildServiceProvider();

        var result = container.GetRequiredServiceForPlatform<ITestInterface1>();

        result.Should().NotBeNull();
    }

    [Fact]
    public void WhenAddForPlatformWithFourInterfaces_ThenRegistersFourWithTheSameInstance()
    {
        var services = new ServiceCollection();

        services
            .AddForPlatform<ITestInterface1, ITestInterface2, ITestInterface3, ITestInterface4, TestContainerClass>(
                _ => new TestContainerClass());
        using var provider = services.BuildServiceProvider();

        var interface1 = provider.GetRequiredServiceForPlatform<ITestInterface1>();
        var interface2 = provider.GetRequiredServiceForPlatform<ITestInterface2>();
        var interface3 = provider.GetRequiredServiceForPlatform<ITestInterface3>();
        var interface4 = provider.GetRequiredServiceForPlatform<ITestInterface4>();

        interface1.Should().BeOfType<TestContainerClass>();
        interface1.Should().Be(interface2);
        interface2.Should().Be(interface3);
        interface3.Should().Be(interface4);
        interface4.Should().Be(interface1);
    }
}

public interface ITestInterface1;

public interface ITestInterface2;

public interface ITestInterface3;

public interface ITestInterface4;

public interface ITestInterface5;

public class TestContainerClass : ITestInterface1, ITestInterface2, ITestInterface3, ITestInterface4, ITestInterface5,
    IDisposable
{
    public void Dispose()
    {
    }
}