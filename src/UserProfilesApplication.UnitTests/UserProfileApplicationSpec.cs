using Application.Interfaces;
using Application.Resources.Shared;
using Common;
using Domain.Common.Identity;
using Domain.Common.ValueObjects;
using Domain.Interfaces.Entities;
using Domain.Shared;
using FluentAssertions;
using Moq;
using UnitTesting.Common;
using UserProfilesApplication.Persistence;
using UserProfilesDomain;
using Xunit;
using PersonName = Domain.Shared.PersonName;

namespace UserProfilesApplication.UnitTests;

[Trait("Category", "Unit")]
public class UserProfileApplicationSpec
{
    private readonly UserProfilesApplication _application;
    private readonly Mock<ICallerContext> _caller;
    private readonly Mock<IIdentifierFactory> _idFactory;
    private readonly Mock<IRecorder> _recorder;
    private readonly Mock<IUserProfileRepository> _repository;

    public UserProfileApplicationSpec()
    {
        _recorder = new Mock<IRecorder>();
        _caller = new Mock<ICallerContext>();
        _idFactory = new Mock<IIdentifierFactory>();
        _idFactory.Setup(idf => idf.Create(It.IsAny<IIdentifiableEntity>()))
            .Returns("anid".ToId());
        _repository = new Mock<IUserProfileRepository>();
        _repository.Setup(rep => rep.FindByUserIdAsync(It.IsAny<Identifier>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Optional<UserProfileRoot>.None);
        _repository.Setup(rep => rep.FindByEmailAddressAsync(It.IsAny<EmailAddress>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Optional<UserProfileRoot>.None);
        _repository.Setup(rep => rep.SaveAsync(It.IsAny<UserProfileRoot>(), It.IsAny<CancellationToken>()))
            .Returns((UserProfileRoot root, CancellationToken _) =>
                Task.FromResult<Result<UserProfileRoot, Error>>(root));

        _application = new UserProfilesApplication(_recorder.Object, _idFactory.Object, _repository.Object);
    }

    [Fact]
    public async Task WhenCreateProfileForAnyAndExistsForUserId_ThenReturnsError()
    {
        var user = UserProfileRoot.Create(_recorder.Object, _idFactory.Object, ProfileType.Person, "auserid".ToId(),
            PersonName.Create("afirstname", "alastname").Value).Value;
        _repository.Setup(rep => rep.FindByUserIdAsync(It.IsAny<Identifier>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(user.ToOptional());

        var result = await _application.CreateProfileAsync(_caller.Object, UserProfileClassification.Person,
            "apersonid",
            "anemailaddress", "afirstname", "alastname", Timezones.Default.ToString(), CountryCodes.Default.ToString(),
            CancellationToken.None);

        result.Should().BeError(ErrorCode.EntityExists, Resources.UserProfilesApplication_ProfileExistsForUser);
    }

    [Fact]
    public async Task WhenCreateProfileForAnyAndExistsForEmailAddress_ThenReturnsError()
    {
        var user = UserProfileRoot.Create(_recorder.Object, _idFactory.Object, ProfileType.Person, "auserid".ToId(),
            PersonName.Create("afirstname", "alastname").Value).Value;
        _repository.Setup(rep => rep.FindByEmailAddressAsync(It.IsAny<EmailAddress>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(user.ToOptional());

        var result = await _application.CreateProfileAsync(_caller.Object, UserProfileClassification.Person,
            "apersonid",
            "auser@company.com", "afirstname", "alastname", Timezones.Default.ToString(),
            CountryCodes.Default.ToString(),
            CancellationToken.None);

        result.Should().BeError(ErrorCode.EntityExists, Resources.UserProfilesApplication_ProfileExistsForEmailAddress);
    }

    [Fact]
    public async Task WhenCreateProfileAsyncForMachine_ThenCreatesProfile()
    {
        var result = await _application.CreateProfileAsync(_caller.Object, UserProfileClassification.Machine,
            "amachineid",
            "anemailaddress", "afirstname", "alastname", Timezones.Default.ToString(), CountryCodes.Default.ToString(),
            CancellationToken.None);

        result.Value.UserId.Should().Be("amachineid".ToId());
        result.Value.Classification.Should().Be(UserProfileClassification.Machine);
        result.Value.DisplayName.Should().Be("afirstname");
        result.Value.Name.FirstName.Should().Be("afirstname");
        result.Value.Name.LastName.Should().BeNull();
        result.Value.EmailAddress.Should().BeNull();
        result.Value.PhoneNumber.Should().BeNull();
        result.Value.Address.CountryCode.Should().Be(CountryCodes.Default.ToString());
        result.Value.Timezone.Should().Be(Timezones.Default.ToString());
        result.Value.AvatarUrl.Should().BeNull();
        _repository.Verify(rep => rep.SaveAsync(It.Is<UserProfileRoot>(up =>
            up.UserId == "amachineid".ToId()
            && up.Type == ProfileType.Machine
            && up.DisplayName.Value.Text == "afirstname"
            && up.Name.Value.FirstName == "afirstname"
            && up.Name.Value.LastName.HasValue == false
            && up.EmailAddress.HasValue == false
            && up.PhoneNumber.HasValue == false
            && up.Address.CountryCode == CountryCodes.Default
            && up.Timezone == Timezones.Default
            && up.AvatarUrl.HasValue == false
        ), It.IsAny<CancellationToken>()));
    }

    [Fact]
    public async Task WhenCreateProfileAsyncForPerson_ThenCreatesProfile()
    {
        var result = await _application.CreateProfileAsync(_caller.Object, UserProfileClassification.Person,
            "apersonid",
            "auser@company.com", "afirstname", "alastname", Timezones.Default.ToString(),
            CountryCodes.Default.ToString(), CancellationToken.None);

        result.Value.UserId.Should().Be("apersonid".ToId());
        result.Value.Classification.Should().Be(UserProfileClassification.Person);
        result.Value.DisplayName.Should().Be("afirstname");
        result.Value.Name.FirstName.Should().Be("afirstname");
        result.Value.Name.LastName.Should().Be("alastname");
        result.Value.EmailAddress.Should().Be("auser@company.com");
        result.Value.PhoneNumber.Should().BeNull();
        result.Value.Address.CountryCode.Should().Be(CountryCodes.Default.ToString());
        result.Value.Timezone.Should().Be(Timezones.Default.ToString());
        result.Value.AvatarUrl.Should().BeNull();
        _repository.Verify(rep => rep.SaveAsync(It.Is<UserProfileRoot>(up =>
            up.UserId == "apersonid".ToId()
            && up.Type == ProfileType.Person
            && up.DisplayName.Value.Text == "afirstname"
            && up.Name.Value.FirstName == "afirstname"
            && up.Name.Value.LastName.Value == "alastname"
            && up.EmailAddress.Value == "auser@company.com"
            && up.PhoneNumber.HasValue == false
            && up.Address.CountryCode == CountryCodes.Default
            && up.Timezone == Timezones.Default
            && up.AvatarUrl.HasValue == false
        ), It.IsAny<CancellationToken>()));
    }

    [Fact]
    public async Task WhenFindPersonByEmailAddressAsyncAndNotExists_ThenReturns()
    {
        var result =
            await _application.FindPersonByEmailAddressAsync(_caller.Object, "auser@company.com",
                CancellationToken.None);

        result.Should().BeSuccess();
        result.Value.HasValue.Should().BeFalse();
    }

    [Fact]
    public async Task WhenFindPersonByEmailAddressAsyncAndExists_ThenReturnsProfile()
    {
        var user = UserProfileRoot.Create(_recorder.Object, _idFactory.Object, ProfileType.Person, "auserid".ToId(),
            PersonName.Create("afirstname", "alastname").Value).Value;
        _repository.Setup(rep => rep.FindByEmailAddressAsync(It.IsAny<EmailAddress>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(user.ToOptional());

        var result =
            await _application.FindPersonByEmailAddressAsync(_caller.Object, "auser@company.com",
                CancellationToken.None);

        result.Should().BeSuccess();
        result.Value.HasValue.Should().BeTrue();
        result.Value.Value.Id.Should().Be("anid");
    }

    [Fact]
    public async Task WhenChangeProfileAsyncAndNotOwner_ThenReturnsError()
    {
        _caller.Setup(cc => cc.CallerId)
            .Returns("auserid");

        var result = await _application.ChangeProfileAsync(_caller.Object, "anotheruserid", "afirstname", "alastname",
            "adisplayname", "aphonenumber", "atimezone", CancellationToken.None);

        result.Should().BeError(ErrorCode.ForbiddenAccess);
    }

    [Fact]
    public async Task WhenChangeProfileAsyncAndNotExists_ThenReturnsError()
    {
        _caller.Setup(cc => cc.CallerId)
            .Returns("auserid");

        var result = await _application.ChangeProfileAsync(_caller.Object, "auserid", "afirstname", "alastname",
            "adisplayname",
            "aphonenumber", "atimezone", CancellationToken.None);

        result.Should().BeError(ErrorCode.EntityNotFound);
    }

    [Fact]
    public async Task WhenChangeProfileAsync_ThenChangesProfile()
    {
        _caller.Setup(cc => cc.CallerId)
            .Returns("auserid");
        var user = UserProfileRoot.Create(_recorder.Object, _idFactory.Object, ProfileType.Person, "auserid".ToId(),
            PersonName.Create("afirstname", "alastname").Value).Value;
        _repository.Setup(rep => rep.FindByUserIdAsync(It.IsAny<Identifier>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(user.ToOptional());

        var result = await _application.ChangeProfileAsync(_caller.Object, "auserid", "anewfirstname",
            "anewlastname", "anewdisplayname",
            "+6498876986", Timezones.Sydney.ToString(), CancellationToken.None);

        result.Should().BeSuccess();
        result.Value.Name.FirstName.Should().Be("anewfirstname");
        result.Value.Name.LastName.Should().Be("anewlastname");
        result.Value.DisplayName.Should().Be("anewdisplayname");
        result.Value.PhoneNumber.Should().Be("+6498876986");
        result.Value.Timezone.Should().Be(Timezones.Sydney.ToString());
    }

    [Fact]
    public async Task WhenChangeContactAddressAsyncAndNotOwner_ThenReturnsError()
    {
        _caller.Setup(cc => cc.CallerId)
            .Returns("auserid");

        var result = await _application.ChangeContactAddressAsync(_caller.Object, "anotheruserid", "anewline1",
            "anewline2", "anewline3",
            "anewcity", "anewstate", CountryCodes.Australia.ToString(), "anewzipcode", CancellationToken.None);

        result.Should().BeError(ErrorCode.ForbiddenAccess);
    }

    [Fact]
    public async Task WhenChangeContactAddressAsyncAndNotExists_ThenReturnsError()
    {
        _caller.Setup(cc => cc.CallerId)
            .Returns("auserid");

        var result = await _application.ChangeContactAddressAsync(_caller.Object, "auserid", "anewline1",
            "anewline2", "anewline3",
            "anewcity", "anewstate", CountryCodes.Australia.ToString(), "anewzipcode", CancellationToken.None);

        result.Should().BeError(ErrorCode.EntityNotFound);
    }

    [Fact]
    public async Task WhenChangeContactAddressAsync_ThenReturnsError()
    {
        _caller.Setup(cc => cc.CallerId)
            .Returns("auserid");
        var user = UserProfileRoot.Create(_recorder.Object, _idFactory.Object, ProfileType.Person, "auserid".ToId(),
            PersonName.Create("afirstname", "alastname").Value).Value;
        _repository.Setup(rep => rep.FindByUserIdAsync(It.IsAny<Identifier>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(user.ToOptional());

        var result = await _application.ChangeContactAddressAsync(_caller.Object, "auserid", "anewline1",
            "anewline2", "anewline3", "anewcity", "anewstate", CountryCodes.Australia.ToString(), "anewzipcode",
            CancellationToken.None);

        result.Should().BeSuccess();
        result.Value.Address.Line1.Should().Be("anewline1");
        result.Value.Address.Line2.Should().Be("anewline2");
        result.Value.Address.Line3.Should().Be("anewline3");
        result.Value.Address.City.Should().Be("anewcity");
        result.Value.Address.State.Should().Be("anewstate");
        result.Value.Address.CountryCode.Should().Be(CountryCodes.Australia.ToString());
        result.Value.Address.Zip.Should().Be("anewzipcode");
    }

    [Fact]
    public async Task WhenGetProfileAsyncAndNotOwner_ThenReturnsError()
    {
        _caller.Setup(cc => cc.CallerId)
            .Returns("auserid");

        _repository.Setup(rep => rep.FindByUserIdAsync(It.IsAny<Identifier>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Optional<UserProfileRoot>.None);

        var result = await _application.GetProfileAsync(_caller.Object, "anotheruserid", CancellationToken.None);

        result.Should().BeError(ErrorCode.ForbiddenAccess);
    }

    [Fact]
    public async Task WhenGetProfileAsyncAndNotExists_ThenReturnsError()
    {
        _caller.Setup(cc => cc.CallerId)
            .Returns("auserid");

        _repository.Setup(rep => rep.FindByUserIdAsync(It.IsAny<Identifier>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Optional<UserProfileRoot>.None);

        var result = await _application.GetProfileAsync(_caller.Object, "auserid", CancellationToken.None);

        result.Should().BeError(ErrorCode.EntityNotFound);
    }

    [Fact]
    public async Task WhenGetProfileAsync_ThenReturnsProfile()
    {
        _caller.Setup(cc => cc.CallerId)
            .Returns("auserid");

        var profile = UserProfileRoot.Create(_recorder.Object, _idFactory.Object, ProfileType.Person, "auserid".ToId(),
            PersonName.Create("afirstname", "alastname").Value).Value;
        _repository.Setup(rep => rep.FindByUserIdAsync(It.IsAny<Identifier>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(profile.ToOptional());

        var result = await _application.GetProfileAsync(_caller.Object, "auserid", CancellationToken.None);

        result.Should().BeSuccess();
        result.Value.Name.FirstName.Should().Be("afirstname");
        result.Value.Name.LastName.Should().Be("alastname");
        result.Value.DisplayName.Should().Be("afirstname");
        result.Value.Timezone.Should().Be(Timezones.Default.ToString());
        result.Value.Address.CountryCode.Should().Be(CountryCodes.Default.ToString());
    }

    [Fact]
    public async Task WhenGetAllProfilesAsyncAndNoIds_ThenReturnsProfiles()
    {
        var result = await _application.GetAllProfilesAsync(_caller.Object, new List<string>(), new GetOptions(),
            CancellationToken.None);

        result.Value.Should().BeEmpty();
    }

    [Fact]
    public async Task WhenGetAllProfilesAsync_ThenReturnsProfiles()
    {
        var profile = UserProfileRoot.Create(_recorder.Object, _idFactory.Object, ProfileType.Person, "auserid".ToId(),
            PersonName.Create("afirstname", "alastname").Value).Value;
        _repository.Setup(rep =>
                rep.SearchAllByUserIdsAsync(It.IsAny<List<Identifier>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<UserProfileRoot>
            {
                profile
            });

        var result = await _application.GetAllProfilesAsync(_caller.Object, new List<string> { "auserid" },
            new GetOptions(), CancellationToken.None);

        result.Value.Count.Should().Be(1);
        result.Value[0].Id.Should().Be("anid");
    }
}