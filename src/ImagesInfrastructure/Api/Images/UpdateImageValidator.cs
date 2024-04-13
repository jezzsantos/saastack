using Common.Extensions;
using Domain.Common.Identity;
using Domain.Interfaces.Validations;
using FluentValidation;
using ImagesDomain;
using Infrastructure.Web.Api.Common.Validation;
using Infrastructure.Web.Api.Operations.Shared.Images;

namespace ImagesInfrastructure.Api.Images;

public class UpdateImageRequestValidator : AbstractValidator<UpdateImageRequest>
{
    public UpdateImageRequestValidator(IIdentifierFactory identifierFactory)
    {
        RuleFor(req => req.Id)
            .IsEntityId(identifierFactory)
            .WithMessage(CommonValidationResources.AnyValidator_InvalidId);
        RuleFor(req => req.Description)
            .Matches(Validations.Images.Description)
            .WithMessage(Resources.UpdateImageRequestValidator_InvalidDescription)
            .When(req => req.Description.HasValue());
    }
}