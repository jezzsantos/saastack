using Common.Configuration;
using Common.Recording;
using FluentAssertions;
using Infrastructure.Shared.ApplicationServices.External;
using Infrastructure.Web.Api.Interfaces;
using IntegrationTesting.WebApi.Common;
using Microsoft.Extensions.DependencyInjection;
using UnitTesting.Common;
using Xunit;

namespace Infrastructure.Shared.IntegrationTests.ApplicationServices.External;

[Trait("Category", "Integration.External")]
[Collection("External")]
public class GravatarHttpServiceClientSpec : ExternalApiSpec
{
    private readonly GravatarHttpServiceClient _serviceClient;

    public GravatarHttpServiceClientSpec(ExternalApiSetup setup) : base(setup, OverrideDependencies)
    {
        var settings = setup.GetRequiredService<IConfigurationSettings>();
        _serviceClient = new GravatarHttpServiceClient(NoOpRecorder.Instance, settings, new TestHttpClientFactory());
    }

    [Fact]
    public async Task WhenRequestRegisteredAvatar_ReturnsAvatar()
    {
        var result =
            await _serviceClient.FindAvatarAsync(new TestCaller(), "jitewaboh@lagify.com", CancellationToken.None);

        result.Should().BeSuccess();
        result.Value.Value.ContentType.Charset.Should().BeNull();
        result.Value.Value.ContentType.MediaType.Should().Be(HttpConstants.ContentTypes.ImagePng);
        result.Value.Value.Content.Should().NotBeNull();
        result.Value.Value.Filename.Should().BeNull();
        result.Value.Value.Size.Should().Be(156531);
    }

    [Fact]
    public async Task WhenRequestNoRegisteredAvatar_ReturnsNoImage()
    {
        var result =
            await _serviceClient.FindAvatarAsync(new TestCaller(), "noavatar@missing.com", CancellationToken.None);

        result.Should().BeSuccess();
        result.Value.HasValue.Should().BeFalse();
    }

    private static void OverrideDependencies(IServiceCollection services)
    {
        //Do nothing
    }
}