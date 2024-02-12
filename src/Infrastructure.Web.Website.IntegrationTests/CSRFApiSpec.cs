using System.Net;
using System.Text.Json;
using Common.Extensions;
using FluentAssertions;
using HtmlAgilityPack;
using Infrastructure.Web.Api.Common.Extensions;
using Infrastructure.Web.Api.Operations.Shared.TestingOnly;
using Infrastructure.Web.Hosting.Common;
using Infrastructure.Web.Hosting.Common.Pipeline;
using IntegrationTesting.WebApi.Common;
using JetBrains.Annotations;
using WebsiteHost;
using Xunit;

namespace Infrastructure.Web.Website.IntegrationTests;

[UsedImplicitly]
public class CSRFApiSpec
{
    [Trait("Category", "Integration.Web")]
    [Collection("API")]
    public class GivenNoContext : WebApiSpec<Program>
    {
        public GivenNoContext(WebApiSetup<Program> setup) : base(setup)
        {
            StartupServer<ApiHost1.Program>();
            var csrfService = setup.GetRequiredService<CSRFMiddleware.ICSRFService>();
#if TESTINGONLY
            HttpApi.PostEmptyJsonAsync(new DestroyAllRepositoriesRequest().MakeApiRoute(),
                    (message, cookies) => message.WithCSRF(cookies, csrfService)).GetAwaiter()
                .GetResult();
#endif
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

    [Trait("Category", "Integration.Web")]
    [Collection("API")]
    public class GivenAnInsecureGetRequest : WebApiSpec<Program>
    {
        private readonly CSRFMiddleware.ICSRFService _csrfService;
        private readonly JsonSerializerOptions _jsonOptions;

        public GivenAnInsecureGetRequest(WebApiSetup<Program> setup) : base(setup)
        {
            StartupServer<ApiHost1.Program>();
            _csrfService = setup.GetRequiredService<CSRFMiddleware.ICSRFService>();
#if TESTINGONLY
            HttpApi.PostEmptyJsonAsync(new DestroyAllRepositoriesRequest().MakeApiRoute(),
                    (message, cookies) => message.WithCSRF(cookies, _csrfService)).GetAwaiter()
                .GetResult();
#endif
            _jsonOptions = setup.GetRequiredService<JsonSerializerOptions>();
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
                (message, cookies) => message.WithCSRF(cookies, _csrfService));

            result.StatusCode.Should().Be(HttpStatusCode.OK);
#endif
        }

        [Fact]
        public async Task WhenRequestedForUserWithCSRFToken_ThenSucceeds()
        {
            var (userId, _) = await HttpApi.LoginUserFromBrowserAsync(_jsonOptions, _csrfService);

#if TESTINGONLY
            var result = await HttpApi.GetAsync(new GetInsecureTestingOnlyRequest().MakeApiRoute(),
                (message, cookies) => message.WithCSRF(cookies, _csrfService, userId));

            result.StatusCode.Should().Be(HttpStatusCode.OK);
#endif
        }
    }

    [Trait("Category", "Integration.Web")]
    [Collection("API")]
    public class GivenAnInsecurePostRequestByAnonymousUser : WebApiSpec<Program>
    {
        private readonly CSRFMiddleware.ICSRFService _csrfService;
        private readonly JsonSerializerOptions _jsonOptions;

        public GivenAnInsecurePostRequestByAnonymousUser(WebApiSetup<Program> setup) : base(setup)
        {
            StartupServer<ApiHost1.Program>();
            _csrfService = setup.GetRequiredService<CSRFMiddleware.ICSRFService>();
#if TESTINGONLY
            HttpApi.PostEmptyJsonAsync(new DestroyAllRepositoriesRequest().MakeApiRoute(),
                    (message, cookies) => message.WithCSRF(cookies, _csrfService)).GetAwaiter()
                .GetResult();
#endif
            _jsonOptions = setup.GetRequiredService<JsonSerializerOptions>();
        }

        [Fact]
        public async Task WhenRequestedWithNoCSRFToken_ThenForbidden()
        {
#if TESTINGONLY
            var result = await HttpApi.PostEmptyJsonAsync(new PostInsecureTestingOnlyRequest().MakeApiRoute());

            result.StatusCode.Should().Be(HttpStatusCode.Forbidden);
            var problem = await result.AsProblemAsync(_jsonOptions);
            problem!.Detail.Should().Be(Resources.CSRFMiddleware_MissingCSRFHeaderValue);
#endif
        }

        [Fact]
        public async Task WhenRequestedCSRFToken_ThenSucceeds()
        {
#if TESTINGONLY
            var result = await HttpApi.PostEmptyJsonAsync(new PostInsecureTestingOnlyRequest().MakeApiRoute(),
                (message, cookies) => message.WithCSRF(cookies, _csrfService));

            result.StatusCode.Should().Be(HttpStatusCode.OK);
#endif
        }

        [Fact]
        public async Task WhenRequestedWithMismatchedCookieAndHeaderForAnonymous_ThenSucceeds()
        {
#if TESTINGONLY
            var result = await HttpApi.PostEmptyJsonAsync(new PostInsecureTestingOnlyRequest().MakeApiRoute(),
                (message, cookies) =>
                {
                    var anonymous1 = _csrfService.CreateTokens(null);
                    var anonymous2 = _csrfService.CreateTokens(null);

                    message.WithCSRF(cookies, anonymous1.Token, anonymous2.Signature);
                });

            result.StatusCode.Should().Be(HttpStatusCode.OK);
#endif
        }

