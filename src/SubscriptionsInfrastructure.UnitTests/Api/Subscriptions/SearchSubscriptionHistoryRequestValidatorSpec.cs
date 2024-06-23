using Common.Extensions;
using Domain.Common.Identity;
using FluentAssertions;
using FluentValidation;
using Infrastructure.Web.Api.Common.Validation;
using Infrastructure.Web.Api.Operations.Shared.Subscriptions;
using SubscriptionsDomain;
using SubscriptionsInfrastructure.Api.Subscriptions;
using UnitTesting.Common.Validation;
using Xunit;

namespace SubscriptionsInfrastructure.UnitTests.Api.Subscriptions;

[Trait("Category", "Unit")]
public class SearchSubscriptionHistoryRequestValidatorSpec
{
    private readonly SearchSubscriptionHistoryRequest _dto;
    private readonly SearchSubscriptionHistoryRequestValidator _validator;

    public SearchSubscriptionHistoryRequestValidatorSpec()
    {
        _validator = new SearchSubscriptionHistoryRequestValidator(
            new HasSearchOptionsValidator(new HasGetOptionsValidator()), new FixedIdentifierFactory("anid"));
        _dto = new SearchSubscriptionHistoryRequest
        {
            Id = "anid"
        };
    }

    [Fact]
    public void WhenAllProperties_ThenSucceeds()
    {
        _validator.ValidateAndThrow(_dto);
    }

    [Fact]
    public void WhenFromUtcIsTooFarInPast_ThenThrows()
    {
        _dto.FromUtc = DateTime.UtcNow.AddYears(-2);

        _validator
            .Invoking(x => x.ValidateAndThrow(_dto))
            .Should().Throw<ValidationException>()
            .WithMessageLike(
                Resources.SearchSubscriptionHistoryRequestValidator_FromUtc_TooPast.Format(Validations.Subscription
                    .MinInvoiceDate));
    }

    [Fact]
    public void WhenFromUtcIsTooFarInFuture_ThenThrows()
    {
        _dto.FromUtc = DateTime.UtcNow.AddYears(2);

        _validator
            .Invoking(x => x.ValidateAndThrow(_dto))
            .Should().Throw<ValidationException>()
            .WithMessageLike(
                Resources.SearchSubscriptionHistoryRequestValidator_FromUtc_TooFuture.Format(Validations.Subscription
                    .MaxInvoiceDate));
    }

    [Fact]
    public void WhenFromUtcIsAfterToUtc_ThenThrows()
    {
        _dto.FromUtc = DateTime.UtcNow.AddHours(2);
        _dto.ToUtc = DateTime.UtcNow.AddHours(1);

        _validator
            .Invoking(x => x.ValidateAndThrow(_dto))
            .Should().Throw<ValidationException>()
            .WithMessageLike(Resources.SearchSubscriptionHistoryRequestValidator_FromUtc_StartAfterEnd);
    }

    [Fact]
    public void WhenToUtcIsTooFarInPast_ThenThrows()
    {
        _dto.ToUtc = DateTime.UtcNow.AddYears(-2);

        _validator
            .Invoking(x => x.ValidateAndThrow(_dto))
            .Should().Throw<ValidationException>()
            .WithMessageLike(
                Resources.SearchSubscriptionHistoryRequestValidator_ToUtc_TooPast.Format(Validations.Subscription
                    .MinInvoiceDate));
    }

    [Fact]
    public void WhenToUtcIsTooFarInFuture_ThenThrows()
    {
        _dto.ToUtc = DateTime.UtcNow.AddYears(2);

        _validator
            .Invoking(x => x.ValidateAndThrow(_dto))
            .Should().Throw<ValidationException>()
            .WithMessageLike(
                Resources.SearchSubscriptionHistoryRequestValidator_ToUtc_TooFuture.Format(Validations.Subscription
                    .MaxInvoiceDate));
    }

    [Fact]
    public void WhenToUtcIsBeforeFromUtc_ThenThrows()
    {
        _dto.ToUtc = DateTime.UtcNow.AddHours(-2);
        _dto.FromUtc = DateTime.UtcNow.AddHours(-1);

        _validator
            .Invoking(x => x.ValidateAndThrow(_dto))
            .Should().Throw<ValidationException>()
            .WithMessageLike(Resources.SearchSubscriptionHistoryRequestValidator_ToUtc_EndBeforeStart);
    }

    [Fact]
    public void WhenFromUtcAndToUtcAreWithinLimits_ThenSucceeds()
    {
        _dto.FromUtc = DateTime.UtcNow.AddHours(1);
        _dto.ToUtc = DateTime.UtcNow.AddHours(2);

        _validator.ValidateAndThrow(_dto);
    }
}