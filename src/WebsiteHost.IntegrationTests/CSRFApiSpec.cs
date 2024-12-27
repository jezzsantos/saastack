using System.Net;
using Common.Extensions;
using FluentAssertions;
using HtmlAgilityPack;
using Infrastructure.Web.Api.Operations.Shared.TestingOnly;
using Infrastructure.Web.Common.Extensions;
using Infrastructure.Web.Hosting.Common.Pipeline;
using IntegrationTesting.WebApi.Common;
using IntegrationTesting.WebApi.Common.Extensions;
using IntegrationTesting.Website.Common;
using JetBrains.Annotations;
using Xunit;

namespace WebsiteHost.IntegrationTests;

[UsedImplicitly]
public class CSRFApiSpec
{
    [Trait("Category", "Integration.Website")]
    [Collection("WEBSITE")]
    public class GivenNoContext : WebsiteSpec<Program, ApiHost1.Program>
    {
        public GivenNoContext(WebApiSetup<Program> setup) : base(setup)
        {
        }

        [Fact]
        public async Task WhenRequestWebRoot_ThenReturnsIndexHtml()
        {
            var result = await HttpApi.GetAsync("/");

            var content = await result.Content.ReadAsStringAsync();
            content.Should().Contain("<html");
            result.GetCookie(CSRFConstants.Cookies.AntiCSRF).Value.Should().NotBeEmpty();
            result.Headers.TryGetValues(CSRFConstants.Headers.AntiCSRF, out _).Should().BeFalse();

            var doc = new HtmlDocument();
            doc.Load(await result.Content.ReadAsStreamAsync());

            var csrfTokenMetaTag = doc.DocumentNode
                .SelectNodes($"//meta[@name='{CSRFConstants.Html.CSRFRequestFieldName}']")
                ?.FirstOrDefault();

            csrfTokenMetaTag.Should()
                .NotBeNull("Index.html should have contained a <meta> tag with the name: {0}".Format(CSRFConstants.Html
                    .CSRFRequestFieldName));
            csrfTokenMetaTag?.Attributes["content"].Value.Should().NotBeNull(
                "The <meta> tag named '{0}' should have been replaced with a real token".Format(CSRFConstants.Html
                    .CSRFRequestFieldName));
            csrfTokenMetaTag?.Attributes["content"].Value.Should().NotBe(CSRFConstants.Html.CSRFTokenPlaceholder,
                "The <meta> tag named '{0}' should have been replaced with a real token".Format(CSRFConstants.Html
                    .CSRFRequestFieldName));
        }
    }

    [Trait("Category", "Integration.Website")]
    [Collection("WEBSITE")]
    public class GivenAnInsecureGetRequest : WebsiteSpec<Program, ApiHost1.Program>
    {
        public GivenAnInsecureGetRequest(WebApiSetup<Program> setup) : base(setup)
        {
        }

        [Fact]
        public async Task WhenRequestedWithNoCSRFToken_ThenSucceeds()
        {
#if TESTINGONLY
            var result = await HttpApi.GetAsync(new GetInsecureTestingOnlyRequest().MakeApiRoute());

            result.StatusCode.Should().Be(HttpStatusCode.OK);
#endif
        }

        [Fact]
        public async Task WhenRequestedForAnonymousWithCSRFToken_ThenSucceeds()
        {
#if TESTINGONLY
            var result = await HttpApi.GetAsync(new GetInsecureTestingOnlyRequest().MakeApiRoute(),
                (message, cookies) => message.WithCSRF(cookies, CSRFService));

            result.StatusCode.Should().Be(HttpStatusCode.OK);
#endif
        }

        [Fact]
        public async Task WhenRequestedForUserWithCSRFToken_ThenSucceeds()
        {
            var (userId, _) = await HttpApi.LoginUserFromBrowserAsync(JsonOptions, CSRFService);

#if TESTINGONLY
            var result = await HttpApi.GetAsync(new GetInsecureTestingOnlyRequest().MakeApiRoute(),
                (message, cookies) => message.WithCSRF(cookies, CSRFService, userId));

            result.StatusCode.Should().Be(HttpStatusCode.OK);
#endif
        }
    }

    [Trait("Category", "Integration.Website")]
    [Collection("WEBSITE")]
    public class GivenAnInsecurePostRequestByAnonymousUser : WebsiteSpec<Program, ApiHost1.Program>
    {
        public GivenAnInsecurePostRequestByAnonymousUser(WebApiSetup<Program> setup) : base(setup)
        {
        }

