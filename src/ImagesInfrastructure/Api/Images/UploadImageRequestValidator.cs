using Common.Extensions;
using FluentValidation;
using ImagesDomain;
using Infrastructure.Web.Api.Common.Validation;
using Infrastructure.Web.Api.Operations.Shared.Images;

namespace ImagesInfrastructure.Api.Images;

public class UploadImageRequestValidator : AbstractValidator<UploadImageRequest>
{
    public UploadImageRequestValidator()
    {
        //Note: the file itself will be validated in the application
        RuleFor(req => req.Description)
            .Matches(Validations.Images.Description)
            .WithMessage(Resources.UpdateImageRequestValidator_InvalidDescription)
            .When(req => req.Description.HasValue());
    }
}