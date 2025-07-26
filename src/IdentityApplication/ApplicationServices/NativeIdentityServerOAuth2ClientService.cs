using Application.Common.Extensions;
using Application.Interfaces;
using Application.Persistence.Common.Extensions;
using Application.Resources.Shared;
using Application.Services.Shared;
using Common;
using Common.Extensions;
using Domain.Common.Identity;
using Domain.Common.ValueObjects;
using Domain.Services.Shared;
using Domain.Shared;
using IdentityApplication.Persistence;
using IdentityDomain;
using IdentityDomain.DomainServices;

namespace IdentityApplication.ApplicationServices;

/// <summary>
///     Provides a native OAuth2 client management service for managing and persisting OAuth2 clients
///     OAuth2 Specification: <see href="https://datatracker.ietf.org/doc/html/rfc6749#section-2" />
/// </summary>
public class NativeIdentityServerOAuth2ClientService : IIdentityServerOAuth2ClientService
{
    private readonly IOAuth2ClientConsentRepository _clientConsentRepository;
    private readonly IOAuth2ClientRepository _clientRepository;
    private readonly IIdentifierFactory _identifierFactory;
    private readonly IPasswordHasherService _passwordHasherService;
    private readonly IRecorder _recorder;
    private readonly ITokensService _tokensService;

    public NativeIdentityServerOAuth2ClientService(IRecorder recorder, IIdentifierFactory identifierFactory,
        ITokensService tokensService, IPasswordHasherService passwordHasherService,
        IOAuth2ClientRepository clientRepository, IOAuth2ClientConsentRepository clientConsentRepository)
    {
        _recorder = recorder;
        _identifierFactory = identifierFactory;
        _tokensService = tokensService;
        _passwordHasherService = passwordHasherService;
        _clientRepository = clientRepository;
        _clientConsentRepository = clientConsentRepository;
    }

    public async Task<Result<bool, Error>> ConsentToClientAsync(ICallerContext caller, string clientId, string userId,
        string? scope, bool isConsented, CancellationToken cancellationToken)
    {
        var retrievedClient = await _clientRepository.LoadAsync(clientId.ToId(), cancellationToken);
        if (retrievedClient.IsFailure)
        {
            return retrievedClient.Error;
        }

        var retrieved = await _clientConsentRepository.FindByUserId(clientId.ToId(), userId.ToId(), cancellationToken);
        if (retrieved.IsFailure)
        {
            return retrieved.Error;
        }

        OAuth2ClientConsentRoot consent;
        if (retrieved.Value.HasValue)
        {
            consent = retrieved.Value.Value;
        }
        else
        {
            var created = OAuth2ClientConsentRoot.Create(_recorder, _identifierFactory, clientId.ToId(), userId.ToId());
            if (created.IsFailure)
            {
                return created.Error;
            }

            consent = created.Value;
        }

        if (consent.IsConsented)
        {
            return true;
        }

        var consentedScopes = OAuth2Scopes.Create(scope);
        if (consentedScopes.IsFailure)
        {
            return consentedScopes.Error;
        }

        var consented = consent.ChangeConsent(userId.ToId(), isConsented, consentedScopes.Value);
        if (consented.IsFailure)
        {
            return consented.Error;
        }

        var saved = await _clientConsentRepository.SaveAsync(consent, cancellationToken);
        if (saved.IsFailure)
        {
            return saved.Error;
        }

        consent = saved.Value;
        _recorder.TraceInformation(caller.ToCall(), consent.IsConsented
            ? "Client {Id} was consented to by user {UserId}"
            : "Client {Id} was un-consented from by user {UserId}", consent.ClientId, consent.UserId);

        return consent.IsConsented;
    }

