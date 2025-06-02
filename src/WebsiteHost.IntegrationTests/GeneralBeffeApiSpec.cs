#if TESTINGONLY
using System.Net;
using System.Net.Http.Json;
using Domain.Interfaces;
using FluentAssertions;
using Infrastructure.Web.Api.Common.Extensions;
using Infrastructure.Web.Api.Operations.Shared.BackEndForFrontEnd.TestingOnly;
using IntegrationTesting.WebApi.Common;
using IntegrationTesting.Website.Common;
using JetBrains.Annotations;
using Xunit;

namespace WebsiteHost.IntegrationTests;

[UsedImplicitly]
public class GeneralBeffeApiSpec
{
    [Trait("Category", "Integration.Website")]
    [Collection("WEBSITE")]
    public class GivenCSRFIgnoredRoutes : WebsiteSpec<Program, ApiHost1.Program>
    {
        public GivenCSRFIgnoredRoutes(WebApiSetup<Program> setup) : base(setup)
        {
        }

        [Fact]
        public async Task WhenAnonymousRequestAndNoAuthNCookie_ThenReturns()
        {
            var request = new BeffeAnonymousDirectTestingOnlyRequest();
            var result = await HttpApi.PostEmptyJsonAsync(request.MakeApiRoute());

            result.StatusCode.Should().Be(HttpStatusCode.OK);
            var response = await result.Content.ReadFromJsonAsync<BeffeTestingOnlyResponse>();
            response!.CallerId.Should().Be(CallerConstants.AnonymousUserId);
        }

        [Fact]
        public async Task WhenAnonymousRequestAndAuthNCookie_ThenReturns()
        {
            var (userId, _) = await HttpApi.LoginUserFromBrowserAsync(JsonOptions, CSRFService);

            var request = new BeffeAnonymousDirectTestingOnlyRequest();
            var result = await HttpApi.PostEmptyJsonAsync(request.MakeApiRoute());

            result.StatusCode.Should().Be(HttpStatusCode.OK);
            var response = await result.Content.ReadFromJsonAsync<BeffeTestingOnlyResponse>();
            response!.CallerId.Should().Be(userId);
        }

        [Fact]
        public async Task WhenHmacAuthRequest_ThenReturns()
        {
            var request = new BeffeHMacDirectTestingOnlyRequest();
            var result = await HttpApi.PostEmptyJsonAsync(request.MakeApiRoute(),
                (msg, _) => msg.SetHMACAuth(request, "asecret"));

            result.StatusCode.Should().Be(HttpStatusCode.OK);
            var response = await result.Content.ReadFromJsonAsync<BeffeTestingOnlyResponse>();
            response!.CallerId.Should().Be(CallerConstants.MaintenanceAccountUserId);
        }
    }

    [Trait("Category", "Integration.Website")]
    [Collection("WEBSITE")]
    public class GivenCSRFRoutes : WebsiteSpec<Program, ApiHost1.Program>
    {
        public GivenCSRFRoutes(WebApiSetup<Program> setup) : base(setup)
        {
        }

        [Fact]
        public async Task WhenAnonymousRequestAndNoAuthNCookie_ThenReturns()
        {
            var request = new BeffeAnonymousTestingOnlyRequest();
            var result = await HttpApi.PostEmptyJsonAsync(request.MakeApiRoute(),
                (msg, cookies) => msg.WithCSRF(cookies, CSRFService));

            result.StatusCode.Should().Be(HttpStatusCode.OK);
            var response = await result.Content.ReadFromJsonAsync<BeffeTestingOnlyResponse>();
            response!.CallerId.Should().Be(CallerConstants.AnonymousUserId);
        }

        [Fact]
        public async Task WhenAnonymousRequestAndAuthNCookie_ThenReturns()
        {
            var (userId, _) = await HttpApi.LoginUserFromBrowserAsync(JsonOptions, CSRFService);

            var request = new BeffeAnonymousTestingOnlyRequest();
            var result = await HttpApi.PostEmptyJsonAsync(request.MakeApiRoute(),
                (msg, cookies) => msg.WithCSRF(cookies, CSRFService, userId));

            result.StatusCode.Should().Be(HttpStatusCode.OK);
            var response = await result.Content.ReadFromJsonAsync<BeffeTestingOnlyResponse>();
            response!.CallerId.Should().Be(userId);
        }
    }
}

#endif