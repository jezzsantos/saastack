using Application.Interfaces;
using Application.Resources.Shared;
using Application.Services.Shared;
using Common;
using Domain.Common.Identity;
using Domain.Common.ValueObjects;
using Domain.Interfaces.Entities;
using Domain.Shared;
using Domain.Shared.EndUsers;
using EndUsersDomain;
using Moq;
using UnitTesting.Common;
using UserProfilesApplication.Persistence;
using UserProfilesDomain;
using Xunit;
using Events = EndUsersDomain.Events;
using PersonName = Domain.Shared.PersonName;

namespace UserProfilesApplication.UnitTests;

[Trait("Category", "Unit")]
public class UserProfileApplicationDomainEventHandlersSpec
{
    private readonly UserProfilesApplication _application;
    private readonly Mock<IAvatarService> _avatarService;
    private readonly Mock<ICallerContext> _caller;
    private readonly Mock<IIdentifierFactory> _idFactory;
    private readonly Mock<IImagesService> _imagesService;
    private readonly Mock<IRecorder> _recorder;
    private readonly Mock<IUserProfileRepository> _repository;

    public UserProfileApplicationDomainEventHandlersSpec()
    {
        _recorder = new Mock<IRecorder>();
        _caller = new Mock<ICallerContext>();
        _idFactory = new Mock<IIdentifierFactory>();
        _idFactory.Setup(idf => idf.Create(It.IsAny<IIdentifiableEntity>()))
            .Returns("anid".ToId());
        _imagesService = new Mock<IImagesService>();
        _avatarService = new Mock<IAvatarService>();
        _avatarService.Setup(avs =>
                avs.FindAvatarAsync(It.IsAny<ICallerContext>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Optional<FileUpload>.None);
        _repository = new Mock<IUserProfileRepository>();
        _repository.Setup(rep => rep.FindByUserIdAsync(It.IsAny<Identifier>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Optional<UserProfileRoot>.None);
        _repository.Setup(rep => rep.FindByEmailAddressAsync(It.IsAny<EmailAddress>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Optional<UserProfileRoot>.None);
        _repository.Setup(rep => rep.SaveAsync(It.IsAny<UserProfileRoot>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((UserProfileRoot root, CancellationToken _) => root);

        _application = new UserProfilesApplication(_recorder.Object, _idFactory.Object, _imagesService.Object,
            _avatarService.Object, _repository.Object);
    }

    [Fact]
    public async Task WhenHandleEndUserRegisteredAsyncForPersonWButNoEmail_ThenReturnsError()
    {
        var domainEvent = Events.Registered("apersonid".ToId(), EndUserProfile.Create("afirstname").Value,
            Optional<EmailAddress>.None, UserClassification.Person, UserAccess.Enabled,
            UserStatus.Registered, Roles.Empty, Features.Empty);

        var result = await _application.HandleEndUserRegisteredAsync(_caller.Object, domainEvent,
            CancellationToken.None);

        result.Should().BeError(ErrorCode.RuleViolation, Resources.UserProfilesApplication_PersonMustHaveEmailAddress);
    }

    [Fact]
    public async Task WhenHandleEndUserRegisteredAsyncForAnyAndExistsForUserId_ThenReturnsError()
    {
        var user = UserProfileRoot.Create(_recorder.Object, _idFactory.Object, ProfileType.Person, "auserid".ToId(),
            PersonName.Create("afirstname", "alastname").Value).Value;
        _repository.Setup(rep => rep.FindByUserIdAsync(It.IsAny<Identifier>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(user.ToOptional());
        var domainEvent = Events.Registered("apersonid".ToId(), EndUserProfile.Create("afirstname").Value,
            EmailAddress.Create("auser@company.com").Value, UserClassification.Person, UserAccess.Enabled,
            UserStatus.Registered, Roles.Empty, Features.Empty);

        var result = await _application.HandleEndUserRegisteredAsync(_caller.Object, domainEvent,
            CancellationToken.None);

        result.Should().BeError(ErrorCode.EntityExists, Resources.UserProfilesApplication_ProfileExistsForUser);
    }

    [Fact]
    public async Task WhenHandleEndUserRegisteredAsyncForAnyAndExistsForEmailAddress_ThenReturnsError()
    {
        var user = UserProfileRoot.Create(_recorder.Object, _idFactory.Object, ProfileType.Person, "auserid".ToId(),
            PersonName.Create("afirstname", "alastname").Value).Value;
        _repository.Setup(rep => rep.FindByEmailAddressAsync(It.IsAny<EmailAddress>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(user.ToOptional());
        var domainEvent = Events.Registered("apersonid".ToId(), EndUserProfile.Create("afirstname").Value,
            EmailAddress.Create("auser@company.com").Value, UserClassification.Person, UserAccess.Enabled,
            UserStatus.Registered, Roles.Empty, Features.Empty);

        var result =
            await _application.HandleEndUserRegisteredAsync(_caller.Object, domainEvent, CancellationToken.None);

        result.Should().BeError(ErrorCode.EntityExists, Resources.UserProfilesApplication_ProfileExistsForEmailAddress);
    }

    [Fact]
    public async Task WhenHandleEndUserRegisteredAsyncForMachine_ThenCreatesProfile()
    {
        var domainEvent = Events.Registered("amachineid".ToId(), EndUserProfile.Create("afirstname").Value,
            EmailAddress.Create("amachine@company.com").Value, UserClassification.Machine, UserAccess.Enabled,
            UserStatus.Registered, Roles.Empty, Features.Empty);

        var result =
            await _application.HandleEndUserRegisteredAsync(_caller.Object, domainEvent, CancellationToken.None);

        result.Should().BeSuccess();
        _avatarService.Verify(
            avs => avs.FindAvatarAsync(It.IsAny<ICallerContext>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
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
            && up.Avatar.HasValue == false
        ), It.IsAny<CancellationToken>()));
    }

    [Fact]
    public async Task WhenHandleEndUserRegisteredAsyncForPerson_ThenCreatesProfile()
    {
        var domainEvent = Events.Registered("apersonid".ToId(), EndUserProfile.Create("afirstname", "alastname").Value,
            EmailAddress.Create("auser@company.com").Value, UserClassification.Person, UserAccess.Enabled,
            UserStatus.Registered, Roles.Empty, Features.Empty);

        var result =
            await _application.HandleEndUserRegisteredAsync(_caller.Object, domainEvent, CancellationToken.None);

        result.Should().BeSuccess();
        _avatarService.Verify(avs =>
            avs.FindAvatarAsync(_caller.Object, "auser@company.com", It.IsAny<CancellationToken>()));
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
            && up.Avatar.HasValue == false
        ), It.IsAny<CancellationToken>()));
    }

    [Fact]
    public async Task WhenHandleEndUserRegisteredAsyncForPersonAndHasDefaultAvatar_ThenCreatesProfile()
    {
        var domainEvent = Events.Registered("apersonid".ToId(), EndUserProfile.Create("afirstname", "alastname").Value,
            EmailAddress.Create("auser@company.com").Value, UserClassification.Person, UserAccess.Enabled,
            UserStatus.Registered, Roles.Empty, Features.Empty);
        var upload = new FileUpload
        {
            Content = new MemoryStream(),
            ContentType = "acontenttype",
            Filename = null,
            Size = 0
        };
        _avatarService.Setup(avs =>
                avs.FindAvatarAsync(It.IsAny<ICallerContext>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(upload.ToOptional());
        _imagesService.Setup(isv =>
                isv.CreateImageAsync(It.IsAny<ICallerContext>(), It.IsAny<FileUpload>(), It.IsAny<string>(),
                    It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Image
            {
                ContentType = "acontenttype",
                Description = "adescription",
                Filename = "afilename",
                Url = "aurl",
                Id = "animageid"
            });

        var result =
            await _application.HandleEndUserRegisteredAsync(_caller.Object, domainEvent, CancellationToken.None);

        result.Should().BeSuccess();
        _avatarService.Verify(avs =>
            avs.FindAvatarAsync(_caller.Object, "auser@company.com", It.IsAny<CancellationToken>()));
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
            && up.Avatar.Value.ImageId == "animageid"
            && up.Avatar.Value.Url == "aurl"
        ), It.IsAny<CancellationToken>()));
    }

    [Fact]
    public async Task WhenHandleEndUserDefaultOrganizationChangedAsync_ThenSetsDefaultOrganization()
    {
        var domainEvent = Events.MembershipDefaultChanged("auserid".ToId(), "amembershipid".ToId(),
            "amembershipid".ToId(), "anorganizationid".ToId(), Roles.Empty, Features.Empty);
        var user = UserProfileRoot.Create(_recorder.Object, _idFactory.Object, ProfileType.Person, "auserid".ToId(),
            PersonName.Create("afirstname", "alastname").Value).Value;
        _repository.Setup(rep => rep.FindByUserIdAsync(It.IsAny<Identifier>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(user.ToOptional());

        var result = await _application.HandleEndUserDefaultOrganizationChangedAsync(_caller.Object,
            domainEvent, CancellationToken.None);

        result.Should().BeSuccess();
        _repository.Verify(rep => rep.SaveAsync(It.Is<UserProfileRoot>(up =>
            up.UserId == "auserid".ToId()
            && up.DefaultOrganizationId == "anorganizationid".ToId()
        ), It.IsAny<CancellationToken>()));
        _repository.Verify(rep => rep.FindByUserIdAsync("auserid".ToId(), It.IsAny<CancellationToken>()));
    }
}