using Application.Resources.Shared;
using IdentityApplication;
using Infrastructure.Interfaces;
using Infrastructure.Web.Api.Common.Extensions;
using Infrastructure.Web.Api.Interfaces;
using Infrastructure.Web.Api.Operations.Shared.Identities;

namespace IdentityInfrastructure.Api.MFA;

public class MfaApi : IWebApiService
{
    private readonly ICallerContextFactory _callerFactory;
    private readonly IPersonCredentialsApplication _personCredentialsApplication;

    public MfaApi(ICallerContextFactory callerFactory, IPersonCredentialsApplication personCredentialsApplication)
    {
        _callerFactory = callerFactory;
        _personCredentialsApplication = personCredentialsApplication;
    }

    public async
        Task<ApiPostResult<CredentialMfaAuthenticatorAssociation,
            AssociateCredentialMfaAuthenticatorForCallerResponse>> AssociateMfaAuthenticator(
            AssociateCredentialMfaAuthenticatorForCallerRequest request, CancellationToken cancellationToken)
    {
        var authenticator = await _personCredentialsApplication.AssociateMfaAuthenticatorAsync(
            _callerFactory.Create(), request.MfaToken,
            request.AuthenticatorType ?? CredentialMfaAuthenticatorType.None, request.PhoneNumber,
            cancellationToken);
        return () =>
            authenticator
                .HandleApplicationResult<CredentialMfaAuthenticatorAssociation,
                    AssociateCredentialMfaAuthenticatorForCallerResponse>(x =>
                    new PostResult<AssociateCredentialMfaAuthenticatorForCallerResponse>(
                        new AssociateCredentialMfaAuthenticatorForCallerResponse { Authenticator = x }));
    }

    public async
        Task<ApiPutPatchResult<CredentialMfaAuthenticatorChallenge,
            ChallengeCredentialMfaAuthenticatorForCallerResponse>>
        ChallengeMfaAuthenticator(
            ChallengeCredentialMfaAuthenticatorForCallerRequest request, CancellationToken cancellationToken)
    {
        var challenge =
            await _personCredentialsApplication.ChallengeMfaAuthenticatorAsync(_callerFactory.Create(),
                request.MfaToken!, request.AuthenticatorId!, cancellationToken);
        return () =>
            challenge
                .HandleApplicationResult<CredentialMfaAuthenticatorChallenge,
                    ChallengeCredentialMfaAuthenticatorForCallerResponse>(x =>
                    new ChallengeCredentialMfaAuthenticatorForCallerResponse { Challenge = x });
    }

    public async Task<ApiPutPatchResult<PersonCredential, ChangeCredentialMfaResponse>> ChangeMfa(
        ChangeCredentialMfaForCallerRequest request, CancellationToken cancellationToken)
    {
        var credential =
            await _personCredentialsApplication.ChangeMfaAsync(_callerFactory.Create(), request.IsEnabled,
                cancellationToken);
        return () =>
            credential.HandleApplicationResult<PersonCredential, ChangeCredentialMfaResponse>(x =>
                new ChangeCredentialMfaResponse { Credential = x });
    }

    public async
        Task<ApiPutPatchResult<CredentialMfaAuthenticatorConfirmation,
            ConfirmCredentialMfaAuthenticatorForCallerResponse>>
        ConfirmMfaAuthenticatorAssociation(
            ConfirmCredentialMfaAuthenticatorForCallerRequest request, CancellationToken cancellationToken)
    {
        var tokensOrAuthenticators = await _personCredentialsApplication.ConfirmMfaAuthenticatorAssociationAsync(
            _callerFactory.Create(), request.MfaToken,
            request.AuthenticatorType ?? CredentialMfaAuthenticatorType.None, request.OobCode,
            request.ConfirmationCode!, cancellationToken);

        return () =>
            tokensOrAuthenticators
                .HandleApplicationResult<CredentialMfaAuthenticatorConfirmation,
                    ConfirmCredentialMfaAuthenticatorForCallerResponse>(
                    x => new ConfirmCredentialMfaAuthenticatorForCallerResponse
                        { Tokens = x.Tokens, Authenticators = x.Authenticators });
    }

    public async Task<ApiDeleteResult> DisassociateMfaAuthenticator(
        DisassociateCredentialMfaAuthenticatorForCallerRequest request, CancellationToken cancellationToken)
    {
        var resource =
            await _personCredentialsApplication.DisassociateMfaAuthenticatorAsync(_callerFactory.Create(),
                request.Id!, cancellationToken);
        return () => resource.HandleApplicationResult();
    }

    public async
        Task<ApiGetResult<List<CredentialMfaAuthenticator>, ListCredentialMfaAuthenticatorsForCallerResponse>>
        ListMfaAuthenticators(ListCredentialMfaAuthenticatorsForCallerRequest request,
            CancellationToken cancellationToken)
    {
        var authenticators =
            await _personCredentialsApplication.ListMfaAuthenticatorsAsync(_callerFactory.Create(), request.MfaToken,
                cancellationToken);
        return () =>
            authenticators
                .HandleApplicationResult<List<CredentialMfaAuthenticator>,
                    ListCredentialMfaAuthenticatorsForCallerResponse>(x =>
                    new ListCredentialMfaAuthenticatorsForCallerResponse { Authenticators = x });
    }

    public async Task<ApiPutPatchResult<PersonCredential, ChangeCredentialMfaResponse>> ResetPasswordMfa(
        ResetCredentialMfaRequest request, CancellationToken cancellationToken)
    {
        var credential = await _personCredentialsApplication.ResetPasswordMfaAsync(_callerFactory.Create(),
            request.UserId!, cancellationToken);

        return () =>
            credential.HandleApplicationResult<PersonCredential, ChangeCredentialMfaResponse>(x =>
                new ChangeCredentialMfaResponse { Credential = x });
    }

    public async Task<ApiPutPatchResult<AuthenticateTokens, AuthenticateResponse>> VerifyMfaAuthenticator(
        VerifyCredentialMfaAuthenticatorForCallerRequest request, CancellationToken cancellationToken)
    {
        var tokens =
            await _personCredentialsApplication.VerifyMfaAuthenticatorAsync(_callerFactory.Create(),
                request.MfaToken!, request.AuthenticatorType ?? CredentialMfaAuthenticatorType.None,
                request.OobCode, request.ConfirmationCode!, cancellationToken);
        return () =>
            tokens.HandleApplicationResult<AuthenticateTokens, AuthenticateResponse>(x => new AuthenticateResponse
                { Tokens = x });
    }
}