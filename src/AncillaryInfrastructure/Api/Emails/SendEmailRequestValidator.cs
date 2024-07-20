using FluentValidation;
using Infrastructure.Web.Api.Operations.Shared.Ancillary;
using JetBrains.Annotations;

namespace AncillaryInfrastructure.Api.Emails;

[UsedImplicitly]
public class SendEmailRequestValidator : AbstractValidator<SendEmailRequest>
{
    public SendEmailRequestValidator()
    {
        RuleFor(req => req.Message)
            .NotEmpty()
            .WithMessage(Resources.AnyQueueMessageValidator_InvalidMessage);
    }
}