using Domain.Common.Identity;
using Domain.Interfaces.Validations;
using {{Parent.SubdomainName | string.pascalplural}}Domain;
using FluentValidation;
using Infrastructure.Web.Api.Common.Validation;
using Infrastructure.Web.Api.Operations.Shared.{{Parent.SubdomainName | string.pascalplural}};
using JetBrains.Annotations;

namespace {{Parent.SubdomainName | string.pascalplural}}Infrastructure.Api.{{Parent.SubdomainName | string.pascalplural}};

[UsedImplicitly]
{{~if Kind == "Search"~}}
public class {{ActionName}}{{Parent.SubdomainName | string.pascalplural}}RequestValidator : AbstractValidator<{{ActionName}}{{Parent.SubdomainName | string.pascalplural}}Request>
{
    public {{ActionName}}{{Parent.SubdomainName | string.pascalplural}}RequestValidator(IHasSearchOptionsValidator hasSearchOptionsValidator)
    {
        Include(hasSearchOptionsValidator);

        //TODO: Add rules for each of the other fields
        //For example: RuleFor(req => req.Name)
        //    .Matches(Validations.{{Parent.SubdomainName | string.pascalsingular}}.Name)
        //    .WithMessage(Resources.{{ActionName}}{{Parent.SubdomainName | string.pascalsingular}}RequestValidator_InvalidName);
    }
}
{{~else~}}
public class {{ActionName}}{{Parent.SubdomainName | string.pascalsingular}}RequestValidator : AbstractValidator<{{ActionName}}{{Parent.SubdomainName | string.pascalsingular}}Request>
{
{{~if Kind == "Get" || Kind =="PutPatch" || Kind == "Delete"~}}
    public {{ActionName}}{{Parent.SubdomainName | string.pascalsingular}}RequestValidator(IIdentifierFactory idFactory)
    {
        RuleFor(req => req.Id)
            .IsEntityId(idFactory)
            .WithMessage(CommonValidationResources.AnyValidator_InvalidId);
        //TODO: Add rules for each of the other fields
        //For example: RuleFor(req => req.Name)
        //    .Matches(Validations.{{Parent.SubdomainName | string.pascalsingular}}.Name)
        //    .WithMessage(Resources.{{ActionName}}{{Parent.SubdomainName | string.pascalsingular}}RequestValidator_InvalidName);
    }
{{~else~}}
    public {{ActionName}}{{Parent.SubdomainName | string.pascalsingular}}RequestValidator()
    {
        //TODO: Add rules for each of the other fields
        //For example: RuleFor(req => req.Name)
        //    .Matches(Validations.{{Parent.SubdomainName | string.pascalsingular}}.Name)
        //    .WithMessage(Resources.{{ActionName}}{{Parent.SubdomainName | string.pascalsingular}}RequestValidator_InvalidName);
    }
{{~end~}}
}
{{~end~}}