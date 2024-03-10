using Application.Interfaces;
using Common.Extensions;
using FluentValidation;
using Infrastructure.Web.Api.Common.Extensions;
using Infrastructure.Web.Api.Interfaces;

namespace Infrastructure.Web.Api.Common.Validation;

/// <summary>
///     Defines a validator for <see cref="IHasGetOptions" />
/// </summary>
public interface IHasGetOptionsValidator : IValidator<IHasGetOptions>;

/// <summary>
///     Validates a <see cref="IHasGetOptions" />
/// </summary>
public class HasGetOptionsValidator : AbstractValidator<IHasGetOptions>, IHasGetOptionsValidator
{
    private const string ResourceReferenceExpression = @"^[\d\w\.]{1,100}$";

    public HasGetOptionsValidator()
    {
        When(req => req.Embed.HasValue(), () =>
        {
            RuleForEach(req => req.ToGetOptions(null, null)
                    .ResourceReferences)
                .Matches(ResourceReferenceExpression)
                .WithMessage(ValidationResources.HasGetOptionsValidator_InvalidEmbed);
            RuleFor(req => req.ToGetOptions(null, null)
                    .ResourceReferences.Count())
                .LessThanOrEqualTo(GetOptions.MaxResourceReferences)
                .WithMessage(
                    ValidationResources.HasGetOptionsValidator_TooManyResourceReferences.Format(GetOptions
                        .MaxResourceReferences));
        });
    }
}