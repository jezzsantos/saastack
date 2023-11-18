using FluentAssertions;
using Infrastructure.Web.Hosting.Common.Extensions;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Infrastructure.Web.Hosting.Common.UnitTests.Extensions;

[Trait("Category", "Unit")]
public class ServiceCollectionExtensionsSpec
{
    [Fact]
    public void WhenAddSingletonWithTwoInterfaces_ThenRegistersFourWithTheSameInstance()
    {
        var services = new ServiceCollection();

        services.AddSingleton<ITestInterface1, ITestInterface2, TestContainerClass>(
            _ => new TestContainerClass());
        using var provider = services.BuildServiceProvider();

        var interface1 = provider.GetService<ITestInterface1>();
        var interface2 = provider.GetService<ITestInterface2>();

        interface1.Should().BeSameAs(interface2);
    }

    [Fact]
    public void WhenAddSingletonWithThreeInterfaces_ThenRegistersFourWithTheSameInstance()
    {
        var services = new ServiceCollection();

        services.AddSingleton<ITestInterface1, ITestInterface2, ITestInterface3, TestContainerClass>(
            _ => new TestContainerClass());
        using var provider = services.BuildServiceProvider();

        var interface1 = provider.GetService<ITestInterface1>();
        var interface2 = provider.GetService<ITestInterface2>();
        var interface3 = provider.GetService<ITestInterface3>();

        interface1.Should().BeSameAs(interface2);
        interface2.Should().BeSameAs(interface3);
        interface3.Should().BeSameAs(interface1);
    }

    [Fact]
    public void WhenAddSingletonWithFourInterfaces_ThenRegistersFourWithTheSameInstance()
    {
        var services = new ServiceCollection();

        services.AddSingleton<ITestInterface1, ITestInterface2, ITestInterface3, ITestInterface4, TestContainerClass>(
            _ => new TestContainerClass());
        using var provider = services.BuildServiceProvider();

        var interface1 = provider.GetService<ITestInterface1>();
        var interface2 = provider.GetService<ITestInterface2>();
        var interface3 = provider.GetService<ITestInterface3>();
        var interface4 = provider.GetService<ITestInterface4>();

        interface1.Should().BeSameAs(interface2);
        interface2.Should().BeSameAs(interface3);
        interface3.Should().BeSameAs(interface4);
        interface4.Should().BeSameAs(interface1);
    }

    [Fact]
    public void WhenAddSingletonWithFiveInterfaces_ThenRegistersFourWithTheSameInstance()
    {
        var services = new ServiceCollection();

        services
            .AddSingleton<ITestInterface1, ITestInterface2, ITestInterface3, ITestInterface4, ITestInterface5,
                TestContainerClass>(
                _ => new TestContainerClass());
        using var provider = services.BuildServiceProvider();

        var interface1 = provider.GetService<ITestInterface1>();
        var interface2 = provider.GetService<ITestInterface2>();
        var interface3 = provider.GetService<ITestInterface3>();
        var interface4 = provider.GetService<ITestInterface4>();
        var interface5 = provider.GetService<ITestInterface5>();

        interface1.Should().BeSameAs(interface2);
        interface2.Should().BeSameAs(interface3);
        interface3.Should().BeSameAs(interface4);
        interface4.Should().BeSameAs(interface5);
        interface5.Should().BeSameAs(interface1);
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