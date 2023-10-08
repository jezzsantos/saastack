using MediatR;
using Microsoft.AspNetCore.Http;

namespace Infrastructure.WebApi.Interfaces;

/// <summary>
///     Defines a incoming REST request and response.
///     Note: <see cref="IRequest{IResult}" /> is required for the MediatR handlers to be wired up
/// </summary>
public partial interface IWebRequest : IRequest<IResult>
{
}
