using Application.Interfaces;
using Application.Interfaces.Services;
using Application.Services.Shared;
using Common;
using Common.Extensions;
using OrganizationsApplication;

namespace OrganizationsInfrastructure.ApplicationServices;

public class OrganizationsInProcessServiceClient : IOrganizationsService
{
    private readonly Func<IOrganizationsApplication> _organizationsApplicationFactory;
    private IOrganizationsApplication? _application;

    /// <summary>
    ///     HACK: LazyGetRequiredService and <see cref="Func{TResult}" /> is needed here to avoid the runtime cyclic dependency
    ///     between
    ///     <see cref="IOrganizationsApplication" /> requiring <see cref="IEndUsersService" />, and
    ///     <see cref="IEndUsersApplication" /> requiring <see cref="IOrganizationsService" />
    /// </summary>
    public OrganizationsInProcessServiceClient(Func<IOrganizationsApplication> organizationsApplicationFactory)
    {
        _organizationsApplicationFactory = organizationsApplicationFactory;
    }

    public async Task<Result<Error>> ChangeSettingsPrivateAsync(ICallerContext caller, string id,
        TenantSettings settings,
        CancellationToken cancellationToken)
    {
        return await GetApplication().ChangeSettingsAsync(caller, id, settings, cancellationToken);
    }

    public async Task<Result<TenantSettings, Error>> GetSettingsPrivateAsync(ICallerContext caller, string id,
        CancellationToken cancellationToken)
    {
        return await GetApplication().GetSettingsAsync(caller, id, cancellationToken);
    }

    private IOrganizationsApplication GetApplication()
    {
        if (_application.NotExists())
        {
            _application = _organizationsApplicationFactory();
        }

        return _application;
    }
}