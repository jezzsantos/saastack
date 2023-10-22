using Common.Configuration;
using Common.Extensions;
using FluentAssertions;
using Infrastructure.Interfaces;
using Infrastructure.Web.Hosting.Common.ApplicationServices;
using JetBrains.Annotations;
using Microsoft.Extensions.Configuration;
using Moq;
using Xunit;

namespace Infrastructure.Web.Hosting.Common.UnitTests.ApplicationServices;

[UsedImplicitly]
public class AspNetConfigurationSettingsSpec
{
    private static IConfigurationSettings SetupPlatformConfiguration(Dictionary<string, string> values)
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(values!)
            .Build();

        return new AspNetConfigurationSettings(configuration);
    }

    private static IConfigurationSettings SetupTenancyConfiguration(Mock<ITenancyContext> context,
        Dictionary<string, object?> values)
    {
        var configuration = new ConfigurationBuilder()
            .Build();

        context.Setup(ctx => ctx.Settings).Returns(values!);

        return new AspNetConfigurationSettings(configuration, context.Object);
    }

    [Trait("Category", "Unit")]
    public class GivenOnlyPlatformConfiguration
    {
        [Fact]
        public void WhenGetStringFromPlatformAndNotExists_ThenThrows()
        {
            var settings = SetupPlatformConfiguration(new Dictionary<string, string>());

            settings.Platform
                .Invoking(x => x.GetString("akey"))
                .Should().Throw<InvalidOperationException>()
                .WithMessage(Resources.AspNetConfigurationSettings_KeyNotFound.Format("akey"));
        }

        [Fact]
        public void WhenGetStringFromPlatformAndExists_ThenReturns()
        {
            var settings = SetupPlatformConfiguration(new Dictionary<string, string>
            {
                { "akey", "avalue" }
            });

            var result = settings.Platform.GetString("akey");

            result.Should().Be("avalue");
        }

        [Fact]
        public void WhenGetStringFromPlatformAndEmpty_ThenReturnsEmpty()
        {
            var settings = SetupPlatformConfiguration(new Dictionary<string, string>
            {
                { "akey", string.Empty }
            });

            var result = settings.Platform.GetString("akey");

            result.Should().Be(string.Empty);
        }

        [Fact]
        public void WhenGetNestedStringFromPlatformAndExists_ThenReturns()
        {
            var settings = SetupPlatformConfiguration(new Dictionary<string, string>
            {
                { "anamespace.akey", "avalue" }
            });

            var result = settings.Platform.GetString("anamespace.akey");

            result.Should().Be("avalue");
        }

        [Fact]
        public void WhenGetBoolFromPlatformAndNotExists_ThenThrows()
        {
            var settings = SetupPlatformConfiguration(new Dictionary<string, string>());

            settings.Platform
                .Invoking(x => x.GetBool("akey"))
                .Should().Throw<InvalidOperationException>()
                .WithMessage(Resources.AspNetConfigurationSettings_KeyNotFound.Format("akey"));
        }

        [Fact]
        public void WhenGetBoolFromPlatformAndExists_ThenReturns()
        {
            var settings = SetupPlatformConfiguration(new Dictionary<string, string>
            {
                { "akey", "true" }
            });

            var result = settings.Platform.GetBool("akey");

            result.Should().BeTrue();
        }

        [Fact]
        public void WhenGetBoolFromPlatformAndNotBoolean_ThenThrows()
        {
            var settings = SetupPlatformConfiguration(new Dictionary<string, string>
            {
                { "akey", "notaboolvalue" }
            });

            settings.Platform
                .Invoking(x => x.GetBool("akey"))
                .Should().Throw<InvalidOperationException>()
                .WithMessage(Resources.AspNetConfigurationSettings_ValueNotBoolean.Format("akey"));
        }

        [Fact]
        public void WhenGetNumberFromPlatformAndNotExists_ThenThrows()
        {
            var settings = SetupPlatformConfiguration(new Dictionary<string, string>());

            settings.Platform
                .Invoking(x => x.GetNumber("akey"))
                .Should().Throw<InvalidOperationException>()
                .WithMessage(Resources.AspNetConfigurationSettings_KeyNotFound.Format("akey"));
        }

        [Fact]
        public void WhenGetNumberFromPlatformAndExists_ThenReturns()
        {
            var settings = SetupPlatformConfiguration(new Dictionary<string, string>
            {
                { "akey", "999" }
            });

            var result = settings.Platform.GetNumber("akey");

            result.Should().Be(999);
        }

        [Fact]
        public void WhenGetNumberFromPlatformAndNotANumber_ThenThrows()
        {
            var settings = SetupPlatformConfiguration(new Dictionary<string, string>
            {
                { "akey", "notanumbervalue" }
            });

            settings.Platform
                .Invoking(x => x.GetNumber("akey"))
                .Should().Throw<InvalidOperationException>()
                .WithMessage(Resources.AspNetConfigurationSettings_ValueNotNumber.Format("akey"));
        }
    }

    [Trait("Category", "Unit")]
    public class GivenTenancyConfiguration
    {
        private readonly Mock<ITenancyContext> _tenantContext;

        public GivenTenancyConfiguration()
        {
            _tenantContext = new Mock<ITenancyContext>();
        }

        [Fact]
        public void WhenGetStringFromTenancyAndNotExists_ThenThrows()
        {
            var settings = SetupTenancyConfiguration(_tenantContext, new Dictionary<string, object?>());

            settings.Tenancy
                .Invoking(x => x.GetString("akey"))
                .Should().Throw<InvalidOperationException>()
                .WithMessage(Resources.AspNetConfigurationSettings_KeyNotFound.Format("akey"));
        }

        [Fact]
        public void WhenGetStringFromTenancyAndExists_ThenReturns()
        {
            var settings = SetupTenancyConfiguration(_tenantContext, new Dictionary<string, object?>
            {
                { "akey", "avalue" }
            });

            var result = settings.Tenancy.GetString("akey");

            result.Should().Be("avalue");
        }

        [Fact]
        public void WhenGetStringFromTenancyAndEmpty_ThenReturnsEmpty()
        {
            var settings = SetupTenancyConfiguration(_tenantContext, new Dictionary<string, object?>
            {
                { "akey", string.Empty }
            });

            var result = settings.Tenancy.GetString("akey");

            result.Should().Be(string.Empty);
        }

        [Fact]
        public void WhenGetNestedStringFromPlatformAndExists_ThenReturns()
        {
            var settings = SetupTenancyConfiguration(_tenantContext, new Dictionary<string, object?>
            {
                { "anamespace.akey", "avalue" }
            });

            var result = settings.Tenancy.GetString("anamespace.akey");

            result.Should().Be("avalue");
        }

        [Fact]
        public void WhenGetBoolFromTenancyAndNotExists_ThenThrows()
        {
            var settings = SetupTenancyConfiguration(_tenantContext, new Dictionary<string, object?>());

            settings.Tenancy
                .Invoking(x => x.GetBool("akey"))
                .Should().Throw<InvalidOperationException>()
                .WithMessage(Resources.AspNetConfigurationSettings_KeyNotFound.Format("akey"));
        }

        [Fact]
        public void WhenGetBoolFromTenancyAndExists_ThenReturns()
        {
            var settings = SetupTenancyConfiguration(_tenantContext, new Dictionary<string, object?>
            {
                { "akey", true }
            });

            var result = settings.Tenancy.GetBool("akey");

            result.Should().BeTrue();
        }

        [Fact]
        public void WhenGetBoolFromTenancyAndNotBoolean_ThenThrows()
        {
            var settings = SetupTenancyConfiguration(_tenantContext, new Dictionary<string, object?>
            {
                { "akey", "notaboolvalue" }
            });

            settings.Tenancy
                .Invoking(x => x.GetBool("akey"))
                .Should().Throw<InvalidOperationException>()
                .WithMessage(Resources.AspNetConfigurationSettings_ValueNotBoolean.Format("akey"));
        }

        [Fact]
        public void WhenGetNumberFromTenancyAndNotExists_ThenThrows()
        {
            var settings = SetupTenancyConfiguration(_tenantContext, new Dictionary<string, object?>());

            settings.Tenancy
                .Invoking(x => x.GetNumber("akey"))
                .Should().Throw<InvalidOperationException>()
                .WithMessage(Resources.AspNetConfigurationSettings_KeyNotFound.Format("akey"));
        }

        [Fact]
        public void WhenGetNumberFromTenancyAndExists_ThenReturns()
        {
            var settings = SetupTenancyConfiguration(_tenantContext, new Dictionary<string, object?>
            {
                { "akey", 999 }
            });

            var result = settings.Tenancy.GetNumber("akey");

            result.Should().Be(999);
        }

        [Fact]
        public void WhenGetNumberFromTenancyAndNotANumber_ThenThrows()
        {
            var settings = SetupTenancyConfiguration(_tenantContext, new Dictionary<string, object?>
            {
                { "akey", "notanumbervalue" }
            });

            settings.Tenancy
                .Invoking(x => x.GetNumber("akey"))
                .Should().Throw<InvalidOperationException>()
                .WithMessage(Resources.AspNetConfigurationSettings_ValueNotNumber.Format("akey"));
        }
    }
}