using Application.Resources.Shared;
using FluentValidation;
using Infrastructure.Web.Api.Operations.Shared.Ancillary;
using JetBrains.Annotations;

namespace WebsiteHost.Api.Recording;

[UsedImplicitly]
public class RecordTraceRequestValidator : AbstractValidator<RecordTraceRequest>
{
    public RecordTraceRequestValidator()
    {
        RuleFor(req => req.Level)
            .NotNull()
            .IsEnumName(typeof(RecorderTraceLevel), false)
            .WithMessage(Resources.RecordTraceRequestValidator_InvalidLevel);
        RuleFor(req => req.MessageTemplate)
            .NotEmpty()
            .WithMessage(Resources.RecordTraceRequestValidator_InvalidMessageTemplate);
        RuleForEach(req => req.Arguments)
            .NotNull()
            .WithMessage(Resources.RecordTraceRequestValidator_InvalidMessageArgument);
    }
}