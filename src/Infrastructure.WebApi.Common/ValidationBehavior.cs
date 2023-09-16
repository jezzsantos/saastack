using FluentValidation;
using Infrastructure.WebApi.Interfaces;
using JetBrains.Annotations;
using MediatR;
using Microsoft.AspNetCore.Http;

namespace Infrastructure.WebApi.Common;

[UsedImplicitly]
public class ValidationBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, IResult>
    where TRequest : IWebRequest<TResponse>
    where TResponse : IWebResponse
{
    private readonly IValidator<TRequest> _validator;


    public ValidationBehavior(IValidator<TRequest> validator)
    {
        _validator = validator;
    }

    public async Task<IResult> Handle(TRequest request, RequestHandlerDelegate<IResult> next,
        CancellationToken cancellationToken)
    {
        var validationResult = await _validator.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
        {
            return Results.BadRequest(validationResult.Errors);
        }

        return await next();
    }
}