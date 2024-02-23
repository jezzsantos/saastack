using Application.Interfaces;
using Application.Resources.Shared;
using Application.Services.Shared;
using Common;
using EndUsersApplication;

namespace EndUsersInfrastructure.ApplicationServices;

/// <summary>
///     Provides an in-process service client to be used to make cross-domain calls,
///     when the EndUsers subdomain is deployed in the same host as the consumer of this service
/// </summary>
public class EndUsersInProcessServiceClient : IEndUsersService
{
    private readonly IEndUsersApplication _endUsersApplication;

    public EndUsersInProcessServiceClient(IEndUsersApplication endUsersApplication)
    {
        _endUsersApplication = endUsersApplication;
    }

    public async Task<Result<EndUser, Error>> GetPersonAsync(ICallerContext caller, string id,
        CancellationToken cancellationToken)
    {
        return await _endUsersApplication.GetPersonAsync(caller, id, cancellationToken);
    }

    public async Task<Result<Membership, Error>> CreateMembershipForCallerPrivateAsync(ICallerContext caller,
        string organizationId,
        CancellationToken cancellationToken)
    {
        return await _endUsersApplication.CreateMembershipForCallerAsync(caller, organizationId, cancellationToken);
    }

    public async Task<Result<Optional<EndUser>, Error>> FindPersonByEmailPrivateAsync(ICallerContext caller,
        string emailAddress, CancellationToken cancellationToken)
    {
        return await _endUsersApplication.FindPersonByEmailAsync(caller, emailAddress, cancellationToken);
    }

    public async Task<Result<EndUserWithMemberships, Error>> GetMembershipsPrivateAsync(ICallerContext caller,
        string id,
        CancellationToken cancellationToken)
    {
        return await _endUsersApplication.GetMembershipsAsync(caller, id, cancellationToken);
    }

    public async Task<Result<RegisteredEndUser, Error>> RegisterPersonPrivateAsync(ICallerContext caller,
        string emailAddress,
        string firstName, string? lastName, string? timezone, string? countryCode, bool termsAndConditionsAccepted,
        CancellationToken cancellationToken)
    {
        return await _endUsersApplication.RegisterPersonAsync(caller, emailAddress, firstName, lastName, timezone,
            countryCode, termsAndConditionsAccepted, cancellationToken);
    }

    public async Task<Result<RegisteredEndUser, Error>> RegisterMachinePrivateAsync(ICallerContext caller, string name,
        string? timezone, string? countryCode,
        CancellationToken cancellationToken)
    {
        return await _endUsersApplication.RegisterMachineAsync(caller, name, timezone, countryCode, cancellationToken);
    }
}