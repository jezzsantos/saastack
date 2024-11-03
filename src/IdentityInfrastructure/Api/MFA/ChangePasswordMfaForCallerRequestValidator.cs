using FluentValidation;
using Infrastructure.Web.Api.Operations.Shared.Identities;

namespace IdentityInfrastructure.Api.MFA;

public class ChangePasswordMfaForCallerRequestValidator : AbstractValidator<ChangePasswordMfaForCallerRequest>
{
}