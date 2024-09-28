using Application.Interfaces.Services;
using Common.Configuration;
using Common.Extensions;
using FluentAssertions;
using Infrastructure.Interfaces;
using JetBrains.Annotations;
using Microsoft.Extensions.Configuration;
using Moq;
using Xunit;

namespace Infrastructure.Hosting.Common.UnitTests.ApplicationServices;

[UsedImplicitly]
public class AspNetDynamicConfigurationSettingsSpec
{
    [Trait("Category", "Unit")]
    public class GivenOnlyPlatformConfiguration
    {
        [Fact]
        public void WhenGetStringAndNotExists_ThenThrows()
        {
            var settings = SetupPlatformConfiguration(new Dictionary<string, string>());

            settings
                .Invoking(x => x.GetString("akey"))
                .Should().Throw<InvalidOperationException>()
                .WithMessage(Resources.AspNetDynamicConfigurationSettings_PlatformSettings_KeyNotFound.Format("akey"));
        }

        [Fact]
        public void WhenGetStringAndExists_ThenReturns()
        {
            var settings = SetupPlatformConfiguration(new Dictionary<string, string>
            {
                { "akey", "avalue" }
            });

            var result = settings.GetString("akey");

            result.Should().Be("avalue");
        }

        [Fact]
        public void WhenGetStringAndEmpty_ThenReturnsEmpty()
        {
            var settings = SetupPlatformConfiguration(new Dictionary<string, string>
            {
                { "akey", string.Empty }
            });

            var result = settings.GetString("akey");

            result.Should().Be(string.Empty);
        }

        [Fact]
        public void WhenGetNestedStringAndExists_ThenReturns()
        {
            var settings = SetupPlatformConfiguration(new Dictionary<string, string>
            {
                { "anamespace.akey", "avalue" }
            });

            var result = settings.GetString("anamespace.akey");

            result.Should().Be("avalue");
        }

        [Fact]
        public void WhenGetBoolAndNotExists_ThenThrows()
        {
            var settings = SetupPlatformConfiguration(new Dictionary<string, string>());

            settings
                .Invoking(x => x.GetBool("akey"))
                .Should().Throw<InvalidOperationException>()
                .WithMessage(Resources.AspNetDynamicConfigurationSettings_PlatformSettings_KeyNotFound.Format("akey"));
        }

        [Fact]
        public void WhenGetBoolAndExists_ThenReturns()
        {
            var settings = SetupPlatformConfiguration(new Dictionary<string, string>
            {
                { "akey", "true" }
            });

            var result = settings.GetBool("akey");

            result.Should().BeTrue();
        }

        [Fact]
        public void WhenGetBoolAndNotBoolean_ThenThrows()
        {
            var settings = SetupPlatformConfiguration(new Dictionary<string, string>
            {
                { "akey", "notaboolvalue" }
            });

            settings
                .Invoking(x => x.GetBool("akey"))
                .Should().Throw<InvalidOperationException>()
                .WithMessage(Resources.AspNetDynamicConfigurationSettings_ValueNotBoolean.Format("akey"));
        }

        [Fact]
        public void WhenGetNumberAndNotExists_ThenThrows()
        {
            var settings = SetupPlatformConfiguration(new Dictionary<string, string>());

            settings
                .Invoking(x => x.GetNumber("akey"))
                .Should().Throw<InvalidOperationException>()
                .WithMessage(Resources.AspNetDynamicConfigurationSettings_PlatformSettings_KeyNotFound.Format("akey"));
        }

        [Fact]
        public void WhenGetNumberAndExists_ThenReturns()
        {
            var settings = SetupPlatformConfiguration(new Dictionary<string, string>
            {
                { "akey", "999" }
            });

            var result = settings.GetNumber("akey");

            result.Should().Be(999);
        }

        [Fact]
        public void WhenGetNumberAndNotANumber_ThenThrows()
        {
            var settings = SetupPlatformConfiguration(new Dictionary<string, string>
            {
                { "akey", "notanumbervalue" }
            });

            settings
                .Invoking(x => x.GetNumber("akey"))
                .Should().Throw<InvalidOperationException>()
                .WithMessage(Resources.AspNetDynamicConfigurationSettings_ValueNotNumber.Format("akey"));
        }

