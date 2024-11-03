using Domain.Common.Identity;
using FluentValidation;
using IdentityInfrastructure.Api.MFA;
using Infrastructure.Web.Api.Operations.Shared.Identities;
using Xunit;

namespace IdentityInfrastructure.UnitTests.Api.MFA;

[Trait("Category", "Unit")]
public class DisassociatePasswordMfaAuthenticatorForCallerRequestValidatorSpec
{
    private readonly DisassociatePasswordMfaAuthenticatorForCallerRequest _dto;
    private readonly DisassociatePasswordMfaAuthenticatorForCallerRequestValidator _validator;

    public DisassociatePasswordMfaAuthenticatorForCallerRequestValidatorSpec()
    {
        _validator =
            new DisassociatePasswordMfaAuthenticatorForCallerRequestValidator(new FixedIdentifierFactory("anid"));
        _dto = new DisassociatePasswordMfaAuthenticatorForCallerRequest
        {
            Id = "anid"
        };
    }

    [Fact]
    public void WhenAllProperties_ThenSucceeds()
    {
        _validator.ValidateAndThrow(_dto);
    }
}