using Application.Resources.Shared;
using FluentAssertions;
using FluentValidation;
using Infrastructure.Web.Api.Operations.Shared.BackEndForFrontEnd;
using UnitTesting.Common.Validation;
using WebsiteHost.Api.Recording;
using Xunit;
using Resources = WebsiteHost.Resources;

namespace Infrastructure.Web.Website.UnitTests.Api.Recording;

[Trait("Category", "Unit")]
public class RecordTraceRequestValidatorSpec
{
    private readonly RecordTraceRequest _dto;
    private readonly RecordTraceRequestValidator _validator;

    public RecordTraceRequestValidatorSpec()
    {
        _validator = new RecordTraceRequestValidator();
        _dto = new RecordTraceRequest
        {
            Level = RecorderTraceLevel.Information.ToString(),
            MessageTemplate = "amessagetemplate"
        };
    }

    [Fact]
    public void WhenAllProperties_ThenSucceeds()
    {
        _validator.ValidateAndThrow(_dto);
    }

    [Fact]
    public void WhenMessageTemplateIsNull_ThenThrows()
    {
        _dto.MessageTemplate = null!;

        _validator.Invoking(x => x.ValidateAndThrow(_dto))
            .Should().Throw<ValidationException>()
            .WithMessageLike(Resources.RecordTraceRequestValidator_InvalidMessageTemplate);
    }

    [Fact]
    public void WhenAnArgumentIsNull_ThenThrows()
    {
        _dto.Arguments = new List<string>
        {
            "anarg1",
            null!,
            "anarg2"
        };

        _validator.Invoking(x => x.ValidateAndThrow(_dto))
            .Should().Throw<ValidationException>()
            .WithMessageLike(Resources.RecordTraceRequestValidator_InvalidMessageArgument);
    }

    [Fact]
    public void WhenArguments_ThenSucceeds()
    {
        _dto.Arguments = new List<string>
        {
            "anarg1",
            "anarg2"
        };

        _validator.ValidateAndThrow(_dto);
    }
}