        private static IConfigurationSettings SetupPlatformConfiguration(Dictionary<string, string> values)
        {
            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(values!)
                .Build();

            return new AspNetDynamicConfigurationSettings(configuration);
        }
    }

    [Trait("Category", "Unit")]
    public class GivenTenancyAndNoPlatformConfiguration
    {
        private readonly Mock<ITenancyContext> _tenantContext;

        public GivenTenancyAndNoPlatformConfiguration()
        {
            _tenantContext = new Mock<ITenancyContext>();
        }

        [Fact]
        public void WhenGetStringAndNotExists_ThenThrows()
        {
            var settings = SetupTenancyConfiguration(_tenantContext, new TenantSettings());

            settings
                .Invoking(x => x.GetString("akey"))
                .Should().Throw<InvalidOperationException>()
                .WithMessage(Resources.AspNetDynamicConfigurationSettings_EitherSettings_KeyNotFound.Format("akey"));
        }

        [Fact]
        public void WhenGetStringAndExists_ThenReturns()
        {
            var settings = SetupTenancyConfiguration(_tenantContext, new TenantSettings(new Dictionary<string, object>
            {
                { "akey", "avalue" }
            }));

            var result = settings.GetString("akey");

            result.Should().Be("avalue");
        }

        [Fact]
        public void WhenGetStringAndEmpty_ThenReturnsEmpty()
        {
            var settings = SetupTenancyConfiguration(_tenantContext, new TenantSettings(new Dictionary<string, object>
            {
                { "akey", string.Empty }
            }));

            var result = settings.GetString("akey");

            result.Should().Be(string.Empty);
        }

        [Fact]
        public void WhenGetNestedStringAndExists_ThenReturns()
        {
            var settings = SetupTenancyConfiguration(_tenantContext, new TenantSettings(new Dictionary<string, object>
            {
                { "anamespace.akey", "avalue" }
            }));

            var result = settings.GetString("anamespace.akey");

            result.Should().Be("avalue");
        }

        [Fact]
        public void WhenGetBoolAndNotExists_ThenThrows()
        {
            var settings = SetupTenancyConfiguration(_tenantContext, new TenantSettings());

            settings
                .Invoking(x => x.GetBool("akey"))
                .Should().Throw<InvalidOperationException>()
                .WithMessage(Resources.AspNetDynamicConfigurationSettings_EitherSettings_KeyNotFound.Format("akey"));
        }

        [Fact]
        public void WhenGetBoolAndExists_ThenReturns()
        {
            var settings = SetupTenancyConfiguration(_tenantContext, new TenantSettings(new Dictionary<string, object>
            {
                { "akey", true }
            }));

            var result = settings.GetBool("akey");

            result.Should().BeTrue();
        }

        [Fact]
        public void WhenGetBoolAndNotBoolean_ThenThrows()
        {
            var settings = SetupTenancyConfiguration(_tenantContext, new TenantSettings(new Dictionary<string, object>
            {
                { "akey", "notaboolvalue" }
            }));

            settings
                .Invoking(x => x.GetBool("akey"))
                .Should().Throw<InvalidOperationException>()
                .WithMessage(Resources.AspNetDynamicConfigurationSettings_ValueNotBoolean.Format("akey"));
        }

        [Fact]
        public void WhenGetNumberAndNotExists_ThenThrows()
        {
            var settings = SetupTenancyConfiguration(_tenantContext, new TenantSettings());

            settings
                .Invoking(x => x.GetNumber("akey"))
                .Should().Throw<InvalidOperationException>()
                .WithMessage(Resources.AspNetDynamicConfigurationSettings_EitherSettings_KeyNotFound.Format("akey"));
        }

        [Fact]
        public void WhenGetNumberAndExists_ThenReturns()
        {
            var settings = SetupTenancyConfiguration(_tenantContext, new TenantSettings(new Dictionary<string, object>
            {
                { "akey", 999 }
            }));

            var result = settings.GetNumber("akey");

            result.Should().Be(999);
        }

