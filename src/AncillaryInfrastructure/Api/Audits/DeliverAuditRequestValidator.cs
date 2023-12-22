using FluentValidation;
using Infrastructure.Web.Api.Operations.Shared.Ancillary;
using JetBrains.Annotations;

namespace AncillaryInfrastructure.Api.Audits;

[UsedImplicitly]
public class DeliverAuditRequestValidator : AbstractValidator<DeliverAuditRequest>
{
    public DeliverAuditRequestValidator()
    {
        RuleFor(req => req.Message)
            .NotEmpty()
            .WithMessage(Resources.AnyMessageValidator_InvalidMessage);
    }
}