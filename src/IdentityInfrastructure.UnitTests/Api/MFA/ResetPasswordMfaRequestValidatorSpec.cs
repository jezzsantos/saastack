using Domain.Common.Identity;
using FluentValidation;
using IdentityInfrastructure.Api.MFA;
using Infrastructure.Web.Api.Operations.Shared.Identities;
using Xunit;

namespace IdentityInfrastructure.UnitTests.Api.MFA;

[Trait("Category", "Unit")]
public class ResetPasswordMfaRequestValidatorSpec
{
    private readonly ResetCredentialMfaRequest _dto;
    private readonly ResetPasswordMfaRequestValidator _validator;

    public ResetPasswordMfaRequestValidatorSpec()
    {
        _validator =
            new ResetPasswordMfaRequestValidator(new FixedIdentifierFactory("anid"));
        _dto = new ResetCredentialMfaRequest
        {
            UserId = "anid"
        };
    }

    [Fact]
    public void WhenAllProperties_ThenSucceeds()
    {
        _validator.ValidateAndThrow(_dto);
    }
}