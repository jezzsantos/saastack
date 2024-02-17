using Application.Common.Extensions;
using Application.Interfaces;
using Application.Resources.Shared;
using Common;
using Common.Extensions;
using Infrastructure.Interfaces;
using Infrastructure.Web.Api.Operations.Shared.Identities;
using Infrastructure.Web.Common.Extensions;
using Infrastructure.Web.Interfaces.Clients;

namespace WebsiteHost.Application;

public class AuthenticationApplication : IAuthenticationApplication
{
    private readonly IRecorder _recorder;
    private readonly IServiceClient _serviceClient;

    public AuthenticationApplication(IRecorder recorder, IServiceClient serviceClient)
    {
        _recorder = recorder;
        _serviceClient = serviceClient;
    }

    public async Task<Result<AuthenticateTokens, Error>> AuthenticateAsync(ICallerContext context, string provider,
        string? authCode, string? username, string? password, CancellationToken cancellationToken)
    {
        Task<Result<AuthenticateResponse, ResponseProblem>> request;
        switch (provider)
        {
            case AuthenticationConstants.Providers.Credentials:
                request = _serviceClient.PostAsync(context, new AuthenticatePasswordRequest
                {
                    Username = username!,
                    Password = password!
                }, null, cancellationToken);
                break;

            default:
                request = _serviceClient.PostAsync(context, new AuthenticateSingleSignOnRequest
                {
                    AuthCode = authCode!,
                    Provider = provider
                }, null, cancellationToken);
                break;
        }

        var authenticated = await request;
        if (!authenticated.IsSuccessful)
        {
            return authenticated.Error.ToError();
        }

        _recorder.TrackUsage(context.ToCall(), UsageConstants.Events.UsageScenarios.UserLogin);

        return authenticated.Value.ToTokens();
    }

    public Task<Result<Error>> LogoutAsync(ICallerContext context, CancellationToken cancellationToken)
    {
        _recorder.TrackUsage(context.ToCall(), UsageConstants.Events.UsageScenarios.UserLogout);

        return Task.FromResult(Result.Ok);
    }

    public async Task<Result<AuthenticateTokens, Error>> RefreshTokenAsync(ICallerContext context, string? refreshToken,
        CancellationToken cancellationToken)
    {
        if (!refreshToken.HasValue())
        {
            return Error.NotAuthenticated();
        }

        var refreshed = await _serviceClient.PostAsync(context, new RefreshTokenRequest
        {
            RefreshToken = refreshToken
        }, null, cancellationToken);
        if (!refreshed.IsSuccessful)
        {
            return refreshed.Error.ToError();
        }

        _recorder.TrackUsage(context.ToCall(), UsageConstants.Events.UsageScenarios.UserExtendedLogin);

        return refreshed.Value.ToTokens();
    }
}

internal static class AuthenticationConversionExtensions
{
    public static AuthenticateTokens ToTokens(this AuthenticateResponse response)
    {
        return response.Tokens!;
    }

    public static AuthenticateTokens ToTokens(this RefreshTokenResponse response)
    {
        return response.Tokens!;
    }
}