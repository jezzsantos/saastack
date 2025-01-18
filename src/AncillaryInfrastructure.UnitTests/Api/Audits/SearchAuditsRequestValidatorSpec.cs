using AncillaryInfrastructure.Api.Audits;
using Domain.Common.ValueObjects;
using FluentValidation;
using Infrastructure.Web.Api.Operations.Shared.Ancillary;
using Xunit;

namespace AncillaryInfrastructure.UnitTests.Api.Audits;

[Trait("Category", "Unit")]
public class SearchAllAuditsRequestValidatorSpec
{
    private readonly SearchAllAuditsRequest _dto;
    private readonly SearchAllAuditsRequestValidator _validator;

    public SearchAllAuditsRequestValidatorSpec()
    {
        _validator = new SearchAllAuditsRequestValidator("anid".ToIdentifierFactory());
        _dto = new SearchAllAuditsRequest();
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
}