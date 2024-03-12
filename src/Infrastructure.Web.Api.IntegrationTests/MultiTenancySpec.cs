using System.Net;
using ApiHost1;
using Application.Resources.Shared;
using CarsDomain;
using Common.Extensions;
using FluentAssertions;
using Infrastructure.Web.Api.Common.Extensions;
using Infrastructure.Web.Api.IntegrationTests.Stubs;
using Infrastructure.Web.Api.Operations.Shared.Cars;
using Infrastructure.Web.Api.Operations.Shared.Organizations;
using Infrastructure.Web.Hosting.Common;
using IntegrationTesting.WebApi.Common;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
#if TESTINGONLY
using Application.Interfaces.Services;
using Common.Configuration;
using Infrastructure.Hosting.Common.Extensions;
using Infrastructure.Persistence.Common.ApplicationServices;
using Infrastructure.Persistence.Interfaces;
using Infrastructure.Persistence.Interfaces.ApplicationServices;
using QueryAny;
using Car = CarsApplication.Persistence.ReadModels.Car;
#endif

namespace Infrastructure.Web.Api.IntegrationTests;

[Trait("Category", "Integration.Web")]
[Collection("API")]
public class MultiTenancySpec : WebApiSpec<Program>
{
    public MultiTenancySpec(WebApiSetup<Program> setup) : base(setup, OverrideDependencies)
    {
        EmptyAllRepositories();
        DeleteAllPreviousTenants(StubTenantSettingsService.GetRepositoryPath(null));
    }

    [Fact]
    public async Task WhenOnePersonAccessesAnotherPersonsOrganization_ThenForbidden()
    {
        var loginB = await LoginUserAsync(LoginUser.PersonB);
        var loginBOrgId = loginB.User.Profile!.DefaultOrganizationId!;

        var loginA = await LoginUserAsync();
        var result = await Api.GetAsync(new SearchAllCarsRequest
        {
            OrganizationId = loginBOrgId
        }, req => req.SetJWTBearerToken(loginA.AccessToken));

        result.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        result.Content.Error.Detail.Should().Be(Resources.MultiTenancyMiddleware_UserNotAMember.Format(loginBOrgId));
    }

    [Fact]
    public async Task WhenOnePersonAccessesAnotherPersonsOrganizationVisaVersa_ThenForbidden()
    {
        var loginA = await LoginUserAsync();
        var loginAOrgId = loginA.User.Profile!.DefaultOrganizationId!;

        var loginB = await LoginUserAsync(LoginUser.PersonB);
        var result = await Api.GetAsync(new SearchAllCarsRequest
        {
            OrganizationId = loginAOrgId
        }, req => req.SetJWTBearerToken(loginB.AccessToken));

        result.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        result.Content.Error.Detail.Should().Be(Resources.MultiTenancyMiddleware_UserNotAMember.Format(loginAOrgId));
    }

