using Application.Interfaces;
using Application.Resources.Shared;
using Application.Services.Shared;
using Common;
using Infrastructure.Web.Api.Common.Extensions;
using Infrastructure.Web.Api.Interfaces.Clients;
using Infrastructure.Web.Api.Operations.Shared.Cars;

namespace CarsInfrastructure.ApplicationServices;

/// <summary>
///     Provides an HTTP service client to be used to make cross-domain calls over HTTP,
///     when the Cars subdomain is deployed separately from the consumer of this service
/// </summary>
public class CarsHttpServiceClient : ICarsService
{
    private readonly IServiceClient _serviceClient;

    public CarsHttpServiceClient(IServiceClient serviceClient)
    {
        _serviceClient = serviceClient;
    }

    public async Task<Result<Car, Error>> GetCarAsync(ICallerContext caller, string organizationId, string id,
        CancellationToken cancellationToken)
    {
        var response = await _serviceClient.GetAsync(caller, new GetCarRequest
        {
            OrganizationId = organizationId,
            Id = id
        }, null, cancellationToken);

        return response.Match<Result<Car, Error>>(res => res.Value.Car!, error => error.ToError());
    }

    public async Task<Result<Car, Error>> ReleaseCarAvailabilityAsync(ICallerContext caller, string organizationId,
        string id, DateTime fromUtc, DateTime toUtc, CancellationToken cancellationToken)
    {
        var response = await _serviceClient.PutAsync(caller, new ReleaseCarAvailabilityRequest
        {
            OrganizationId = organizationId,
            Id = id,
            FromUtc = fromUtc,
            ToUtc = toUtc
        }, null, cancellationToken);

        return response.Match<Result<Car, Error>>(res => res.Value.Car!, error => error.ToError());
    }

    public async Task<Result<bool, Error>> ReserveCarIfAvailableAsync(ICallerContext caller, string organizationId,
        string id, DateTime fromUtc, DateTime toUtc, string referenceId, CancellationToken cancellationToken)
    {
        var response = await _serviceClient.PutAsync(caller, new ReserveCarIfAvailableRequest
        {
            OrganizationId = organizationId,
            Id = id,
            FromUtc = fromUtc,
            ToUtc = toUtc,
            ReferenceId = referenceId
        }, null, cancellationToken);

        return response.Match<Result<bool, Error>>(res => res.Value.IsReserved, error => error.ToError());
    }
}