using FluentValidation;
using Infrastructure.Web.Api.Operations.Shared.Signings;

namespace SigningsInfrastructure.Api.Signings;

public class CreateDraftSigningRequestRequestValidator : AbstractValidator<CreateDraftSigningRequestRequest>
{
    public CreateDraftSigningRequestRequestValidator()
    {
        RuleFor(req => req.Signees)
            .NotEmpty()
            .WithMessage(Resources.SigneeValidator_EmptySignees);
        RuleForEach(item => item.Signees)
            .SetValidator(new SigneeValidator());
    }
}