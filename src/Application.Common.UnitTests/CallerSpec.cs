using Common;
using Domain.Interfaces;
using Domain.Interfaces.Authorization;
using FluentAssertions;
using Moq;
using UnitTesting.Common;
using Xunit;

namespace Application.Common.UnitTests;

[Trait("Category", "Unit")]
public class CallerSpec
{
    [Fact]
    public void WhenCreateAsAnonymous_ThenReturnsANewCallForAnonymousCaller()
    {
        var result = Caller.CreateAsAnonymous();

        result.IsServiceAccount.Should().BeFalse();
        result.Authorization.Should().BeNone();
        result.IsAuthenticated.Should().BeFalse();
        result.Roles.All.Should().BeEmpty();
        result.Features.All.Should().OnlyContain(feat => feat == PlatformFeatures.Basic);
        result.TenantId.Should().BeNull();
        result.CallerId.Should().Be(CallerConstants.AnonymousUserId);
        result.CallId.Should().NotBeNull();
    }

    [Fact]
    public void WhenCreateAsAnonymousTenant_ThenReturnsANewCallForAnonymousTenantedCaller()
    {
        var result = Caller.CreateAsAnonymousTenant("atenantid");

        result.IsServiceAccount.Should().BeFalse();
        result.Authorization.Should().BeNone();
        result.IsAuthenticated.Should().BeFalse();
        result.Roles.All.Should().BeEmpty();
        result.Features.All.Should().OnlyContain(feat => feat == PlatformFeatures.Basic);
        result.TenantId.Should().Be("atenantid");
        result.CallerId.Should().Be(CallerConstants.AnonymousUserId);
        result.CallId.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void WhenCreateAsCallerFromCall_ThenReturnsACustomCaller()
    {
        var call = new Mock<ICallContext>();
        call.Setup(c => c.CallId).Returns("acallid");
        call.Setup(c => c.CallerId).Returns("acallerid");
        call.Setup(c => c.TenantId).Returns((string?)null);

        var result = Caller.CreateAsCallerFromCall(call.Object);

        result.IsServiceAccount.Should().BeFalse();
        result.Authorization.Should().BeNone();
        result.IsAuthenticated.Should().BeFalse();
        result.Roles.All.Should().BeEmpty();
        result.Features.All.Should().OnlyContain(feat => feat == PlatformFeatures.Basic);
        result.TenantId.Should().BeNull();
        result.CallerId.Should().Be("acallerid");
        result.CallId.Should().Be("acallid");
    }

    [Fact]
    public void WhenCreateAsExternalWebHook_ThenReturnsWebhookServiceAccountCaller()
    {
        var result = Caller.CreateAsExternalWebHook("acallid");

        result.IsServiceAccount.Should().BeTrue();
        result.Authorization.Should().BeNone();
        result.IsAuthenticated.Should().BeTrue();
        result.Roles.All.Should().OnlyContain(rol => rol == PlatformRoles.ServiceAccount);
        result.Features.All.Should().OnlyContain(feat => feat == PlatformFeatures.Basic);
        result.TenantId.Should().BeNull();
        result.CallerId.Should().Be(CallerConstants.ExternalWebhookAccountUserId);
        result.CallId.Should().Be("acallid");
    }

    [Fact]
    public void WhenCreateAsMaintenanceWithNoCall_ThenReturnsMaintenanceServiceAccountWithAllFeatures()
    {
        var result = Caller.CreateAsMaintenance();

        result.IsServiceAccount.Should().BeTrue();
        result.Authorization.Should().BeNone();
        result.IsAuthenticated.Should().BeTrue();
        result.Roles.All.Should().OnlyContain(rol => rol == PlatformRoles.ServiceAccount);
        result.Features.All.Should().OnlyContain(feat => feat == PlatformFeatures.Paid2);
        result.TenantId.Should().BeNull();
        result.CallerId.Should().Be(CallerConstants.MaintenanceAccountUserId);
        result.CallId.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void WhenCreateAsMaintenance_ThenReturnsMaintenanceServiceAccountWithAllFeatures()
    {
        var result = Caller.CreateAsMaintenance("acallid");

        result.IsServiceAccount.Should().BeTrue();
        result.Authorization.Should().BeNone();
        result.IsAuthenticated.Should().BeTrue();
        result.Roles.All.Should().OnlyContain(rol => rol == PlatformRoles.ServiceAccount);
        result.Features.All.Should().OnlyContain(feat => feat == PlatformFeatures.Paid2);
        result.TenantId.Should().BeNull();
        result.CallerId.Should().Be(CallerConstants.MaintenanceAccountUserId);
        result.CallId.Should().Be("acallid");
    }

    [Fact]
    public void WhenCreateAsMaintenanceTenant_ThenReturnsMaintenanceServiceAccountWithAllFeatures()
    {
        var result = Caller.CreateAsMaintenanceTenant("atenantid");

        result.IsServiceAccount.Should().BeTrue();
        result.Authorization.Should().BeNone();
        result.IsAuthenticated.Should().BeTrue();
        result.Roles.All.Should().OnlyContain(rol => rol == PlatformRoles.ServiceAccount);
        result.Features.All.Should().OnlyContain(feat => feat == PlatformFeatures.Paid2);
        result.TenantId.Should().Be("atenantid");
        result.CallerId.Should().Be(CallerConstants.MaintenanceAccountUserId);
        result.CallId.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void WhenCreateAsServiceClient_ThenReturnsServiceClientCaller()
    {
        var result = Caller.CreateAsServiceClient();

        result.IsServiceAccount.Should().BeTrue();
        result.Authorization.Should().BeNone();
        result.IsAuthenticated.Should().BeTrue();
        result.Roles.All.Should().OnlyContain(rol => rol == PlatformRoles.ServiceAccount);
        result.Features.All.Should().OnlyContain(feat => feat == PlatformFeatures.Paid2);
        result.TenantId.Should().BeNull();
        result.CallerId.Should().Be(CallerConstants.ServiceClientAccountUserId);
        result.CallId.Should().NotBeNullOrEmpty();
    }
}