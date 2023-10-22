using Application.Common;
using Application.Interfaces.Resources;
using FluentAssertions;
using Infrastructure.Web.Hosting.Common.ApplicationServices;
using JetBrains.Annotations;
using Xunit;

namespace Infrastructure.Web.Hosting.Common.UnitTests.ApplicationServices;

[CollectionDefinition("TenantSettings", DisableParallelization = true)]
public class AllTenantSettingsSpecs : ICollectionFixture<TenantSettingsSpecSetup>
{
}

[UsedImplicitly]
public class TenantSettingsSpecSetup
{
}

[UsedImplicitly]
public class AspNetHostLocalFileTenantSettingsServiceSpec
{
    [Trait("Category", "Unit")]
    [Collection("TenantSettings")]
    public class GivenNoEncryptedSettings
    {
        private readonly AspNetHostLocalFileTenantSettingsService _service;

        public GivenNoEncryptedSettings()
        {
            _service =
                new AspNetHostLocalFileTenantSettingsService(
                    "tenantsettings.testing.json");
            _service.ResetCache();
        }

        [Fact]
        public void WhenCreateForNewTenant_ThenReturnsSettings()
        {
            var result =
                _service.CreateForNewTenant(Caller.CreateAsAnonymousTenant("atenantid"), "atenantid");

            result.Count.Should().Be(1);
            result["AGroup:ASubGroup:ASetting"].Should()
                .BeEquivalentTo(new TenantSetting { Value = "avalue", IsEncrypted = false });
        }
    }

    [Trait("Category", "Unit")]
    [Collection("TenantSettings")]
    public class GivenEncryptedSettings
    {
        private readonly AspNetHostLocalFileTenantSettingsService _service;

        public GivenEncryptedSettings()
        {
            _service =
                new AspNetHostLocalFileTenantSettingsService(
                    "tenantsettings.testing.encrypted.json");
            _service.ResetCache();
        }

        [Fact]
        public void WhenCreateForNewTenant_ThenReturnsSettings()
        {
            var result =
                _service.CreateForNewTenant(Caller.CreateAsAnonymousTenant("atenantid"), "atenantid");

            result.Count.Should().Be(3);
            result["AGroup:ASubGroup:ASetting1"].Should()
                .BeEquivalentTo(new TenantSetting { Value = "avalue1", IsEncrypted = true });
            result["AGroup:ASubGroup:ASetting2"].Should()
                .BeEquivalentTo(new TenantSetting { Value = "avalue2", IsEncrypted = false });
            result["AGroup:ASubGroup:ASetting3"].Should()
                .BeEquivalentTo(new TenantSetting { Value = "avalue3", IsEncrypted = true });
        }
    }
}