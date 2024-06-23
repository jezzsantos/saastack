using Domain.Common.Identity;
using FluentAssertions;
using FluentValidation;
using Infrastructure.Web.Api.Operations.Shared.Subscriptions;
using SubscriptionsInfrastructure.Api.Subscriptions;
using UnitTesting.Common.Validation;
using Xunit;

namespace SubscriptionsInfrastructure.UnitTests.Api.Subscriptions;

[Trait("Category", "Unit")]
public class MigrateSubscriptionRequestValidatorSpec
{
    private readonly MigrateSubscriptionRequest _dto;
    private readonly MigrateSubscriptionRequestValidator _validator;

    public MigrateSubscriptionRequestValidatorSpec()
    {
        _validator = new MigrateSubscriptionRequestValidator(new FixedIdentifierFactory("anid"));
        _dto = new MigrateSubscriptionRequest
        {
            Id = "anid",
            ProviderName = "aprovidername",
            ProviderState = new Dictionary<string, string>
            {
                { "aname", "avalue" }
            }
        };
    }

    [Fact]
    public void WhenAllProperties_ThenSucceeds()
    {
        _validator.ValidateAndThrow(_dto);
    }

    [Fact]
    public void WhenProviderNameIsEmpty_ThenThrows()
    {
        _dto.ProviderName = string.Empty;

        _validator
            .Invoking(x => x.ValidateAndThrow(_dto))
            .Should().Throw<ValidationException>()
            .WithMessageLike(Resources.MigrateSubscriptionRequestValidator_InvalidProviderName);
    }

    [Fact]
    public void WhenProviderNameIsInvalid_ThenThrows()
    {
        _dto.ProviderName = "^aninvalidplanid";

        _validator
            .Invoking(x => x.ValidateAndThrow(_dto))
            .Should().Throw<ValidationException>()
            .WithMessageLike(Resources.MigrateSubscriptionRequestValidator_InvalidProviderName);
    }

    [Fact]
    public void WhenProviderStateIsEmpty_ThenThrows()
    {
        _dto.ProviderState = new Dictionary<string, string>();

        _validator
            .Invoking(x => x.ValidateAndThrow(_dto))
            .Should().Throw<ValidationException>()
            .WithMessageLike(Resources.MigrateSubscriptionRequestValidator_InvalidProviderState);
    }
}