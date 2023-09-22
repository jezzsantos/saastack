using Application.Interfaces;
using Common.Extensions;
using FluentValidation;
using Infrastructure.WebApi.Interfaces;

namespace Infrastructure.WebApi.Common.Validation;

public interface IHasGetOptionsValidator : IValidator<IHasGetOptions>
{
}

/// <summary>
///     Validates a <see cref="IHasGetOptions" />
/// </summary>
public class HasGetOptionsValidator : AbstractValidator<IHasGetOptions>, IHasGetOptionsValidator
{
    public const string ResourceReferenceExpression = @"^[\d\w\.]{1,100}$";

    public HasGetOptionsValidator()
    {
        When(dto => dto.Embed.HasValue(), () =>
        {
            RuleForEach(dto => dto.ToGetOptions(null, null).ResourceReferences)
                .Matches(ResourceReferenceExpression)
                .WithMessage(Resources.HasGetOptionsValidator_InvalidEmbed);
            RuleFor(dto => dto.ToGetOptions(null, null).ResourceReferences.Count())
                .LessThanOrEqualTo(GetOptions.MaxResourceReferences)
                .WithMessage(
                    Resources.HasGetOptionsValidator_TooManyResourceReferences.Format(GetOptions
                        .MaxResourceReferences));
        });
    }
}