    public async Task<Result<OAuth2Client, Error>> CreateClientAsync(ICallerContext caller, string name,
        string? redirectUri, CancellationToken cancellationToken)
    {
        var clientName = Name.Create(name);
        if (clientName.IsFailure)
        {
            return clientName.Error;
        }

        var created = OAuth2ClientRoot.Create(_recorder, _identifierFactory, _tokensService, _passwordHasherService,
            clientName.Value);
        if (created.IsFailure)
        {
            return created.Error;
        }

        var client = created.Value;
        if (redirectUri.HasValue())
        {
            var updated = client.ChangeRedirectUri(redirectUri);
            if (updated.IsFailure)
            {
                return updated.Error;
            }
        }

        var saved = await _clientRepository.SaveAsync(client, cancellationToken);
        if (saved.IsFailure)
        {
            return saved.Error;
        }

        client = saved.Value;
        _recorder.TraceInformation(caller.ToCall(), "Client {Id} created", client.Id);

        return client.ToClient();
    }

    public async Task<Result<Error>> DeleteClientAsync(ICallerContext caller, string id,
        CancellationToken cancellationToken)
    {
        var retrieved = await _clientRepository.LoadAsync(id.ToId(), cancellationToken);
        if (retrieved.IsFailure)
        {
            return retrieved.Error;
        }

        var client = retrieved.Value;
        var deleted = client.Delete(caller.ToCallerId());
        if (deleted.IsFailure)
        {
            return deleted.Error;
        }

        var saved = await _clientRepository.SaveAsync(client, cancellationToken);
        if (saved.IsFailure)
        {
            return saved.Error;
        }

        _recorder.TraceInformation(caller.ToCall(), "Client {Id} deleted", client.Id);

        return Result.Ok;
    }

    public async Task<Result<Optional<OAuth2Client>, Error>> FindClientByIdAsync(ICallerContext caller, string id,
        CancellationToken cancellationToken)
    {
        var retrieved = await _clientRepository.FindById(id.ToId(), cancellationToken);
        if (retrieved.IsFailure)
        {
            return retrieved.Error;
        }

        var client = retrieved.Value;
        if (!client.HasValue)
        {
            return Optional<OAuth2Client>.None;
        }

        return client.Value.ToClient().ToOptional();
    }

    public async Task<Result<OAuth2Client, Error>> GetClientAsync(ICallerContext caller, string id,
        CancellationToken cancellationToken)
    {
        var retrieved = await _clientRepository.LoadAsync(id.ToId(), cancellationToken);
        if (retrieved.IsFailure)
        {
            return retrieved.Error;
        }

        var client = retrieved.Value;
        _recorder.TraceInformation(caller.ToCall(), "Client {Id} was fetched", client.Id);

        return client.ToClient();
    }

    public async Task<Result<bool, Error>> GetConsentAsync(ICallerContext caller, string clientId, string userId,
        CancellationToken cancellationToken)
    {
        var retrievedClient = await _clientRepository.LoadAsync(clientId.ToId(), cancellationToken);
        if (retrievedClient.IsFailure)
        {
            return retrievedClient.Error;
        }

        var retrieved = await _clientConsentRepository.FindByUserId(clientId.ToId(), userId.ToId(), cancellationToken);
        if (retrieved.IsFailure)
        {
            return retrieved.Error;
        }

        if (!retrieved.Value.HasValue)
        {
            return false;
        }

        var consent = retrieved.Value.Value;
        _recorder.TraceInformation(caller.ToCall(), "Consent for client {ClientId} and user {UserId} was retrieved",
            consent.ClientId, consent.UserId);

        return consent.IsConsented;
    }

    public async Task<Result<OAuth2ClientWithSecret, Error>> RegenerateClientSecretAsync(ICallerContext caller,
        string id, CancellationToken cancellationToken)
    {
        var retrieved = await _clientRepository.LoadAsync(id.ToId(), cancellationToken);
        if (retrieved.IsFailure)
        {
            return retrieved.Error;
        }

        var client = retrieved.Value;
        var generated = client.GenerateSecret(Optional<TimeSpan>.None);
        if (generated.IsFailure)
        {
            return generated.Error;
        }

        var secret = generated.Value;
        var saved = await _clientRepository.SaveAsync(client, cancellationToken);
        if (saved.IsFailure)
        {
            return saved.Error;
        }

        client = saved.Value;
        _recorder.TraceInformation(caller.ToCall(), "Client {Id} generated a new secret", client.Id);

        return client.ToClientWithSecret(secret);
    }

