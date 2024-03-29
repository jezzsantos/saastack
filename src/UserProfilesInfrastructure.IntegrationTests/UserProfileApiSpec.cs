using System.Net;
using ApiHost1;
using Common;
using FluentAssertions;
using Infrastructure.Web.Api.Common.Extensions;
using Infrastructure.Web.Api.Operations.Shared.UserProfiles;
using IntegrationTesting.WebApi.Common;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace UserProfilesInfrastructure.IntegrationTests;

[Trait("Category", "Integration.Web")]
[Collection("API")]
public class UserProfileApiSpec : WebApiSpec<Program>
{
    public UserProfileApiSpec(WebApiSetup<Program> setup) : base(setup, OverrideDependencies)
    {
        EmptyAllRepositories();
    }

    [Fact]
    public async Task WhenChangeProfile_ThenChanges()
    {
        var login = await LoginUserAsync();

        var result = await Api.PatchAsync(new ChangeProfileRequest
        {
            UserId = login.User.Id,
            FirstName = "anewfirstname",
            LastName = "anewlastname",
            DisplayName = "anewdisplayname",
            Timezone = Timezones.Sydney.ToString()
        }, req => req.SetJWTBearerToken(login.AccessToken));

        result.Content.Value.Profile!.Name.FirstName.Should().Be("anewfirstname");
        result.Content.Value.Profile.Name.LastName.Should().Be("anewlastname");
        result.Content.Value.Profile.DisplayName.Should().Be("anewdisplayname");
        result.Content.Value.Profile.Timezone.Should().Be(Timezones.Sydney.ToString());
    }

    [Fact]
    public async Task WhenChangeProfileByAnotherUser_ThenForbidden()
    {
        var loginA = await LoginUserAsync();
        var loginB = await LoginUserAsync(LoginUser.PersonB);

        var result = await Api.PatchAsync(new ChangeProfileRequest
        {
            UserId = loginA.User.Id,
            FirstName = "anewfirstname",
            LastName = "anewlastname",
            DisplayName = "anewdisplayname",
            Timezone = Timezones.Sydney.ToString()
        }, req => req.SetJWTBearerToken(loginB.AccessToken));

        result.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task WhenChangeContactAddress_ThenChanges()
    {
        var login = await LoginUserAsync();

        var result = await Api.PatchAsync(new ChangeProfileContactAddressRequest
        {
            UserId = login.User.Id,
            Line1 = "anewline1",
            Line2 = "anewline2",
            Line3 = "anewline3",
            City = "anewcity",
            State = "anewstate",
            CountryCode = CountryCodes.Australia.ToString(),
            Zip = "anewzipcode"
        }, req => req.SetJWTBearerToken(login.AccessToken));

        result.Content.Value.Profile!.Address.Line1.Should().Be("anewline1");
        result.Content.Value.Profile.Address.Line2.Should().Be("anewline2");
        result.Content.Value.Profile.Address.Line3.Should().Be("anewline3");
        result.Content.Value.Profile.Address.City.Should().Be("anewcity");
        result.Content.Value.Profile.Address.State.Should().Be("anewstate");
        result.Content.Value.Profile.Address.CountryCode.Should().Be(CountryCodes.Australia.ToString());
        result.Content.Value.Profile.Address.Zip.Should().Be("anewzipcode");
    }

    [Fact]
    public async Task WhenChangeContactAddressByAnotherUser_ThenForbidden()
    {
        var loginA = await LoginUserAsync();
        var loginB = await LoginUserAsync(LoginUser.PersonB);

        var result = await Api.PatchAsync(new ChangeProfileContactAddressRequest
        {
            UserId = loginA.User.Id,
            Line1 = "anewline1",
            Line2 = "anewline2",
            Line3 = "anewline3",
            City = "anewcity",
            State = "anewstate",
            CountryCode = CountryCodes.Australia.ToString(),
            Zip = "anewzipcode"
        }, req => req.SetJWTBearerToken(loginB.AccessToken));

        result.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    private static void OverrideDependencies(IServiceCollection services)
    {
        // do nothing
    }
}