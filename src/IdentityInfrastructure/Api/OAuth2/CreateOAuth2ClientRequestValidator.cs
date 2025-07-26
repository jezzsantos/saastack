using Common.Extensions;
using FluentValidation;
using IdentityDomain;
using Infrastructure.Web.Api.Common.Validation;
using Infrastructure.Web.Api.Operations.Shared.Identities;

namespace IdentityInfrastructure.Api.OAuth2;

public class CreateOAuth2ClientRequestValidator : AbstractValidator<CreateOAuth2ClientRequest>
{
    public CreateOAuth2ClientRequestValidator()
    {
        RuleFor(req => req.Name)
            .NotEmpty()
            .Matches(Validations.Credentials.Person.Name)
            .WithMessage(Resources.CreateOAuth2ClientRequestValidator_InvalidName);

        RuleFor(req => req.RedirectUri)
            .IsUrl()
            .WithMessage(Resources.CreateOAuth2ClientRequestValidator_InvalidRedirectUri)
            .When(req => req.RedirectUri.HasValue());
    }
}