        [Fact]
        public void WhenGetNumberAndNotANumber_ThenThrows()
        {
            var settings = SetupTenancyConfiguration(_tenantContext, new TenantSettings(new Dictionary<string, object>
            {
                { "akey", "notanumbervalue" }
            }));

            settings
                .Invoking(x => x.GetNumber("akey"))
                .Should().Throw<InvalidOperationException>()
                .WithMessage(Resources.AspNetDynamicConfigurationSettings_ValueNotNumber.Format("akey"));
        }

        private static IConfigurationSettings SetupTenancyConfiguration(Mock<ITenancyContext> context,
            TenantSettings values)
        {
            var configuration = new ConfigurationBuilder()
                .Build();

            context.Setup(ctx => ctx.Settings).Returns(values);

            return new AspNetDynamicConfigurationSettings(configuration, context.Object);
        }
    }

    [Trait("Category", "Unit")]
    public class GivenTenancyAndPlatformConfiguration
    {
        private readonly Mock<ITenancyContext> _tenantContext;

        public GivenTenancyAndPlatformConfiguration()
        {
            _tenantContext = new Mock<ITenancyContext>();
        }

        [Fact]
        public void WhenGetStringWithNoTenantedValueButPlatformValue_ThenReturnsPlatformValue()
        {
            var settings = SetupPlatformAndTenancyConfiguration(_tenantContext, new Dictionary<string, object>
            {
                { "akey", "aplatformvalue" }
            }, new TenantSettings());

            var result = settings.GetString("akey");

            result.Should().Be("aplatformvalue");
        }

        [Fact]
        public void WhenGetStringWithTenantedValueButNoPlatformValue_ThenReturnsTenantedValue()
        {
            var settings = SetupPlatformAndTenancyConfiguration(_tenantContext, new Dictionary<string, object>(),
                new TenantSettings(new Dictionary<string, object>
                {
                    { "akey", "atenantvalue" }
                }));

            var result = settings.GetString("akey");

            result.Should().Be("atenantvalue");
        }

        [Fact]
        public void WhenGetStringWithTenantedValueAndPlatformValue_ThenReturnsTenantedValue()
        {
            var settings = SetupPlatformAndTenancyConfiguration(_tenantContext, new Dictionary<string, object>
            {
                { "akey", "aplatformvalue" }
            }, new TenantSettings(new Dictionary<string, object>
            {
                { "akey", "atenantvalue" }
            }));

            var result = settings.GetString("akey");

            result.Should().Be("atenantvalue");
        }

        [Fact]
        public void WhenGetStringWithEmptyTenantedValue_ThenReturnsEmpty()
        {
            var settings = SetupPlatformAndTenancyConfiguration(_tenantContext, new Dictionary<string, object>
            {
                { "akey", "aplatformvalue" }
            }, new TenantSettings(new Dictionary<string, object>
            {
                { "akey", string.Empty }
            }));

            var result = settings.GetString("akey");

            result.Should().Be(string.Empty);
        }

        [Fact]
        public void WhenGetStringWithNoTenantedValueButEmptyPlatformValue_ThenReturnsEmpty()
        {
            var settings = SetupPlatformAndTenancyConfiguration(_tenantContext, new Dictionary<string, object>
            {
                { "akey", string.Empty }
            }, new TenantSettings(new Dictionary<string, object>()));

            var result = settings.GetString("akey");

            result.Should().Be(string.Empty);
        }

        [Fact]
        public void WhenGetStringWithNeitherValueButHasDefault_ThenReturnsDefault()
        {
            var settings = SetupPlatformAndTenancyConfiguration(_tenantContext, new Dictionary<string, object>(),
                new TenantSettings(new Dictionary<string, object>()));

            var result = settings.GetString("akey", "adefaultvalue");

            result.Should().Be("adefaultvalue");
        }

        [Fact]
        public void WhenGetStringWithNoTenantedValueButPlatformValueAndHasDefault_ThenReturnsPlatformValue()
        {
            var settings = SetupPlatformAndTenancyConfiguration(_tenantContext, new Dictionary<string, object>
                {
                    { "akey", "aplatformvalue" }
                },
                new TenantSettings(new Dictionary<string, object>()));

            var result = settings.GetString("akey", "adefaultvalue");

            result.Should().Be("aplatformvalue");
        }

