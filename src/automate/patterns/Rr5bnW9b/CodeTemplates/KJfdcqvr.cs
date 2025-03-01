using Domain.Common.Identity;
using Domain.Common.ValueObjects;
using {{Parent.SubdomainName | string.pascalplural}}Domain;
using {{Parent.SubdomainName | string.pascalplural}}Infrastructure.Api.{{Parent.SubdomainName | string.pascalplural}};
using FluentAssertions;
using FluentValidation;
using Infrastructure.Web.Api.Common.Validation;
using Infrastructure.Web.Api.Operations.Shared.{{Parent.SubdomainName | string.pascalplural}};
using Moq;
using UnitTesting.Common.Validation;
using Xunit;

namespace {{Parent.SubdomainName | string.pascalplural}}Infrastructure.UnitTests.Api.{{Parent.SubdomainName | string.pascalplural}};

[Trait("Category", "Unit")]
{{~if Kind == "Search"~}}
public class {{ActionName}}{{Parent.SubdomainName | string.pascalplural}}RequestValidatorSpec
{
    private readonly {{ActionName}}{{Parent.SubdomainName | string.pascalplural}}Request _dto;
    private readonly {{ActionName}}{{Parent.SubdomainName | string.pascalplural}}RequestValidator _validator;

    public {{ActionName}}{{Parent.SubdomainName | string.pascalplural}}RequestValidatorSpec()
{{~else~}}
public class {{ActionName}}{{Parent.SubdomainName | string.pascalsingular}}RequestValidatorSpec
{
    private readonly {{ActionName}}{{Parent.SubdomainName | string.pascalsingular}}Request _dto;
    private readonly {{ActionName}}{{Parent.SubdomainName | string.pascalsingular}}RequestValidator _validator;

    public {{ActionName}}{{Parent.SubdomainName | string.pascalsingular}}RequestValidatorSpec()
{{~end~}}
    {
{{~if Kind == "Search"~}}
        _validator = new {{ActionName}}{{Parent.SubdomainName | string.pascalplural}}RequestValidator(new HasSearchOptionsValidator(new HasGetOptionsValidator()));
        _dto = new {{ActionName}}{{Parent.SubdomainName | string.pascalplural}}Request
        {
            //TODO: add valid values to all mandatory search fields
        };
{{~else~}}
{{~if Kind == "Get" || Kind =="PutPatch" || Kind == "Delete"~}}
        var idFactory = new Mock<IIdentifierFactory>();
        idFactory.Setup(idf => idf.IsValid(It.IsAny<Identifier>()))
            .Returns(true);
        _validator = new {{ActionName}}{{Parent.SubdomainName | string.pascalsingular}}RequestValidator(idFactory.Object);
        _dto = new {{ActionName}}{{Parent.SubdomainName | string.pascalsingular}}Request
        {
            //TODO: add valid values to all mandatory fields
            Id = "anid"
        };
{{~else~}}
        _validator = new {{ActionName}}{{Parent.SubdomainName | string.pascalsingular}}RequestValidator();
        _dto = new {{ActionName}}{{Parent.SubdomainName | string.pascalsingular}}Request
        {
            //TODO: add valid values to all mandatory fields
        };
{{~end~}}
{{~end~}}
    }

    [Fact]
    public void WhenAllProperties_ThenSuccess()
    {
        _validator.ValidateAndThrow(_dto);
    }

    //TODO: add tests for your specific fields
}