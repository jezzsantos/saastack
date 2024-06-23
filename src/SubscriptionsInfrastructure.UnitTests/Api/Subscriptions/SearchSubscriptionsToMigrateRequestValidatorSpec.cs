using FluentValidation;
using Infrastructure.Web.Api.Common.Validation;
using Infrastructure.Web.Api.Operations.Shared.Subscriptions;
using SubscriptionsInfrastructure.Api.Subscriptions;
using Xunit;

namespace SubscriptionsInfrastructure.UnitTests.Api.Subscriptions;

[Trait("Category", "Unit")]
public class ExportSubscriptionsToMigrateRequestValidatorSpec
{
    private readonly ExportSubscriptionsToMigrateRequest _dto;
    private readonly ExportSubscriptionsToMigrateRequestValidator _validator;

    public ExportSubscriptionsToMigrateRequestValidatorSpec()
    {
        _validator = new ExportSubscriptionsToMigrateRequestValidator(
            new HasSearchOptionsValidator(new HasGetOptionsValidator()));
        _dto = new ExportSubscriptionsToMigrateRequest();
    }

    [Fact]
    public void WhenAllProperties_ThenSucceeds()
    {
        _validator.ValidateAndThrow(_dto);
    }
}