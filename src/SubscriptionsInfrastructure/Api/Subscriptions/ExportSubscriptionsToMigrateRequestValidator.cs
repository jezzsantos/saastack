using FluentValidation;
using Infrastructure.Web.Api.Common.Validation;
using Infrastructure.Web.Api.Operations.Shared.Subscriptions;

namespace SubscriptionsInfrastructure.Api.Subscriptions;

public class ExportSubscriptionsToMigrateRequestValidator : AbstractValidator<ExportSubscriptionsToMigrateRequest>
{
    public ExportSubscriptionsToMigrateRequestValidator(IHasSearchOptionsValidator hasSearchOptionsValidator)
    {
        Include(hasSearchOptionsValidator);
    }
}