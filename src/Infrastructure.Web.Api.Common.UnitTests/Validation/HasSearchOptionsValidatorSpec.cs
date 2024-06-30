using Application.Interfaces;
using FluentAssertions;
using FluentValidation;
using Infrastructure.Web.Api.Common.Validation;
using Infrastructure.Web.Api.Interfaces;
using UnitTesting.Common.Validation;
using Xunit;

namespace Infrastructure.Web.Api.Common.UnitTests.Validation;

[Trait("Category", "Unit")]
public class HasSearchOptionsValidatorSpec
{
    private readonly HasSearchOptionsDto _dto;
    private readonly HasSearchOptionsValidator _validator;

    public HasSearchOptionsValidatorSpec()
    {
        _validator = new HasSearchOptionsValidator(new HasGetOptionsValidator());
        _dto = new HasSearchOptionsDto
        {
            Limit = 0,
            Offset = 0,
            Sort = "+afield",
            Filter = "afield1;afield2"
        };
    }

    [Fact]
    public void WhenAllPropertiesValid_ThenSucceeds()
    {
        _validator.ValidateAndThrow(_dto);
    }

    [Fact]
    public void WhenLimitIsNull_ThenSucceeds()
    {
        _dto.Limit = null;

        _validator.ValidateAndThrow(_dto);
    }

    [Fact]
    public void WhenLimitIsMin_ThenSucceeds()
    {
        _dto.Limit = SearchOptions.NoLimit;

        _validator.ValidateAndThrow(_dto);
    }

    [Fact]
    public void WhenLimitIsLessThanMax_ThenSucceeds()
    {
        _dto.Limit = SearchOptions.MaxLimit - 1;

        _validator.ValidateAndThrow(_dto);
    }

    [Fact]
    public void WhenLimitIsLessThanMin_ThenThrows()
    {
        _dto.Limit = SearchOptions.NoLimit - 1;

        _validator.Invoking(x => x.ValidateAndThrow(_dto))
            .Should().Throw<ValidationException>()
            .WithMessageLike(ValidationResources.HasSearchOptionsValidator_InvalidLimit);
    }

    [Fact]
    public void WhenLimitIsGreaterThanMax_ThenThrows()
    {
        _dto.Limit = SearchOptions.MaxLimit + 1;

        _validator.Invoking(x => x.ValidateAndThrow(_dto))
            .Should().Throw<ValidationException>()
            .WithMessageLike(ValidationResources.HasSearchOptionsValidator_InvalidLimit);
    }

    [Fact]
    public void WhenOffsetIsNull_ThenSucceeds()
    {
        _dto.Offset = null;

        _validator.ValidateAndThrow(_dto);
    }

    [Fact]
    public void WhenOffsetIsMin_ThenSucceeds()
    {
        _dto.Offset = SearchOptions.NoOffset;

        _validator.ValidateAndThrow(_dto);
    }

    [Fact]
    public void WhenOffsetIsLessThanMax_ThenSucceeds()
    {
        _dto.Offset = SearchOptions.MaxLimit - 1;

        _validator.ValidateAndThrow(_dto);
    }

    [Fact]
    public void WhenOffsetIsLessThanMin_ThenThrows()
    {
        _dto.Offset = SearchOptions.NoOffset - 1;

        _validator.Invoking(x => x.ValidateAndThrow(_dto))
            .Should().Throw<ValidationException>()
            .WithMessageLike(ValidationResources.HasSearchOptionsValidator_InvalidOffset);
    }

    [Fact]
    public void WhenOffsetIsGreaterThanMax_ThenThrows()
    {
        _dto.Offset = SearchOptions.MaxLimit + 1;

        _validator.Invoking(x => x.ValidateAndThrow(_dto))
            .Should().Throw<ValidationException>()
            .WithMessageLike(ValidationResources.HasSearchOptionsValidator_InvalidOffset);
    }

    [Fact]
    public void WhenSortIsNull_ThenSucceeds()
    {
        _dto.Sort = null;

        _validator.ValidateAndThrow(_dto);
    }

    [Fact]
    public void WhenSortIsInvalid_ThenThrows()
    {
        _dto.Sort = "*";

        _validator.Invoking(x => x.ValidateAndThrow(_dto))
            .Should().Throw<ValidationException>()
            .WithMessageLike(ValidationResources.HasSearchOptionsValidator_InvalidSort);
    }

    [Fact]
    public void WhenFilterIsNull_ThenSucceeds()
    {
        _dto.Filter = null;

        _validator.ValidateAndThrow(_dto);
    }

    [Fact]
    public void WhenFilterIsInvalid_ThenThrows()
    {
        _dto.Filter = "*";

        _validator.Invoking(x => x.ValidateAndThrow(_dto))
            .Should().Throw<ValidationException>()
            .WithMessageLike(ValidationResources.HasSearchOptionsValidator_InvalidFilter);
    }
}

public class HasSearchOptionsDto : IHasSearchOptions
{
    public string? Embed { get; set; }

    public string? Filter { get; set; }

    public int? Limit { get; set; }

    public int? Offset { get; set; }

    public string? Sort { get; set; }
}