        [Fact]
        public void WhenGetStringWithTenantedValueButNoPlatformValueAndHasDefault_ThenReturnsTenantedValue()
        {
            var settings = SetupPlatformAndTenancyConfiguration(_tenantContext, new Dictionary<string, object>(),
                new TenantSettings(new Dictionary<string, object>
                {
                    { "akey", "atenantvalue" }
                }));

            var result = settings.GetString("akey", "adefaultvalue");

            result.Should().Be("atenantvalue");
        }

        [Fact]
        public void WhenGetNestedStringAndExists_ThenReturns()
        {
            var settings = SetupPlatformAndTenancyConfiguration(_tenantContext, new Dictionary<string, object>
            {
                { "anamespace.akey", "aplatformvalue" }
            }, new TenantSettings(new Dictionary<string, object>
            {
                { "anamespace.akey", "atenantvalue" }
            }));

            var result = settings.GetString("anamespace.akey");

            result.Should().Be("atenantvalue");
        }

        [Fact]
        public void WhenGetBoolAndNotExists_ThenReturnsPlatformValue()
        {
            var settings = SetupPlatformAndTenancyConfiguration(_tenantContext, new Dictionary<string, object>
            {
                { "akey", true }
            }, new TenantSettings());

            var result = settings.GetBool("akey");

            result.Should().BeTrue();
        }

        [Fact]
        public void WhenGetBoolAndExistsInTenant_ThenReturnsTenantSetting()
        {
            var settings = SetupPlatformAndTenancyConfiguration(_tenantContext, new Dictionary<string, object>
            {
                { "akey", false }
            }, new TenantSettings(new Dictionary<string, object>
            {
                { "akey", true }
            }));

            var result = settings.GetBool("akey");

            result.Should().BeTrue();
        }

        [Fact]
        public void WhenGetBoolAndNotBoolean_ThenThrows()
        {
            var settings = SetupPlatformAndTenancyConfiguration(_tenantContext, new Dictionary<string, object>
            {
                { "akey", true }
            }, new TenantSettings(new Dictionary<string, object>
            {
                { "akey", "notaboolvalue" }
            }));

            settings
                .Invoking(x => x.GetBool("akey"))
                .Should().Throw<InvalidOperationException>()
                .WithMessage(Resources.AspNetDynamicConfigurationSettings_ValueNotBoolean.Format("akey"));
        }

        [Fact]
        public void WhenGetBoolWithNoTenantedValueButPlatformValue_ThenReturnsPlatformValue()
        {
            var settings = SetupPlatformAndTenancyConfiguration(_tenantContext, new Dictionary<string, object>
            {
                { "akey", true }
            }, new TenantSettings());

            var result = settings.GetBool("akey");

            result.Should().Be(true);
        }

        [Fact]
        public void WhenGetBoolWithTenantedValueButNoPlatformValue_ThenReturnsTenantedValue()
        {
            var settings = SetupPlatformAndTenancyConfiguration(_tenantContext, new Dictionary<string, object>(),
                new TenantSettings(new Dictionary<string, object>
                {
                    { "akey", true }
                }));

            var result = settings.GetBool("akey");

            result.Should().Be(true);
        }

        [Fact]
        public void WhenGetBoolWithTenantedValueAndPlatformValue_ThenReturnsTenantedValue()
        {
            var settings = SetupPlatformAndTenancyConfiguration(_tenantContext, new Dictionary<string, object>
            {
                { "akey", false }
            }, new TenantSettings(new Dictionary<string, object>
            {
                { "akey", true }
            }));

            var result = settings.GetBool("akey");

            result.Should().Be(true);
        }

        [Fact]
        public void WhenGetBoolWithNeitherValueButHasDefault_ThenReturnsDefault()
        {
            var settings = SetupPlatformAndTenancyConfiguration(_tenantContext, new Dictionary<string, object>(),
                new TenantSettings(new Dictionary<string, object>()));

            var result = settings.GetBool("akey", true);

            result.Should().Be(true);
        }

        [Fact]
        public void WhenGetBoolWithNoTenantedValueButPlatformValueAndHasDefault_ThenReturnsPlatformValue()
        {
            var settings = SetupPlatformAndTenancyConfiguration(_tenantContext, new Dictionary<string, object>
                {
                    { "akey", true }
                },
                new TenantSettings(new Dictionary<string, object>()));

            var result = settings.GetBool("akey", true);

            result.Should().Be(true);
        }

