using FluentValidation;
using Infrastructure.Web.Api.Operations.Shared.Ancillary;
using JetBrains.Annotations;

namespace WebsiteHost.Api.Recording;

[UsedImplicitly]
public class RecordCrashRequestValidator : AbstractValidator<RecordCrashRequest>
{
    public RecordCrashRequestValidator()
    {
        RuleFor(req => req.Message)
            .NotEmpty()
            .WithMessage(Resources.RecordCrashRequestValidator_InvalidMessage);
    }
}