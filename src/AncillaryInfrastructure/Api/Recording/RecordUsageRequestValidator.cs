using FluentValidation;
using Infrastructure.Web.Api.Common.Validation;
using Infrastructure.Web.Api.Operations.Shared.Ancillary;
using JetBrains.Annotations;

namespace AncillaryInfrastructure.Api.Recording;

[UsedImplicitly]
public class RecordUseRequestValidator : AbstractValidator<RecordUseRequest>
{
    public RecordUseRequestValidator()
    {
        RuleFor(req => req.EventName)
            .NotEmpty()
            .WithMessage(Resources.AnyRecordingEventNameValidator_InvalidEventName);
        RuleFor(req => req.Additional)
            .SetValidator(new AdditionalValidator());
    }
}