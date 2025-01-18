using Application.Interfaces;
using Application.Resources.Shared;
using Common;

namespace Application.Services.Shared;

public interface IAncillaryService
{
    Task<Result<SearchResults<Audit>, Error>> SearchAllAuditsPrivateAsync(
        ICallerContext caller, DateTime? sinceUtc, string? organizationId,
        SearchOptions searchOptions, GetOptions getOptions,
        CancellationToken cancellationToken);

    Task<Result<SearchResults<DeliveredEmail>, Error>> SearchAllEmailDeliveriesPrivateAsync(ICallerContext caller,
        DateTime? sinceUtc, string? organizationId, IReadOnlyList<string>? tags, SearchOptions searchOptions,
        GetOptions getOptions, CancellationToken cancellationToken);

    Task<Result<SearchResults<DeliveredSms>, Error>> SearchAllSmsDeliveriesPrivateAsync(ICallerContext caller,
        DateTime? sinceUtc, string? organizationId, IReadOnlyList<string>? tags, SearchOptions searchOptions,
        GetOptions getOptions, CancellationToken cancellationToken);
}