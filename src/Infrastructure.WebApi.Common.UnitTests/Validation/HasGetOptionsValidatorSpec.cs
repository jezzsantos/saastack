using FluentAssertions;
using FluentValidation;
using Infrastructure.WebApi.Common.Validation;
using Infrastructure.WebApi.Interfaces;
using UnitTesting.Common.Validation;
using Xunit;

namespace Infrastructure.WebApi.Common.UnitTests.Validation;

[Trait("Category", "Unit")]
public class HasGetOptionsValidatorSpec
{
    private readonly HasGetOptionsDto _dto;
    private readonly HasGetOptionsValidator _validator;

    public HasGetOptionsValidatorSpec()
    {
        _validator = new HasGetOptionsValidator();
        _dto = new HasGetOptionsDto();
    }

    [Fact]
    public void WhenAllPropertiesValid_ThenSucceeds()
    {
        _validator.ValidateAndThrow(_dto);
    }

    [Fact]
    public void WhenEmbedIsNull_ThenSucceeds()
    {
        _dto.Embed = null;

        _validator.ValidateAndThrow(_dto);
    }

    [Fact]
    public void WhenEmbedIsOff_ThenSucceeds()
    {
        _dto.Embed = HasGetOptions.EmbedNone;

        _validator.ValidateAndThrow(_dto);
    }

    [Fact]
    public void WhenEmbedIsTopLevelField_ThenSucceeds()
    {
        _dto.Embed = "aresourceref";

        _validator.ValidateAndThrow(_dto);
    }

    [Fact]
    public void WhenEmbedIsChildLevelField_ThenSucceeds()
    {
        _dto.Embed = "aresourceref.achildresourceref";

        _validator.ValidateAndThrow(_dto);
    }

    [Fact]
    public void WhenEmbedIsGrandChildLevelField_ThenSucceeds()
    {
        _dto.Embed = "aresourceref.achildresourceref.agrandchildresourceref";

        _validator.ValidateAndThrow(_dto);
    }

    [Fact]
    public void WhenEmbedIsInvalidResourceReference_ThenThrows()
    {
        _dto.Embed = "^aresourceref";

        _validator.Invoking(x => x.ValidateAndThrow(_dto)).Should().Throw<ValidationException>()
            .WithMessageLike(ValidationResources.HasGetOptionsValidator_InvalidEmbed);
    }

    [Fact]
    public void WhenEmbedIsInvalidChildResourceReference_ThenThrows()
    {
        _dto.Embed = "aresourceref.^achildresourceref";

        _validator.Invoking(x => x.ValidateAndThrow(_dto)).Should().Throw<ValidationException>()
            .WithMessageLike(ValidationResources.HasGetOptionsValidator_InvalidEmbed);
    }

    [Fact]
    public void WhenEmbedIsInvalidGrandChildResourceReference_ThenThrows()
    {
        _dto.Embed = "aresourceref.achildresourceref.^agrandchildresourceref";

        _validator.Invoking(x => x.ValidateAndThrow(_dto)).Should().Throw<ValidationException>()
            .WithMessageLike(ValidationResources.HasGetOptionsValidator_InvalidEmbed);
    }

    [Fact]
    public void WhenEmbedContainsTooManyResources_ThenThrows()
    {
        _dto.Embed =
            "aresourceref1,aresourceref2,aresourceref3,aresourceref4,aresourceref5,aresourceref6,aresourceref7,aresourceref8,aresourceref9,aresourceref10,aresourceref11";

        _validator.Invoking(x => x.ValidateAndThrow(_dto)).Should().Throw<ValidationException>()
            .WithMessageLike(ValidationResources.HasGetOptionsValidator_TooManyResourceReferences);
    }
}

internal class HasGetOptionsDto : IHasGetOptions
{
    public string? Embed { get; set; }
}