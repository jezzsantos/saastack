using System.Diagnostics.CodeAnalysis;
using Common;
using Common.Extensions;
using Infrastructure.Web.Api.Interfaces;
using Microsoft.AspNetCore.Http;

namespace Infrastructure.Web.Api.Common.Extensions;

[ExcludeFromCodeCoverage]
public static class HandlerExtensions
{
    /// <summary>
    ///     Converts the <see cref="result" /> into an appropriate <see cref="IResult" /> depending on error returned
    /// </summary>
    public static IResult HandleApiResult(this ApiStreamResult result, OperationMethod method)
    {
        return result()
            .Match(response => response.Value.ToResult(method), error => error.ToResult());
    }

    /// <summary>
    ///     Converts the <see cref="result" /> into an appropriate <see cref="IResult" /> depending on error returned
    /// </summary>
    public static IResult HandleApiResult(this ApiEmptyResult result, OperationMethod method)
    {
        return result()
            .Match(response => (response.HasValue
                    ? response.Value
                    : new PostResult<EmptyResponse>(new EmptyResponse())).ToResult(method),
                error => error.ToResult());
    }

    /// <summary>
    ///     Converts the <see cref="result" /> into an appropriate <see cref="IResult" /> depending on error returned
    /// </summary>
    public static IResult HandleApiResult<TResource, TResponse>(this ApiResult<TResource, TResponse> result,
        OperationMethod method)
        where TResponse : IWebResponse
    {
        return result()
            .Match(response => ((PostResult<TResponse>)response.Value).ToResult(method), error => error.ToResult());
    }

    /// <summary>
    ///     Converts the <see cref="result" /> into an appropriate <see cref="IResult" /> depending on error returned
    /// </summary>
    public static IResult HandleApiResult<TResource, TResponse>(this ApiPostResult<TResource, TResponse> result,
        OperationMethod method)
        where TResponse : IWebResponse
    {
        return result()
            .Match(response => response.Value.ToResult(method), error => error.ToResult());
    }

    /// <summary>
    ///     Converts the <see cref="result" /> into an appropriate <see cref="IResult" /> depending on error returned
    /// </summary>
    public static IResult HandleApiResult<TResource, TResponse>(this ApiPutPatchResult<TResource, TResponse> result,
        OperationMethod method)
        where TResponse : IWebResponse
    {
        return result()
            .Match(response => ((PostResult<TResponse>)response.Value).ToResult(method), error => error.ToResult());
    }

    /// <summary>
    ///     Converts the <see cref="result" /> into an appropriate <see cref="IResult" /> depending on error returned
    /// </summary>
    public static IResult HandleApiResult<TResource, TResponse>(this ApiGetResult<TResource, TResponse> result,
        OperationMethod method)
        where TResponse : IWebResponse
    {
        return result()
            .Match(response => ((PostResult<TResponse>)response.Value).ToResult(method), error => error.ToResult());
    }

    /// <summary>
    ///     Converts the <see cref="result" /> into an appropriate <see cref="IResult" /> depending on error returned
    /// </summary>
    public static IResult HandleApiResult<TResource, TResponse>(this ApiSearchResult<TResource, TResponse> result,
        OperationMethod method)
        where TResponse : IWebSearchResponse
    {
        return result()
            .Match(response => ((PostResult<TResponse>)response.Value).ToResult(method), error => error.ToResult());
    }

    /// <summary>
    ///     Converts the <see cref="result" /> into an appropriate <see cref="IResult" /> depending on error returned
    /// </summary>
    public static IResult HandleApiResult(this ApiDeleteResult result, OperationMethod method)
    {
        return result()
            .Match(response => ((PostResult<EmptyResponse>)response.Value).ToResult(method),
                error => error.ToResult());
    }

    /// <summary>
    ///     Converts the <see cref="Result{TResource,Error}" /> to an <see cref="Result{EmptyResponse,Error}" />
    /// </summary>
    public static Result<EmptyResponse, Error> HandleApplicationResult<TResource>(this Result<TResource, Error> result)
    {
        return result.Match(_ => new Result<EmptyResponse, Error>(new EmptyResponse()),
            error => new Result<EmptyResponse, Error>(error));
    }

    /// <summary>
    ///     Converts the <see cref="TResource" /> in the <see cref="Result{TResource,Error}" /> to an
    ///     <see cref="Result{TResponse,Error}" />
    ///     using the <see cref="onSuccess" /> callback
    /// </summary>
    public static Result<TResponse, Error> HandleApplicationResult<TResource, TResponse>(
        this Result<TResource, Error> result, Func<TResource, TResponse> onSuccess)
        where TResponse : IWebResponse
    {
        return result.Match(resource => new Result<TResponse, Error>(onSuccess(resource.Value)),
            error => new Result<TResponse, Error>(error));
    }

    /// <summary>
    ///     Converts the <see cref="TResource" /> in the <see cref="Result{TResource,Error}" /> to an
    ///     <see cref="Result{PostResult,Error}" />
    ///     using the <see cref="onSuccess" /> callback
    /// </summary>
    public static Result<PostResult<TResponse>, Error> HandleApplicationResult<TResource, TResponse>(
        this Result<TResource, Error> result, Func<TResource, PostResult<TResponse>> onSuccess)
        where TResponse : IWebResponse
    {
        return result.Match(resource => new Result<PostResult<TResponse>, Error>(onSuccess(resource.Value)),
            error => new Result<PostResult<TResponse>, Error>(error));
    }

    /// <summary>
    ///     Converts the <see cref="Result{Error}" /> to an <see cref="Result{EmptyResponse,Error}" />
    /// </summary>
    public static Result<EmptyResponse, Error> HandleApplicationResult(this Result<Error> result)
    {
        return result.Match(() => new Result<EmptyResponse, Error>(new EmptyResponse()),
            error => new Result<EmptyResponse, Error>(error));
    }

    /// <summary>
    ///     Converts the <see cref="Result{IHasStream,Error}" /> to an <see cref="Result{StreamResult,Error}" />
    /// </summary>
    public static Result<StreamResult, Error> HandleApplicationResult<TResource>(this Result<TResource, Error> result,
        Func<TResource, StreamResult> onSuccess)
    {
        return result.Match(stream => new Result<StreamResult, Error>(onSuccess(stream.Value)),
            error => new Result<StreamResult, Error>(error));
    }

    private static IResult ToResult(this StreamResult result, OperationMethod _)
    {
        return Results.Stream(result.Stream, result.ContentType);
    }

    private static IResult ToResult<TResponse>(this PostResult<TResponse> postResult, OperationMethod method)
        where TResponse : IWebResponse
    {
        var response = postResult.Response;
        var location = postResult.Location;
        switch (method)
        {
            case OperationMethod.Get:
            case OperationMethod.Search:
                return Results.Ok(response);

            case OperationMethod.Post:
            {
                return location.HasValue()
                    ? Results.Created(location, response)
                    : Results.Ok(response);
            }

            case OperationMethod.PutPatch:
                return Results.Accepted(null, response);

            case OperationMethod.Delete:
                var hasResponse = response is not EmptyResponse;
                return hasResponse
                    ? Results.Accepted(null, response)
                    : Results.NoContent();

            default:
                return Results.Ok(response);
        }
    }

    private static IResult ToResult(this Error error)
    {
        var httpError = error.ToHttpError();
        return Results.Problem(statusCode: (int)httpError.Code, detail: httpError.Message);
    }
}