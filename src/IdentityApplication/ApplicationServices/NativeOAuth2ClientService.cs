using Application.Interfaces;
using Application.Resources.Shared;
using Application.Services.Shared;
using Common;
using Domain.Common.Identity;
using Domain.Services.Shared;
using IdentityApplication.Persistence;
using IdentityDomain.DomainServices;

namespace IdentityApplication.ApplicationServices;

/// <summary>
///     Provides a native service that manages OAuth2 clients
/// </summary>
public class NativeOAuth2ClientService : IOAuth2ClientService
{
    private readonly IIdentityServerOAuth2ClientService _service;

    public NativeOAuth2ClientService(IRecorder recorder, IIdentifierFactory identifierFactory,
        ITokensService tokensService, IPasswordHasherService passwordHasherService,
        IOAuth2ClientRepository oAuthClientRepository, IOAuth2ClientConsentRepository oAuthClientConsentRepository)
    {
        _service =
            new NativeIdentityServerOAuth2ClientService(recorder, identifierFactory, tokensService,
                passwordHasherService, oAuthClientRepository, oAuthClientConsentRepository);
    }

    public async Task<Result<Optional<OAuth2Client>, Error>> FindClientByIdAsync(ICallerContext caller, string clientId,
        CancellationToken cancellationToken)
    {
        return await _service.FindClientByIdAsync(caller, clientId, cancellationToken);
    }

    public async Task<Result<bool, Error>> HasClientConsentedUserAsync(ICallerContext caller, string clientId,
        string userId, CancellationToken cancellationToken)
    {
        return await _service.GetConsentAsync(caller, clientId, userId, cancellationToken);
    }
}