    public async Task<Result<Error>> RevokeConsentAsync(ICallerContext caller, string clientId, string userId,
        CancellationToken cancellationToken)
    {
        var retrievedClient = await _clientRepository.LoadAsync(clientId.ToId(), cancellationToken);
        if (retrievedClient.IsFailure)
        {
            return retrievedClient.Error;
        }

        var retrieved = await _clientConsentRepository.FindByUserId(clientId.ToId(), userId.ToId(), cancellationToken);
        if (retrieved.IsFailure)
        {
            return retrieved.Error;
        }

        if (!retrieved.Value.HasValue)
        {
            return Error.EntityNotFound();
        }

        var consent = retrieved.Value.Value;
        var revoked = consent.Revoke(userId.ToId());
        if (revoked.IsFailure)
        {
            return revoked.Error;
        }

        if (revoked.Value)
        {
            var saved = await _clientConsentRepository.SaveAsync(consent, cancellationToken);
            if (saved.IsFailure)
            {
                return saved.Error;
            }

            consent = saved.Value;
            _recorder.TraceInformation(caller.ToCall(), "Consent for client {ClientId} and user {UserId} was revoked",
                consent.ClientId, consent.UserId);
        }

        return Result.Ok;
    }

    public async Task<Result<SearchResults<OAuth2Client>, Error>> SearchAllClientsAsync(ICallerContext caller,
        SearchOptions searchOptions, GetOptions getOptions, CancellationToken cancellationToken)
    {
        var searched = await _clientRepository.SearchAllAsync(searchOptions, cancellationToken);
        if (searched.IsFailure)
        {
            return searched.Error;
        }

        var clients = searched.Value;
        _recorder.TraceInformation(caller.ToCall(), "All clients were fetched");

        return clients.ToSearchResults(searchOptions, client => client.ToClient());
    }

    public async Task<Result<OAuth2Client, Error>> UpdateClientAsync(ICallerContext caller, string id, string? name,
        string? redirectUri, CancellationToken cancellationToken)
    {
        var retrieved = await _clientRepository.LoadAsync(id.ToId(), cancellationToken);
        if (retrieved.IsFailure)
        {
            return retrieved.Error;
        }

        var client = retrieved.Value;
        if (name.HasValue())
        {
            var clientName = Name.Create(name);
            if (clientName.IsFailure)
            {
                return clientName.Error;
            }

            var updated = client.ChangeName(clientName.Value);
            if (updated.IsFailure)
            {
                return updated.Error;
            }
        }

        if (redirectUri.HasValue())
        {
            var updated = client.ChangeRedirectUri(redirectUri);
            if (updated.IsFailure)
            {
                return updated.Error;
            }
        }

        var saved = await _clientRepository.SaveAsync(client, cancellationToken);
        if (saved.IsFailure)
        {
            return saved.Error;
        }

        client = saved.Value;
        _recorder.TraceInformation(caller.ToCall(), "Client {Id} updated", client.Id);

        return client.ToClient();
    }
}

public static class NativeIdentityServerOAuth2ClientServiceConversionExtensions
{
    public static OAuth2Client ToClient(this OAuth2ClientRoot client)
    {
        return new OAuth2Client
        {
            Id = client.Id,
            Name = client.Name.ValueOrDefault!,
            RedirectUri = client.RedirectUri.ValueOrDefault!
        };
    }

    public static OAuth2Client ToClient(this Persistence.ReadModels.OAuth2Client client)
    {
        return new OAuth2Client
        {
            Id = client.Id,
            Name = client.Name,
            RedirectUri = client.RedirectUri
        };
    }

    public static OAuth2ClientWithSecret ToClientWithSecret(this OAuth2ClientRoot client, GeneratedClientSecret secret)
    {
        return new OAuth2ClientWithSecret
        {
            Id = client.Id,
            Name = client.Name.ValueOrDefault!,
            RedirectUri = client.RedirectUri,
            Secret = secret.PlainSecret,
            ExpiresOnUtc = secret.ExpiresOn.HasValue
                ? secret.ExpiresOn.Value
                : null
        };
    }
}