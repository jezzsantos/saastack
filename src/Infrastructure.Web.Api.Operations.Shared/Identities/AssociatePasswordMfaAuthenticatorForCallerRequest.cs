using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using Application.Resources.Shared;
using Infrastructure.Web.Api.Interfaces;

namespace Infrastructure.Web.Api.Operations.Shared.Identities;

/// <summary>
///     Associates another MFA authentication factor to the user
/// </summary>
/// <response code="405">
///     The user is already associated to this <see cref="AuthenticatorType" />.You must make the
///     challenge using the existing association
/// </response>
/// <remarks>
///     This API can be called Anonymously (during password authentication), as well as after being authenticated
/// </remarks>
[Route("/passwords/mfa/authenticators", OperationMethod.Post)]
public class AssociatePasswordMfaAuthenticatorForCallerRequest : UnTenantedRequest<
    AssociatePasswordMfaAuthenticatorForCallerRequest, AssociatePasswordMfaAuthenticatorForCallerResponse>
{
    [JsonConverter(typeof(JsonStringEnumConverter))]
    [Required]
    public PasswordCredentialMfaAuthenticatorType? AuthenticatorType { get; set; }

    public string? MfaToken { get; set; }

    public string? PhoneNumber { get; set; }
}