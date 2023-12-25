using FluentValidation;
using Infrastructure.Web.Api.Common.Validation;
using Infrastructure.Web.Api.Operations.Shared.Ancillary;
using JetBrains.Annotations;

namespace WebsiteHost.Api.Recording;

[UsedImplicitly]
public class RecordMeasureRequestValidator : AbstractValidator<RecordMeasureRequest>
{
    public RecordMeasureRequestValidator()
    {
        RuleFor(req => req.EventName)
            .NotEmpty()
            .WithMessage(Resources.AnyRecordingEventNameValidator_InvalidEventName);
        RuleFor(req => req.Additional)
            .SetValidator(new AdditionalValidator());
    }
}