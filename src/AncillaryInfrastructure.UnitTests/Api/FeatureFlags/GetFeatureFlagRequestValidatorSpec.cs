using AncillaryInfrastructure.Api.FeatureFlags;
using Domain.Common.Identity;
using Domain.Common.ValueObjects;
using FluentAssertions;
using FluentValidation;
using Infrastructure.Web.Api.Operations.Shared.Ancillary;
using Moq;
using UnitTesting.Common.Validation;
using Xunit;

namespace AncillaryInfrastructure.UnitTests.Api.FeatureFlags;

[Trait("Category", "Unit")]
public class GetFeatureFlagRequestValidatorSpec
{
    private readonly GetFeatureFlagRequest _dto;
    private readonly Mock<IIdentifierFactory> _idFactory;
    private readonly GetFeatureFlagRequestValidator _validator;

    public GetFeatureFlagRequestValidatorSpec()
    {
        _idFactory = new Mock<IIdentifierFactory>();
        _idFactory.Setup(idf => idf.IsValid(It.IsAny<Identifier>()))
            .Returns(true);
        _validator = new GetFeatureFlagRequestValidator(_idFactory.Object);
        _dto = new GetFeatureFlagRequest
        {
            Name = "aname",
            UserId = "auserid"
        };
    }

    [Fact]
    public void WhenAllProperties_ThenSucceeds()
    {
        _validator.ValidateAndThrow(_dto);
    }

    [Fact]
    public void WhenNameIsEmpty_ThenThrows()
    {
        _dto.Name = string.Empty;

        _validator.Invoking(x => x.ValidateAndThrow(_dto))
            .Should().Throw<ValidationException>()
            .WithMessageLike(Resources.GetFeatureFlagRequestValidator_InvalidName);
    }

    [Fact]
    public void WhenTenantIdIsEmpty_ThenSucceeds()
    {
        _dto.TenantId = string.Empty;

        _validator.ValidateAndThrow(_dto);
    }

    [Fact]
    public void WhenUserIdIsEmpty_ThenThrows()
    {
        _dto.UserId = string.Empty;

        _validator.Invoking(x => x.ValidateAndThrow(_dto))
            .Should().Throw<ValidationException>()
            .WithMessageLike(Resources.GetFeatureFlagRequestValidator_InvalidUserId);
    }

    [Fact]
    public void WhenTenantIdIsNotValid_ThenThrows()
    {
        _idFactory.Setup(idf => idf.IsValid("notavalidid".ToId()))
            .Returns(false);
        _dto.TenantId = "notavalidid";

        _validator.Invoking(x => x.ValidateAndThrow(_dto))
            .Should().Throw<ValidationException>()
            .WithMessageLike(Resources.GetFeatureFlagRequestValidator_InvalidTenantId);
    }

    [Fact]
    public void WhenUserIdIsNotValid_ThenThrows()
    {
        _idFactory.Setup(idf => idf.IsValid("notavalidid".ToId()))
            .Returns(false);
        _dto.UserId = "notavalidid";

        _validator.Invoking(x => x.ValidateAndThrow(_dto))
            .Should().Throw<ValidationException>()
            .WithMessageLike(Resources.GetFeatureFlagRequestValidator_InvalidUserId);
    }
}