        [Fact]
        public async Task WhenRequestedWithAnonymousHeaderAndUserCookie_ThenForbidden()
        {
#if TESTINGONLY
            var result = await HttpApi.PostEmptyJsonAsync(new PostInsecureTestingOnlyRequest().MakeApiRoute(),
                (message, cookies) =>
                {
                    var anonymous = _csrfService.CreateTokens(null);
                    var user = _csrfService.CreateTokens("auserid");

                    message.WithCSRF(cookies, anonymous.Token, user.Signature);
                });

            result.StatusCode.Should().Be(HttpStatusCode.Forbidden);
            var problem = await result.AsProblemAsync(_jsonOptions);
            problem!.Detail.Should().Be(Resources.CSRFMiddleware_InvalidSignature.Format("None"));
#endif
        }

        [Fact]
        public async Task WhenRequestedWithUserHeaderAndAnonymousCookie_ThenForbidden()
        {
#if TESTINGONLY
            var result = await HttpApi.PostEmptyJsonAsync(new PostInsecureTestingOnlyRequest().MakeApiRoute(),
                (message, cookies) =>
                {
                    var anonymous = _csrfService.CreateTokens(null);
                    var user = _csrfService.CreateTokens("auserid");

                    message.WithCSRF(cookies, user.Token, anonymous.Signature);
                });

            result.StatusCode.Should().Be(HttpStatusCode.Forbidden);
            var problem = await result.AsProblemAsync(_jsonOptions);
            problem!.Detail.Should().Be(Resources.CSRFMiddleware_InvalidSignature.Format("None"));
#endif
        }

        [Fact]
        public async Task WhenRequestedWithUserCSRFToken_ThenForbidden()
        {
#if TESTINGONLY
            var result = await HttpApi.PostEmptyJsonAsync(new PostInsecureTestingOnlyRequest().MakeApiRoute(),
                (message, cookies) => message.WithCSRF(cookies, _csrfService, "auserid"));

            result.StatusCode.Should().Be(HttpStatusCode.Forbidden);
            var problem = await result.AsProblemAsync(_jsonOptions);
            problem!.Detail.Should().Be(Resources.CSRFMiddleware_InvalidSignature.Format("None"));
#endif
        }
    }

    [Trait("Category", "Integration.Web")]
    [Collection("API")]
    public class GivenAnInsecurePostRequestByAuthenticatedUser : WebApiSpec<Program>
    {
        private readonly CSRFMiddleware.ICSRFService _csrfService;
        private readonly JsonSerializerOptions _jsonOptions;
        private readonly string _userId;

        public GivenAnInsecurePostRequestByAuthenticatedUser(WebApiSetup<Program> setup) : base(setup)
        {
            StartupServer<ApiHost1.Program>();
            _csrfService = setup.GetRequiredService<CSRFMiddleware.ICSRFService>();
#if TESTINGONLY
            HttpApi.PostEmptyJsonAsync(new DestroyAllRepositoriesRequest().MakeApiRoute(),
                    (message, cookies) => message.WithCSRF(cookies, _csrfService)).GetAwaiter()
                .GetResult();
#endif
            _jsonOptions = setup.GetRequiredService<JsonSerializerOptions>();

            var (userId, _) = HttpApi.LoginUserFromBrowserAsync(_jsonOptions, _csrfService).GetAwaiter().GetResult();
            _userId = userId;
        }

        [Fact]
        public async Task WhenRequestedWithAnonymousCSRFToken_ThenForbidden()
        {
#if TESTINGONLY
            var result = await HttpApi.PostEmptyJsonAsync(new PostInsecureTestingOnlyRequest().MakeApiRoute(),
                (message, cookies) => message.WithCSRF(cookies, _csrfService));

            result.StatusCode.Should().Be(HttpStatusCode.Forbidden);
            var problem = await result.AsProblemAsync(_jsonOptions);
            problem!.Detail.Should().Be(Resources.CSRFMiddleware_InvalidSignature.Format(_userId));
#endif
        }

        [Fact]
        public async Task WhenRequestedWithUsersCSRFToken_ThenSucceeds()
        {
#if TESTINGONLY
            var result = await HttpApi.PostEmptyJsonAsync(new PostInsecureTestingOnlyRequest().MakeApiRoute(),
                (message, cookies) => message.WithCSRF(cookies, _csrfService, _userId));

            result.StatusCode.Should().Be(HttpStatusCode.OK);
#endif
        }

        [Fact]
        public async Task WhenRequestedWithMismatchedCookieAndHeaderForDifferentUsers_ThenForbidden()
        {
#if TESTINGONLY
            var result = await HttpApi.PostEmptyJsonAsync(new PostInsecureTestingOnlyRequest().MakeApiRoute(),
                (message, cookies) =>
                {
                    var user1 = _csrfService.CreateTokens(_userId);
                    var user2 = _csrfService.CreateTokens("anotheruserid");

                    message.WithCSRF(cookies, user1.Token, user2.Signature);
                });

            result.StatusCode.Should().Be(HttpStatusCode.Forbidden);
            var problem = await result.AsProblemAsync(_jsonOptions);
            problem!.Detail.Should().Be(Resources.CSRFMiddleware_InvalidSignature.Format(_userId));
#endif
        }

        [Fact]
        public async Task WhenRequestedWithMismatchedCookieAndHeaderForSameUser_ThenSucceeds()
        {
#if TESTINGONLY
            var result = await HttpApi.PostEmptyJsonAsync(new PostInsecureTestingOnlyRequest().MakeApiRoute(),
                (message, cookies) =>
                {
                    var user1 = _csrfService.CreateTokens(_userId);
                    var user2 = _csrfService.CreateTokens(_userId);

                    message.WithCSRF(cookies, user1.Token, user2.Signature);
                });

            result.StatusCode.Should().Be(HttpStatusCode.OK);
#endif
        }
    }
}