        [Fact]
        public void WhenGetBoolWithTenantedValueButNoPlatformValueAndHasDefault_ThenReturnsTenantedValue()
        {
            var settings = SetupPlatformAndTenancyConfiguration(_tenantContext, new Dictionary<string, object>(),
                new TenantSettings(new Dictionary<string, object>
                {
                    { "akey", true }
                }));

            var result = settings.GetBool("akey", true);

            result.Should().Be(true);
        }

        [Fact]
        public void WhenGetNumberAndNotANumber_ThenThrows()
        {
            var settings = SetupPlatformAndTenancyConfiguration(_tenantContext, new Dictionary<string, object>
            {
                { "akey", 99 }
            }, new TenantSettings(new Dictionary<string, object>
            {
                { "akey", "notanumbervalue" }
            }));

            settings
                .Invoking(x => x.GetNumber("akey"))
                .Should().Throw<InvalidOperationException>()
                .WithMessage(Resources.AspNetDynamicConfigurationSettings_ValueNotNumber.Format("akey"));
        }

        [Fact]
        public void WhenGetNumberWithNoTenantedValueButPlatformValue_ThenReturnsPlatformValue()
        {
            var settings = SetupPlatformAndTenancyConfiguration(_tenantContext, new Dictionary<string, object>
            {
                { "akey", 6 }
            }, new TenantSettings());

            var result = settings.GetNumber("akey");

            result.Should().Be(6);
        }

        [Fact]
        public void WhenGetNumberWithTenantedValueButNoPlatformValue_ThenReturnsTenantedValue()
        {
            var settings = SetupPlatformAndTenancyConfiguration(_tenantContext, new Dictionary<string, object>(),
                new TenantSettings(new Dictionary<string, object>
                {
                    { "akey", 9 }
                }));

            var result = settings.GetNumber("akey");

            result.Should().Be(9);
        }

        [Fact]
        public void WhenGetNumberWithTenantedValueAndPlatformValue_ThenReturnsTenantedValue()
        {
            var settings = SetupPlatformAndTenancyConfiguration(_tenantContext, new Dictionary<string, object>
            {
                { "akey", 6 }
            }, new TenantSettings(new Dictionary<string, object>
            {
                { "akey", 9 }
            }));

            var result = settings.GetNumber("akey");

            result.Should().Be(9);
        }

        [Fact]
        public void WhenGetNumberWithNeitherValueButHasDefault_ThenReturnsDefault()
        {
            var settings = SetupPlatformAndTenancyConfiguration(_tenantContext, new Dictionary<string, object>(),
                new TenantSettings(new Dictionary<string, object>()));

            var result = settings.GetNumber("akey", 4);

            result.Should().Be(4);
        }

        [Fact]
        public void WhenGetNumberWithNoTenantedValueButPlatformValueAndHasDefault_ThenReturnsPlatformValue()
        {
            var settings = SetupPlatformAndTenancyConfiguration(_tenantContext, new Dictionary<string, object>
                {
                    { "akey", 6 }
                },
                new TenantSettings(new Dictionary<string, object>()));

            var result = settings.GetNumber("akey", 4);

            result.Should().Be(6);
        }

        [Fact]
        public void WhenGetNumberWithTenantedValueButNoPlatformValueAndHasDefault_ThenReturnsTenantedValue()
        {
            var settings = SetupPlatformAndTenancyConfiguration(_tenantContext, new Dictionary<string, object>(),
                new TenantSettings(new Dictionary<string, object>
                {
                    { "akey", 9 }
                }));

            var result = settings.GetNumber("akey", 4);

            result.Should().Be(9);
        }

        private static IConfigurationSettings SetupPlatformAndTenancyConfiguration(Mock<ITenancyContext> context,
            Dictionary<string, object> platformValues, TenantSettings tenantSettings)
        {
            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(platformValues.ToDictionary(pair => pair.Key, pair => pair.Value.ToString()))
                .Build();

            context.Setup(ctx => ctx.Settings).Returns(tenantSettings);

            return new AspNetDynamicConfigurationSettings(configuration, context.Object);
        }
    }
}