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
    private readonly IPasswordCredentialsApplication _passwordCredentialsApplication;

    public MfaApi(ICallerContextFactory callerFactory, IPasswordCredentialsApplication passwordCredentialsApplication)
    {
        _callerFactory = callerFactory;
        _passwordCredentialsApplication = passwordCredentialsApplication;
    }

    public async
        Task<ApiPostResult<PasswordCredentialMfaAuthenticatorAssociation,
            AssociatePasswordMfaAuthenticatorForCallerResponse>> AssociateMfaAuthenticator(
            AssociatePasswordMfaAuthenticatorForCallerRequest request, CancellationToken cancellationToken)
    {
        var authenticator = await _passwordCredentialsApplication.AssociateMfaAuthenticatorAsync(
            _callerFactory.Create(), request.MfaToken,
            request.AuthenticatorType ?? PasswordCredentialMfaAuthenticatorType.None, request.PhoneNumber,
            cancellationToken);
        return () =>
            authenticator
                .HandleApplicationResult<PasswordCredentialMfaAuthenticatorAssociation,
                    AssociatePasswordMfaAuthenticatorForCallerResponse>(x =>
                    new PostResult<AssociatePasswordMfaAuthenticatorForCallerResponse>(
                        new AssociatePasswordMfaAuthenticatorForCallerResponse { Authenticator = x }));
    }

    public async
        Task<ApiPutPatchResult<PasswordCredentialMfaAuthenticatorChallenge,
            ChallengePasswordMfaAuthenticatorForCallerResponse>>
        ChallengeMfaAuthenticator(
            ChallengePasswordMfaAuthenticatorForCallerRequest request, CancellationToken cancellationToken)
    {
        var challenge =
            await _passwordCredentialsApplication.ChallengeMfaAuthenticatorAsync(_callerFactory.Create(),
                request.MfaToken!, request.AuthenticatorId!, cancellationToken);
        return () =>
            challenge
                .HandleApplicationResult<PasswordCredentialMfaAuthenticatorChallenge,
                    ChallengePasswordMfaAuthenticatorForCallerResponse>(x =>
                    new ChallengePasswordMfaAuthenticatorForCallerResponse { Challenge = x });
    }

    public async Task<ApiPutPatchResult<PasswordCredential, ChangePasswordMfaResponse>> ChangeMfa(
        ChangePasswordMfaForCallerRequest request, CancellationToken cancellationToken)
    {
        var credential =
            await _passwordCredentialsApplication.ChangeMfaAsync(_callerFactory.Create(), request.IsEnabled,
                cancellationToken);
        return () =>
            credential.HandleApplicationResult<PasswordCredential, ChangePasswordMfaResponse>(x =>
                new ChangePasswordMfaResponse { Credential = x });
    }

    public async
        Task<ApiPutPatchResult<PasswordCredentialMfaAuthenticatorConfirmation,
            ConfirmPasswordMfaAuthenticatorForCallerResponse>>
        ConfirmMfaAuthenticatorAssociation(
            ConfirmPasswordMfaAuthenticatorForCallerRequest request, CancellationToken cancellationToken)
    {
        var tokensOrAuthenticators = await _passwordCredentialsApplication.ConfirmMfaAuthenticatorAssociationAsync(
            _callerFactory.Create(), request.MfaToken,
            request.AuthenticatorType ?? PasswordCredentialMfaAuthenticatorType.None, request.OobCode,
            request.ConfirmationCode!, cancellationToken);

        return () =>
            tokensOrAuthenticators
                .HandleApplicationResult<PasswordCredentialMfaAuthenticatorConfirmation,
                    ConfirmPasswordMfaAuthenticatorForCallerResponse>(
                    x => new ConfirmPasswordMfaAuthenticatorForCallerResponse
                        { Tokens = x.Tokens, Authenticators = x.Authenticators });
    }

    public async Task<ApiDeleteResult> DisassociateMfaAuthenticator(
        DisassociatePasswordMfaAuthenticatorForCallerRequest request, CancellationToken cancellationToken)
    {
        var resource =
            await _passwordCredentialsApplication.DisassociateMfaAuthenticatorAsync(_callerFactory.Create(),
                request.Id!, cancellationToken);
        return () => resource.HandleApplicationResult();
    }

    public async
        Task<ApiGetResult<List<PasswordCredentialMfaAuthenticator>, ListPasswordMfaAuthenticatorsForCallerResponse>>
        ListMfaAuthenticators(ListPasswordMfaAuthenticatorsForCallerRequest request,
            CancellationToken cancellationToken)
    {
        var authenticators =
            await _passwordCredentialsApplication.ListMfaAuthenticatorsAsync(_callerFactory.Create(), request.MfaToken,
                cancellationToken);
        return () =>
            authenticators
                .HandleApplicationResult<List<PasswordCredentialMfaAuthenticator>,
                    ListPasswordMfaAuthenticatorsForCallerResponse>(x =>
                    new ListPasswordMfaAuthenticatorsForCallerResponse { Authenticators = x });
    }

    public async Task<ApiPutPatchResult<PasswordCredential, ChangePasswordMfaResponse>> ResetPasswordMfa(
        ResetPasswordMfaRequest request, CancellationToken cancellationToken)
    {
        var credential = await _passwordCredentialsApplication.ResetPasswordMfaAsync(_callerFactory.Create(),
            request.UserId!, cancellationToken);

        return () =>
            credential.HandleApplicationResult<PasswordCredential, ChangePasswordMfaResponse>(x =>
                new ChangePasswordMfaResponse { Credential = x });
    }

    public async Task<ApiPutPatchResult<AuthenticateTokens, AuthenticateResponse>> VerifyMfaAuthenticator(
        VerifyPasswordMfaAuthenticatorForCallerRequest request, CancellationToken cancellationToken)
    {
        var tokens =
            await _passwordCredentialsApplication.VerifyMfaAuthenticatorAsync(_callerFactory.Create(),
                request.MfaToken!, request.AuthenticatorType ?? PasswordCredentialMfaAuthenticatorType.None,
                request.OobCode, request.ConfirmationCode!, cancellationToken);
        return () =>
            tokens.HandleApplicationResult<AuthenticateTokens, AuthenticateResponse>(x => new AuthenticateResponse
                { Tokens = x });
    }
}