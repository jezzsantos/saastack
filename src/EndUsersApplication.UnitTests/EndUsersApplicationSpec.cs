using Application.Interfaces;
using Application.Resources.Shared;
using Common;
using Domain.Common.Identity;
using Domain.Common.ValueObjects;
using Domain.Interfaces.Entities;
using EndUsersApplication.Persistence;
using EndUsersDomain;
using FluentAssertions;
using Moq;
using UnitTesting.Common;
using Xunit;

namespace EndUsersApplication.UnitTests;

[Trait("Category", "Unit")]
public class EndUsersApplicationSpec
{
    private readonly EndUsersApplication _application;
    private readonly Mock<ICallerContext> _caller;
    private readonly Mock<IIdentifierFactory> _idFactory;
    private readonly Mock<IRecorder> _recorder;
    private readonly Mock<IEndUserRepository> _repository;

    public EndUsersApplicationSpec()
    {
        _recorder = new Mock<IRecorder>();
        _caller = new Mock<ICallerContext>();
        _idFactory = new Mock<IIdentifierFactory>();
        _idFactory.Setup(idf => idf.Create(It.IsAny<IIdentifiableEntity>()))
            .Returns("anid".ToId());
        _repository = new Mock<IEndUserRepository>();
        _repository.Setup(rep => rep.SaveAsync(It.IsAny<EndUserRoot>(), It.IsAny<CancellationToken>()))
            .Returns((EndUserRoot root, CancellationToken _) => Task.FromResult<Result<EndUserRoot, Error>>(root));
        _application = new EndUsersApplication(_recorder.Object, _idFactory.Object, _repository.Object);
    }

    [Fact]
    public async Task WhenGetPersonAsync_ThenReturnsUser()
    {
        var user = EndUserRoot.Create(_recorder.Object, _idFactory.Object, UserClassification.Person).Value;
        _repository.Setup(rep => rep.LoadAsync(It.IsAny<Identifier>(), It.IsAny<CancellationToken>()))
            .Returns(Task.FromResult<Result<EndUserRoot, Error>>(user));

        var result = await _application.GetPersonAsync(_caller.Object, "anid", CancellationToken.None);

        result.Should().BeSuccess();
        result.Value.Id.Should().Be("anid");
        result.Value.Access.Should().Be(EndUserAccess.Enabled);
        result.Value.Status.Should().Be(EndUserStatus.Unregistered);
        result.Value.Classification.Should().Be(EndUserClassification.Person);
        result.Value.Roles.Should().BeEmpty();
        result.Value.FeatureLevels.Should().BeEmpty();
    }

    [Fact]
    public async Task WhenRegisterPersonAsyncAndNotAcceptedTerms_ThenReturnsError()
    {
        var result = await _application.RegisterPersonAsync(_caller.Object, "anemailaddress", "afirstname", "alastname",
            "atimezone", "acountrycode", false, CancellationToken.None);

        result.Should().BeError(ErrorCode.RuleViolation, Resources.EndUsersApplication_NotAcceptedTerms);
    }

    [Fact]
    public async Task WhenRegisterPersonAsync_ThenRegisters()
    {
        var result = await _application.RegisterPersonAsync(_caller.Object, "auser@company.com", "afirstname",
            "alastname",
            Timezones.Default.ToString(), CountryCodes.Default.ToString(), true, CancellationToken.None);

        result.Should().BeSuccess();
        result.Value.Id.Should().Be("anid");
        result.Value.Access.Should().Be(EndUserAccess.Enabled);
        result.Value.Status.Should().Be(EndUserStatus.Registered);
        result.Value.Classification.Should().Be(EndUserClassification.Person);
        result.Value.Roles.Should().BeEmpty();
        result.Value.FeatureLevels.Should().BeEmpty();
        result.Value.Profile!.Id.Should().Be("anid");
        result.Value.Profile.DefaultOrganisationId.Should().BeNull();
        result.Value.Profile.Address!.CountryCode.Should().Be(CountryCodes.Default.ToString());
        result.Value.Profile.Name.FirstName.Should().Be("afirstname");
        result.Value.Profile.Name.LastName.Should().Be("alastname");
        result.Value.Profile.DisplayName.Should().Be("afirstname");
        result.Value.Profile.EmailAddress.Should().Be("auser@company.com");
        result.Value.Profile.Timezone.Should().Be(Timezones.Default.ToString());
    }

    [Fact]
    public async Task WhenRegisterMachineAsync_ThenRegisters()
    {
        var result = await _application.RegisterMachineAsync(_caller.Object, "aname", Timezones.Default.ToString(),
            CountryCodes.Default.ToString(), CancellationToken.None);

        result.Should().BeSuccess();
        result.Value.Id.Should().Be("anid");
        result.Value.Access.Should().Be(EndUserAccess.Enabled);
        result.Value.Status.Should().Be(EndUserStatus.Registered);
        result.Value.Classification.Should().Be(EndUserClassification.Machine);
        result.Value.Roles.Should().BeEmpty();
        result.Value.FeatureLevels.Should().BeEmpty();
        result.Value.Profile!.Id.Should().Be("anid");
        result.Value.Profile.DefaultOrganisationId.Should().BeNull();
        result.Value.Profile.Address!.CountryCode.Should().Be(CountryCodes.Default.ToString());
        result.Value.Profile.Name.FirstName.Should().Be("aname");
        result.Value.Profile.Name.LastName.Should().BeNull();
        result.Value.Profile.DisplayName.Should().Be("aname");
        result.Value.Profile.EmailAddress.Should().BeNull();
        result.Value.Profile.Timezone.Should().Be(Timezones.Default.ToString());
    }
}