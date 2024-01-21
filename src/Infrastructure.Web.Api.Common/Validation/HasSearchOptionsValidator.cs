using Application.Interfaces;
using Common.Extensions;
using FluentValidation;
using Infrastructure.Web.Api.Interfaces;

namespace Infrastructure.Web.Api.Common.Validation;

/// <summary>
///     Defines a validator for <see cref="IHasSearchOptions" />
/// </summary>
public interface IHasSearchOptionsValidator : IValidator<IHasSearchOptions>
{
}

/// <summary>
///     Validates a <see cref="IHasSearchOptions" /> request
/// </summary>
public class HasSearchOptionsValidator : AbstractValidator<IHasSearchOptions>, IHasSearchOptionsValidator
{
    private const string FilterExpression = @"^((([\;\,]{0,1})([\d\w\._]{1,25})){1,25})$";
    private const string SortExpression = @"^((([\;\,]{0,1})([\+\-]{0,1})([\d\w\._]{1,25})){1,5})$";

    public HasSearchOptionsValidator(IHasGetOptionsValidator hasGetOptionsValidator)
    {
        RuleFor(req => req.Limit!.Value)
            .InclusiveBetween(SearchOptions.NoLimit, SearchOptions.MaxLimit)
            .When(req => req.Limit.HasValue)
            .WithMessage(
                ValidationResources.HasSearchOptionsValidator_InvalidLimit.Format(SearchOptions.NoLimit,
                    SearchOptions.DefaultLimit));
        RuleFor(req => req.Offset!.Value)
            .InclusiveBetween(SearchOptions.NoOffset, SearchOptions.MaxLimit)
            .When(req => req.Offset.HasValue)
            .WithMessage(
                ValidationResources.HasSearchOptionsValidator_InvalidOffset.Format(SearchOptions.NoOffset,
                    SearchOptions.MaxLimit));
        RuleFor(req => req.Sort!)
            .Matches(SortExpression)
            .When(req => req.Sort.HasValue())
            .WithMessage(ValidationResources.HasSearchOptionsValidator_InvalidSort);
        RuleFor(req => req.Filter!)
            .Matches(FilterExpression)
            .When(req => req.Filter.HasValue())
            .WithMessage(ValidationResources.HasSearchOptionsValidator_InvalidFilter);
        When(req => req.Embed.HasValue(), () =>
        {
            RuleFor(req => req)
                .SetValidator(hasGetOptionsValidator);
        });
    }
}