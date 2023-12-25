using FluentValidation;
using Infrastructure.Web.Api.Operations.Shared.BackEndForFrontEnd;
using JetBrains.Annotations;

namespace WebsiteHost.Api.Recording;

[UsedImplicitly]
public class RecordPageViewRequestValidator : AbstractValidator<RecordPageViewRequest>
{
    public RecordPageViewRequestValidator()
    {
        RuleFor(req => req.Path)
            .NotEmpty()
            .WithMessage(Resources.RecordPageViewRequestValidator_InvalidPath);
    }
}