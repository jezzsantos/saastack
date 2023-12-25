using Domain.Interfaces.Validations;
using FluentAssertions;
using FluentValidation;
using Infrastructure.Web.Api.Common.Validation;
using UnitTesting.Common.Validation;
using Xunit;

namespace Infrastructure.Web.Api.Common.UnitTests.Validation;

[Trait("Category", "Unit")]
public class AdditionalValidatorSpec
{
    private readonly AdditionalValidator _validator;
    private Dictionary<string, object?>? _dto;

    public AdditionalValidatorSpec()
    {
        _validator = new AdditionalValidator();
        _dto = new Dictionary<string, object?>();
    }

    [Fact]
    public void WhenAllProperties_ThenSucceeds()
    {
        _validator.ValidateAndThrow(_dto!);
    }

    [Fact]
    public void WhenAnyNameIsEmpty_ThenThrows()
    {
        _dto = new Dictionary<string, object?>
        {
            { "aname1", "avalue1" },
            { string.Empty, "avalue2" },
            { "aname3", "avalue3" }
        };

        _validator.Invoking(x => x.ValidateAndThrow(_dto!))
            .Should().Throw<ValidationException>()
            .WithMessageLike(ValidationResources.AdditionalValidator_InvalidName);
    }

    [Fact]
    public void WhenAnyValueIsNull_ThenSucceeds()
    {
        _dto = new Dictionary<string, object?>
        {
            { "aname1", "avalue1" },
            { "aname2", null! },
            { "aname3", "avalue3" }
        };

        _validator.ValidateAndThrow(_dto!);
    }

    [Fact]
    public void WhenAnyStringValueIsValid_ThenSucceeds()
    {
        _dto = new Dictionary<string, object?>
        {
            { "aname1", "avalue1" },
            { "aname2", new string('x', CommonValidations.Recording.AdditionalStringValue.MaxLength!.Value) },
            { "aname3", "avalue3" }
        };

        _validator.ValidateAndThrow(_dto!);
    }

    [Fact]
    public void WhenAnyStringValueIsInvalid_ThenThrows()
    {
        _dto = new Dictionary<string, object?>
        {
            { "aname1", "avalue1" },
            { "aname2", new string('x', CommonValidations.Recording.AdditionalStringValue.MaxLength!.Value + 1) },
            { "aname3", "avalue3" }
        };

        _validator.Invoking(x => x.ValidateAndThrow(_dto!))
            .Should().Throw<ValidationException>()
            .WithMessageLike(ValidationResources.AdditionalValidator_InvalidStringValue);
    }
}