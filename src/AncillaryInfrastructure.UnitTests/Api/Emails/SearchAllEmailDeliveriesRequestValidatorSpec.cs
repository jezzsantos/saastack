using AncillaryInfrastructure.Api.Emails;
using Common.Extensions;
using Domain.Common.ValueObjects;
using FluentAssertions;
using FluentValidation;
using Infrastructure.Web.Api.Operations.Shared.Ancillary;
using UnitTesting.Common.Validation;
using Xunit;

namespace AncillaryInfrastructure.UnitTests.Api.Emails;

[Trait("Category", "Unit")]
public class SearchAllEmailDeliveriesRequestValidatorSpec
{
    private readonly SearchAllEmailDeliveriesRequest _dto;
    private readonly SearchAllEmailDeliveriesRequestValidator _validator;

    public SearchAllEmailDeliveriesRequestValidatorSpec()
    {
        _validator = new SearchAllEmailDeliveriesRequestValidator("anid".ToIdentifierFactory());
        _dto = new SearchAllEmailDeliveriesRequest();
    }

    [Fact]
    public void WhenAllProperties_ThenSucceeds()
    {
        _validator.ValidateAndThrow(_dto);
    }

    [Fact]
    public void WhenOrganizationIdExists_ThenSucceeds()
    {
        _dto.OrganizationId = "anid";
        _validator.ValidateAndThrow(_dto);
    }
    
    [Fact]
    public void WhenSinceUtcIsTooFuture_ThenThrows()
    {
        _dto.SinceUtc = DateTime.UtcNow.AddHours(2);

        _validator.Invoking(x => x.ValidateAndThrow(_dto))
            .Should().Throw<ValidationException>()
            .WithMessageLike(Resources.SearchEmailDeliveriesRequestValidator_SinceUtc_TooFuture);
    }

    [Fact]
    public void WhenSinceUtcIsPast_ThenSucceeds()
    {
        _dto.SinceUtc = DateTime.UtcNow.SubtractSeconds(1);

        _validator.ValidateAndThrow(_dto);
    }

    [Fact]
    public void WhenTagIsInvalid_ThenThrows()
    {
        _dto.Tags = ["atag1,^aninvalidtag^"];

        _validator.Invoking(x => x.ValidateAndThrow(_dto))
            .Should().Throw<ValidationException>()
            .WithMessageLike(Resources.SearchEmailDeliveriesRequestValidator_InvalidTag);
    }

    [Fact]
    public void WhenTagIsValid_ThenSucceeds()
    {
        _dto.Tags = ["atag1"];

        _validator.ValidateAndThrow(_dto);
    }
}