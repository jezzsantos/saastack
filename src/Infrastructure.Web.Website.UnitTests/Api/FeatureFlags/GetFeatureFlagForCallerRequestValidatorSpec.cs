using FluentAssertions;
using FluentValidation;
using Infrastructure.Web.Api.Operations.Shared.BackEndForFrontEnd;
using UnitTesting.Common.Validation;
using WebsiteHost;
using WebsiteHost.Api.FeatureFlags;
using Xunit;

namespace Infrastructure.Web.Website.UnitTests.Api.FeatureFlags;

[Trait("Category", "Unit")]
public class GetFeatureFlagForCallerRequestValidatorSpec
{
    private readonly GetFeatureFlagForCallerRequest _dto;
    private readonly GetFeatureFlagForCallerRequestValidator _validator;

    public GetFeatureFlagForCallerRequestValidatorSpec()
    {
        _validator = new GetFeatureFlagForCallerRequestValidator();
        _dto = new GetFeatureFlagForCallerRequest
        {
            Name = "aname"
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
            .WithMessageLike(Resources.GetFeatureFlagForCallerRequestValidator_InvalidName);
    }
}