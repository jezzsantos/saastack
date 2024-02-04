using Domain.Interfaces.Validations;
using FluentValidation;
using Infrastructure.Interfaces;
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
        RuleFor(req => req.Username)
            .NotEmpty()
            .IsEmailAddress()
            .When(req => req.Provider == AuthenticationConstants.Providers.Credentials)
            .WithMessage(Resources.AuthenticateRequestValidator_InvalidUsername);
        RuleFor(req => req.Password)
            .NotEmpty()
            .Matches(CommonValidations.Passwords.Password.Strict)
            .When(req => req.Provider == AuthenticationConstants.Providers.Credentials)
            .WithMessage(Resources.AuthenticateRequestValidator_InvalidPassword);
        RuleFor(req => req.AuthCode)
            .NotEmpty()
            .When(req => req.Provider != AuthenticationConstants.Providers.Credentials)
            .WithMessage(Resources.AuthenticateRequestValidator_InvalidAuthCode);
    }
}