        [Fact]
        public async Task WhenRequestedWithNoCSRFToken_ThenForbidden()
        {
#if TESTINGONLY
            var result = await HttpApi.PostEmptyJsonAsync(new PostInsecureTestingOnlyRequest().MakeApiRoute());

            result.StatusCode.Should().Be(HttpStatusCode.Forbidden);
            var problem = await result.AsProblemAsync(JsonOptions);
            problem!.Detail.Should()
                .Be(Infrastructure.Web.Hosting.Common.Resources.CSRFMiddleware_MissingCSRFHeaderValue);
            problem.Title.Should().Be(CSRFMiddleware.CSRFViolation);
#endif
        }

        [Fact]
        public async Task WhenRequestedWithAnonymousHeaderAndUserCookie_ThenForbidden()
        {
#if TESTINGONLY
            var result = await HttpApi.PostEmptyJsonAsync(new PostInsecureTestingOnlyRequest().MakeApiRoute(),
                (message, cookies) =>
                {
                    var anonymous = CSRFService.CreateTokens(null);
                    var user = CSRFService.CreateTokens("auserid");

                    message.WithCSRF(cookies, anonymous.Token, user.Signature, null);
                });

            result.StatusCode.Should().Be(HttpStatusCode.Forbidden);
            var problem = await result.AsProblemAsync(JsonOptions);
            problem!.Detail.Should()
                .Be(Infrastructure.Web.Hosting.Common.Resources.CSRFMiddleware_InvalidSignature);
            problem.Title.Should().Be(CSRFMiddleware.CSRFViolation);
#endif
        }

        [Fact]
        public async Task WhenRequestedWithUserHeaderAndAnonymousCookie_ThenForbidden()
        {
#if TESTINGONLY
            var result = await HttpApi.PostEmptyJsonAsync(new PostInsecureTestingOnlyRequest().MakeApiRoute(),
                (message, cookies) =>
                {
                    var anonymous = CSRFService.CreateTokens(null);
                    var user = CSRFService.CreateTokens("auserid");

                    message.WithCSRF(cookies, user.Token, anonymous.Signature, null);
                });

            result.StatusCode.Should().Be(HttpStatusCode.Forbidden);
            var problem = await result.AsProblemAsync(JsonOptions);
            problem!.Detail.Should()
                .Be(Infrastructure.Web.Hosting.Common.Resources.CSRFMiddleware_InvalidSignature);
            problem.Title.Should().Be(CSRFMiddleware.CSRFViolation);
#endif
        }

        [Fact]
        public async Task WhenRequestedWithUserHeaderAndCookie_ThenForbidden()
        {
#if TESTINGONLY
            var result = await HttpApi.PostEmptyJsonAsync(new PostInsecureTestingOnlyRequest().MakeApiRoute(),
                (message, cookies) =>
                {
                    var user = CSRFService.CreateTokens("auserid");
                    message.WithCSRF(cookies, user.Token, user.Signature, null);
                });

            result.StatusCode.Should().Be(HttpStatusCode.Forbidden);
            var problem = await result.AsProblemAsync(JsonOptions);
            problem!.Detail.Should()
                .Be(Infrastructure.Web.Hosting.Common.Resources.CSRFMiddleware_InvalidSignature);
            problem.Title.Should().Be(CSRFMiddleware.CSRFViolation);
#endif
        }

        [Fact]
        public async Task WhenRequestedWithDifferentAnonymousHeaderThanCookie_ThenSucceeds()
        {
#if TESTINGONLY
            var result = await HttpApi.PostEmptyJsonAsync(new PostInsecureTestingOnlyRequest().MakeApiRoute(),
                (message, cookies) =>
                {
                    var anonymous1 = CSRFService.CreateTokens(null);
                    var anonymous2 = CSRFService.CreateTokens(null);

                    message.WithCSRF(cookies, anonymous1.Token, anonymous2.Signature, null);
                });

            result.StatusCode.Should().Be(HttpStatusCode.OK);
#endif
        }

        [Fact]
        public async Task WhenRequestedWithDifferentAnonymousHCookieThanHeader_ThenSucceeds()
        {
#if TESTINGONLY
            var result = await HttpApi.PostEmptyJsonAsync(new PostInsecureTestingOnlyRequest().MakeApiRoute(),
                (message, cookies) =>
                {
                    var anonymous1 = CSRFService.CreateTokens(null);
                    var anonymous2 = CSRFService.CreateTokens(null);

                    message.WithCSRF(cookies, anonymous2.Token, anonymous1.Signature, null);
                });

            result.StatusCode.Should().Be(HttpStatusCode.OK);
#endif
        }

