using Application.Interfaces;
using Common.Extensions;
using Domain.Interfaces.Validations;
using FluentValidation;
using Infrastructure.Web.Api.Common.Validation;
using Infrastructure.Web.Api.Operations.Shared.BackEndForFrontEnd;

namespace WebsiteHost.Api.AuthN;

public class AuthenticateRequestValidator : AbstractValidator<AuthenticateRequest>
{
    public AuthenticateRequestValidator()
    {
        RuleFor(req => req.Provider)
            .NotEmpty()
            .WithMessage(Resources.AuthenticateRequestValidator_InvalidProvider);
        When(req => req.Provider == AuthenticationConstants.Providers.Credentials, () =>
        {
            RuleFor(req => req.Username)
                .NotEmpty()
                .IsEmailAddress()
                .WithMessage(Resources.AuthenticateRequestValidator_InvalidUsername);
            RuleFor(req => req.Password)
                .NotEmpty()
                .Matches(CommonValidations.Passwords.Password.Strict)
                .WithMessage(Resources.AuthenticateRequestValidator_InvalidPassword);
        });
        When(req => req.Provider != AuthenticationConstants.Providers.Credentials, () =>
        {
            RuleFor(req => req.AuthCode)
                .NotEmpty()
                .WithMessage(Resources.AuthenticateRequestValidator_InvalidAuthCode);
            RuleFor(req => req.Username)
                .NotEmpty()
                .IsEmailAddress()
                .When(req => req.Username.HasValue())
                .WithMessage(Resources.AuthenticateRequestValidator_InvalidUsername);
        });
    }
}