using Application.Resources.Shared;
using Domain.Interfaces.Validations;
using FluentValidation;
using Infrastructure.Web.Api.Common.Validation;

namespace SigningsInfrastructure.Api.Signings;

internal class SigneeValidator : AbstractValidator<Signee>
{
    public SigneeValidator()
    {
        RuleFor(req => req.EmailAddress)
            .IsEmailAddress()
            .WithMessage(Resources.SigneeValidator_InvalidEmailAddress);
        RuleFor(req => req.PhoneNumber)
            .Matches(CommonValidations.PhoneNumber)
            .WithMessage(Resources.SigneeValidator_InvalidPhoneNumber);
    }
}