        [Fact]
        public async Task WhenRequestedWithAnonymousHeaderAndCookie_ThenSucceeds()
        {
#if TESTINGONLY
            var result = await HttpApi.PostEmptyJsonAsync(new PostInsecureTestingOnlyRequest().MakeApiRoute(),
                (message, cookies) => message.WithCSRF(cookies, CSRFService));

            result.StatusCode.Should().Be(HttpStatusCode.OK);
#endif
        }
    }

    [Trait("Category", "Integration.Website")]
    [Collection("WEBSITE")]
    public class GivenAnInsecurePostRequestByAuthenticatedUser : WebsiteSpec<Program, ApiHost1.Program>
    {
        private readonly string _userId;

        public GivenAnInsecurePostRequestByAuthenticatedUser(WebApiSetup<Program> setup) : base(setup)
        {
            var (userId, _) = HttpApi.LoginUserFromBrowserAsync(JsonOptions, CSRFService).GetAwaiter()
                .GetResult();
            _userId = userId;
        }

        [Fact]
        public async Task WhenRequestedWithAnonymousHeaderAndCookie_ThenForbidden()
        {
#if TESTINGONLY
            var result = await HttpApi.PostEmptyJsonAsync(new PostInsecureTestingOnlyRequest().MakeApiRoute(),
                (message, cookies) => message.WithCSRF(cookies, CSRFService));

            result.StatusCode.Should().Be(HttpStatusCode.Forbidden);
            var problem = await result.AsProblemAsync(JsonOptions);
            problem!.Detail.Should()
                .Be(Infrastructure.Web.Hosting.Common.Resources.CSRFMiddleware_InvalidSignature);
            problem.Title.Should().Be(CSRFMiddleware.CSRFViolation);
#endif
        }

        [Fact]
        public async Task WhenRequestedWithDifferentUserHeaderThanCookie_ThenSucceeds()
        {
#if TESTINGONLY
            var result = await HttpApi.PostEmptyJsonAsync(new PostInsecureTestingOnlyRequest().MakeApiRoute(),
                (message, cookies) =>
                {
                    var user1 = CSRFService.CreateTokens(_userId);
                    var user2 = CSRFService.CreateTokens("anotheruserid");

                    message.WithCSRF(cookies, user1.Token, user2.Signature, _userId);
                });

            result.StatusCode.Should().Be(HttpStatusCode.Forbidden);
            var problem = await result.AsProblemAsync(JsonOptions);
            problem!.Detail.Should()
                .Be(Infrastructure.Web.Hosting.Common.Resources.CSRFMiddleware_InvalidSignature);
            problem.Title.Should().Be(CSRFMiddleware.CSRFViolation);
#endif
        }

        [Fact]
        public async Task WhenRequestedWithDifferentUserCookieThanHeader_ThenSucceeds()
        {
#if TESTINGONLY
            var result = await HttpApi.PostEmptyJsonAsync(new PostInsecureTestingOnlyRequest().MakeApiRoute(),
                (message, cookies) =>
                {
                    var user1 = CSRFService.CreateTokens(_userId);
                    var user2 = CSRFService.CreateTokens("anotheruserid");

                    message.WithCSRF(cookies, user2.Token, user1.Signature, _userId);
                });

            result.StatusCode.Should().Be(HttpStatusCode.Forbidden);
            var problem = await result.AsProblemAsync(JsonOptions);
            problem!.Detail.Should()
                .Be(Infrastructure.Web.Hosting.Common.Resources.CSRFMiddleware_InvalidSignature);
            problem.Title.Should().Be(CSRFMiddleware.CSRFViolation);
#endif
        }

        [Fact]
        public async Task WhenRequestedWithUserHeaderAndCookie_ThenSucceeds()
        {
#if TESTINGONLY
            var result = await HttpApi.PostEmptyJsonAsync(new PostInsecureTestingOnlyRequest().MakeApiRoute(),
                (message, cookies) => message.WithCSRF(cookies, CSRFService, _userId));

            result.StatusCode.Should().Be(HttpStatusCode.OK);
#endif
        }

        [Fact]
        public async Task WhenRequestedWithExpiredAuthenticationAndUserHeaderAndCookie_ThenForbidden()
        {
#if TESTINGONLY
            var result = await HttpApi.PostEmptyJsonAsync(new PostInsecureTestingOnlyRequest().MakeApiRoute(),
                (message, cookies) =>
                {
                    cookies.Clear(HttpApi);
                    message.WithCSRF(cookies, CSRFService, _userId);
                });

            result.StatusCode.Should().Be(HttpStatusCode.OK);
#endif
        }
    }
}