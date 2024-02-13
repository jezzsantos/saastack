using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Common;
using Common.Extensions;
using Infrastructure.Interfaces;
using Infrastructure.Web.Api.Common;
using Infrastructure.Web.Api.Common.Extensions;
using Infrastructure.Web.Api.Interfaces;
using Infrastructure.Web.Api.Operations.Shared.BackEndForFrontEnd;
using Infrastructure.Web.Api.Operations.Shared.Identities;
using Infrastructure.Web.Common;
using Infrastructure.Web.Hosting.Common.Pipeline;
using IntegrationTesting.WebApi.Common;
using AuthenticateResponse = Infrastructure.Web.Api.Operations.Shared.BackEndForFrontEnd.AuthenticateResponse;

namespace Infrastructure.Web.Website.IntegrationTests;

public static class WebsiteTestingExtensions
{
    public static async Task<(string UserId, HttpResponseMessage Response)> AuthenticateUserFromBrowserAsync(
        this IHttpClient websiteClient, JsonSerializerOptions jsonOptions, CSRFMiddleware.ICSRFService csrfService,
        string emailAddress, string password)
    {
        // This call should populate the auth cookies
        var authenticateRequest = new AuthenticateRequest
        {
            Provider = AuthenticationConstants.Providers.Credentials,
            Username = emailAddress,
            Password = password
        };
        var authenticateUrl = authenticateRequest.MakeApiRoute();
        var authenticated = await websiteClient.PostAsync(authenticateUrl, JsonContent.Create(authenticateRequest),
            (msg, cookies) => msg.WithCSRF(cookies, csrfService));
        var authTokens = (await authenticated.Content.ReadFromJsonAsync<AuthenticateResponse>(jsonOptions))!;

        return (authTokens.UserId, authenticated)!;
    }

    public static Optional<string> GetCookie(this HttpResponseMessage responseMessage, string name)
    {
        if (!responseMessage.Headers.TryGetValues(HttpHeaders.SetCookie, out var cookies))
        {
            return Optional<string>.None;
        }

        var cookie = cookies
            .FirstOrDefault(s => s.StartsWith($"{name}="));
        if (cookie.NotExists())
        {
            return Optional<string>.None;
        }

        var startOfValue = cookie.IndexOf("=", StringComparison.Ordinal) + 1;
        var indexOfDelimiter = cookie.IndexOf(";", StringComparison.Ordinal);
        var endOfValue = indexOfDelimiter == -1
            ? cookie.Length
            : indexOfDelimiter;

        var value = cookie.Substring(startOfValue, endOfValue - startOfValue);
        return value.HasValue()
            ? value
            : Optional<string>.None;
    }

    public static async Task<(string UserId, HttpResponseMessage Response)> LoginUserFromBrowserAsync(
        this IHttpClient websiteClient, JsonSerializerOptions jsonOptions, CSRFMiddleware.ICSRFService csrfService)
    {
        const string emailAddress = "auser@company.com";
        const string password = "1Password!";

        await RegisterPersonUserFromBrowserAsync(websiteClient, jsonOptions, csrfService, emailAddress, password);

        return await AuthenticateUserFromBrowserAsync(websiteClient, jsonOptions, csrfService, emailAddress, password);
    }

    public static string MakeApiRoute(this IWebRequest request)
    {
        return $"{WebConstants.BackEndForFrontEndBasePath}{request.GetRequestInfo().Route}";
    }

    public static async Task<string> RegisterPersonUserFromBrowserAsync(this IHttpClient websiteClient,
        JsonSerializerOptions jsonOptions, CSRFMiddleware.ICSRFService csrfService, string emailAddress,
        string password)
    {
        var registrationRequest = new RegisterPersonPasswordRequest
        {
            EmailAddress = emailAddress,
            FirstName = "afirstname",
            LastName = "alastname",
            Password = password,
            TermsAndConditionsAccepted = true
        };
        var registrationUrl = registrationRequest.MakeApiRoute();
        var person = await websiteClient.PostAsync(registrationUrl, JsonContent.Create(registrationRequest),
            (msg, cookies) => msg.WithCSRF(cookies, csrfService));

        var userId = (await person.Content.ReadFromJsonAsync<RegisterPersonPasswordResponse>(jsonOptions))!
            .Credential!.User.Id;

#if TESTINGONLY
        var getTokenRequest = new GetRegistrationPersonConfirmationRequest
        {
            UserId = userId
        };
        var getTokenUrl = getTokenRequest.MakeApiRoute();
        var confirmationToken = await websiteClient.GetAsync(getTokenUrl,
            (msg, cookies) => msg.WithCSRF(cookies, csrfService));
        var token =
            (await confirmationToken.Content.ReadFromJsonAsync<GetRegistrationPersonConfirmationResponse>(jsonOptions))!
            .Token;
        var confirmationRequest = new ConfirmRegistrationPersonPasswordRequest
        {
            Token = token!
        };
        var confirmationUrl = confirmationRequest.MakeApiRoute();
        await websiteClient.PostAsync(confirmationUrl, JsonContent.Create(confirmationRequest),
            (msg, cookies) => msg.WithCSRF(cookies, csrfService));
#endif
        return userId;
    }

    public static void WithCSRF(this HttpRequestMessage message, CookieContainer cookies,
        CSRFMiddleware.ICSRFService csrfService,
        string? userId = null)
    {
        var tokens = csrfService.CreateTokens(userId);

        WithCSRF(message, cookies, tokens.Token, tokens.Signature);
    }

    public static void WithCSRF(this HttpRequestMessage message, CookieContainer cookies,
        string token, string signature)
    {
        var host = message.RequestUri!.Host;
        cookies.Add(new Cookie(CSRFConstants.Cookies.AntiCSRF, signature, "/", host));
        message.Headers.Add(CSRFConstants.Headers.AntiCSRF, token);
        var origin = $"{message.RequestUri.Scheme}{Uri.SchemeDelimiter}{message.RequestUri.Authority}";
        message.Headers.Add(HttpHeaders.Origin, origin);
    }

}