    [Fact]
    public async Task WhenCreateTenantedDataToPhysicalTenantStores_ThenReturnsTenantedData()
    {
        var loginA = await LoginUserAsync();
        var organization1Id = loginA.User.Profile!.DefaultOrganizationId!;
        var organization1 = (await Api.GetAsync(new GetOrganizationRequest
        {
            Id = organization1Id
        }, req => req.SetJWTBearerToken(loginA.AccessToken))).Content.Value.Organization!;
        var organization2 = (await Api.PostAsync(new CreateOrganizationRequest
        {
            Name = "anorganizationname2"
        }, req => req.SetJWTBearerToken(loginA.AccessToken))).Content.Value.Organization!;

        loginA = await ReAuthenticateUserAsync(loginA.User);
        var car1Id = await CreateUnregisteredCarAsync(loginA, organization1, 2010);
        var car2Id = await CreateUnregisteredCarAsync(loginA, organization2, 2010);
        var car3Id = await CreateUnregisteredCarAsync(loginA, organization1, 2011);
        var car4Id = await CreateUnregisteredCarAsync(loginA, organization2, 2011);
        var car5Id = await CreateUnregisteredCarAsync(loginA, organization1, 2012);
        var car6Id = await CreateUnregisteredCarAsync(loginA, organization2, 2012);

        var cars1 = (await Api.GetAsync(new SearchAllCarsRequest
        {
            OrganizationId = organization1.Id,
            Sort = "+LastPersistedAtUtc"
        }, req => req.SetJWTBearerToken(loginA.AccessToken))).Content.Value.Cars;
        var cars2 = (await Api.GetAsync(new SearchAllCarsRequest
        {
            OrganizationId = organization2.Id,
            Sort = "+LastPersistedAtUtc"
        }, req => req.SetJWTBearerToken(loginA.AccessToken))).Content.Value.Cars;

        // Proves the Data was logically partitioned 
        cars1!.Count.Should().Be(3);
        cars1[0].Id.Should().Be(car1Id);
        cars1[1].Id.Should().Be(car3Id);
        cars1[2].Id.Should().Be(car5Id);
        cars2!.Count.Should().Be(3);
        cars2[0].Id.Should().Be(car2Id);
        cars2[1].Id.Should().Be(car4Id);
        cars2[2].Id.Should().Be(car6Id);

#if TESTINGONLY
        var repository1 =
            LocalMachineJsonFileStore.Create(
                new FakeConfigurationSettings(StubTenantSettingsService.GetRepositoryPath(organization1.Id)));
        var repository2 =
            LocalMachineJsonFileStore.Create(
                new FakeConfigurationSettings(StubTenantSettingsService.GetRepositoryPath(organization2.Id)));

        var carsRaw1 = (await repository1.QueryAsync(typeof(Car).GetEntityNameSafe(), Query.From<Car>()
                    .WhereAll()
                    .OrderBy(car => car.LastPersistedAtUtc), PersistedEntityMetadata.FromType<Car>(),
                CancellationToken.None))
            .Value;
        var carsRaw2 = (await repository2.QueryAsync(typeof(Car).GetEntityNameSafe(),
            Query.From<Car>().WhereAll().OrderBy(car => car.LastPersistedAtUtc),
            PersistedEntityMetadata.FromType<Car>(), CancellationToken.None)).Value;

        // Proves the Data was physically partitioned 
        carsRaw1.Count.Should().Be(3);
        carsRaw1[0].Id.Should().Be(car1Id);
        carsRaw1[1].Id.Should().Be(car3Id);
        carsRaw1[2].Id.Should().Be(car5Id);
        carsRaw2.Count.Should().Be(3);
        carsRaw2[0].Id.Should().Be(car2Id);
        carsRaw2[1].Id.Should().Be(car4Id);
        carsRaw2[2].Id.Should().Be(car6Id);
#endif
    }

    private static void OverrideDependencies(IServiceCollection services)
    {
#if TESTINGONLY
        services.AddSingleton<ITenantSettingsService, StubTenantSettingsService>();
        services
            .AddPerHttpRequest<IDataStore, IEventStore, IBlobStore, IQueueStore, LocalMachineJsonFileStore>(c =>
                LocalMachineJsonFileStore.Create(c.GetRequiredService<IConfigurationSettings>(),
                    c.GetRequiredService<IQueueStoreNotificationHandler>()));
#endif
    }

    private static void DeleteAllPreviousTenants(string path)
    {
        var basePath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        var rootPath = Path.Combine(basePath, path);
        if (Directory.Exists(rootPath))
        {
            Directory.Delete(rootPath, true);
        }
    }

    private async Task<string> CreateUnregisteredCarAsync(LoginDetails login, Organization organization, int year)
    {
        var car = await Api.PostAsync(new RegisterCarRequest
        {
            OrganizationId = organization.Id,
            Make = Manufacturer.AllowedMakes[0],
            Model = Manufacturer.AllowedModels[0],
            Year = year,
            Jurisdiction = Jurisdiction.AllowedCountries[0],
            NumberPlate = "aplate"
        }, req => req.SetJWTBearerToken(login.AccessToken));

        return car.Content.Value.Car!.Id;
    }

#if TESTINGONLY
    private class FakeConfigurationSettings : IConfigurationSettings
    {
        private readonly string _path;

        public FakeConfigurationSettings(string path)
        {
            _path = path;
        }

        public bool IsConfigured => true;

        public bool GetBool(string key, bool? defaultValue = null)
        {
            throw new NotImplementedException();
        }

        public double GetNumber(string key, double? defaultValue = null)
        {
            throw new NotImplementedException();
        }

        public string GetString(string key, string? defaultValue = null)
        {
#if TESTINGONLY
            if (key == LocalMachineJsonFileStore.PathSettingName)
            {
                return _path;
            }
#endif

            return null!;
        }

        public ISettings Platform => this;

        public ISettings Tenancy => this;
    }
#endif
}