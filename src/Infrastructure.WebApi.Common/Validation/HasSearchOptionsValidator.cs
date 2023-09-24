using Application.Interfaces;
using Common.Extensions;
using FluentValidation;
using Infrastructure.WebApi.Interfaces;

namespace Infrastructure.WebApi.Common.Validation;

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
        RuleFor(dto => dto.Limit!.Value).InclusiveBetween(SearchOptions.NoLimit, SearchOptions.MaxLimit)
            .When(dto => dto.Limit.HasValue)
            .WithMessage(Resources.HasSearchOptionsValidator_InvalidLimit.Format(SearchOptions.NoLimit,
                SearchOptions.DefaultLimit));
        RuleFor(dto => dto.Offset!.Value).InclusiveBetween(SearchOptions.NoOffset, SearchOptions.MaxLimit)
            .When(dto => dto.Offset.HasValue)
            .WithMessage(Resources.HasSearchOptionsValidator_InvalidOffset.Format(SearchOptions.NoOffset,
                SearchOptions.MaxLimit));
        RuleFor(dto => dto.Sort!).Matches(SortExpression)
            .When(dto => dto.Sort.HasValue())
            .WithMessage(Resources.HasSearchOptionsValidator_InvalidSort);
        RuleFor(dto => dto.Filter!).Matches(FilterExpression)
            .When(dto => dto.Filter.HasValue())
            .WithMessage(Resources.HasSearchOptionsValidator_InvalidFilter);
        When(dto => dto.Embed.HasValue(), () =>
        {
            RuleFor(dto => dto)
                .SetValidator(hasGetOptionsValidator);
        });
    }
}