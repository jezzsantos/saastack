using AncillaryApplication;
using Application.Interfaces;
using Application.Resources.Shared;
using Application.Services.Shared;
using Common;

namespace AncillaryInfrastructure.ApplicationServices;

public class AncillaryInProcessServiceClient : IAncillaryService
{
    private readonly IAncillaryApplication _ancillaryApplication;

    public AncillaryInProcessServiceClient(IAncillaryApplication ancillaryApplication)
    {
        _ancillaryApplication = ancillaryApplication;
    }

    public async Task<Result<SearchResults<Audit>, Error>> SearchAllAuditsPrivateAsync(
        ICallerContext caller, DateTime? sinceUtc, string? organizationId,
        SearchOptions searchOptions, GetOptions getOptions,
        CancellationToken cancellationToken)
    {
        return await _ancillaryApplication.SearchAllAuditsAsync(caller, sinceUtc, organizationId, searchOptions,
            getOptions, cancellationToken);
    }

    public async Task<Result<SearchResults<DeliveredEmail>, Error>> SearchAllEmailDeliveriesPrivateAsync(
        ICallerContext caller, DateTime? sinceUtc, string? organizationId,
        IReadOnlyList<string>? tags, SearchOptions searchOptions, GetOptions getOptions,
        CancellationToken cancellationToken)
    {
        return await _ancillaryApplication.SearchAllEmailDeliveriesAsync(caller, sinceUtc, organizationId, tags,
            searchOptions, getOptions, cancellationToken);
    }

    public async Task<Result<SearchResults<DeliveredSms>, Error>> SearchAllSmsDeliveriesPrivateAsync(
        ICallerContext caller, DateTime? sinceUtc, string? organizationId,
        IReadOnlyList<string>? tags, SearchOptions searchOptions, GetOptions getOptions,
        CancellationToken cancellationToken)
    {
        return await _ancillaryApplication.SearchAllSmsDeliveriesAsync(caller, sinceUtc, organizationId, tags,
            searchOptions, getOptions, cancellationToken);
    }
}