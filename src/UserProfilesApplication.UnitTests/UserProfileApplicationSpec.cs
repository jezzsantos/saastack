using Application.Interfaces;
using Application.Resources.Shared;
using Application.Services.Shared;
using Common;
using Domain.Common.Identity;
using Domain.Common.ValueObjects;
using Domain.Interfaces;
using Domain.Interfaces.Authorization;
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
    private readonly Mock<IImagesService> _imagesService;
    private readonly Mock<IRecorder> _recorder;
    private readonly Mock<IUserProfileRepository> _repository;

    public UserProfileApplicationSpec()
    {
        _recorder = new Mock<IRecorder>();
        _caller = new Mock<ICallerContext>();
        _idFactory = new Mock<IIdentifierFactory>();
        _idFactory.Setup(idf => idf.Create(It.IsAny<IIdentifiableEntity>()))
            .Returns("anid".ToId());
        _imagesService = new Mock<IImagesService>();
        var avatarService = new Mock<IAvatarService>();
        _repository = new Mock<IUserProfileRepository>();
        _repository.Setup(rep => rep.FindByUserIdAsync(It.IsAny<Identifier>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Optional<UserProfileRoot>.None);
        _repository.Setup(rep => rep.FindByEmailAddressAsync(It.IsAny<EmailAddress>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Optional<UserProfileRoot>.None);
        _repository.Setup(rep => rep.SaveAsync(It.IsAny<UserProfileRoot>(), It.IsAny<CancellationToken>()))
            .Returns((UserProfileRoot root, CancellationToken _) =>
                Task.FromResult<Result<UserProfileRoot, Error>>(root));

        _application = new UserProfilesApplication(_recorder.Object, _idFactory.Object, _imagesService.Object,
            avatarService.Object, _repository.Object);
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
            .Returns("anotheruserid");
        var user = UserProfileRoot.Create(_recorder.Object, _idFactory.Object, ProfileType.Person, "auserid".ToId(),
            PersonName.Create("afirstname", "alastname").Value).Value;
        _repository.Setup(rep => rep.FindByUserIdAsync(It.IsAny<Identifier>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(user.ToOptional());

        var result = await _application.ChangeProfileAsync(_caller.Object, "auserid", "afirstname", "alastname",
            "adisplayname", "aphonenumber", "atimezone", CancellationToken.None);

        result.Should().BeError(ErrorCode.RoleViolation);
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
    public async Task WhenGetProfileAsyncByServiceAccount_ThenReturnsProfile()
    {
        _caller.Setup(cc => cc.IsServiceAccount)
            .Returns(true);

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
    public async Task WhenGetProfileAsyncByOwner_ThenReturnsProfile()
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

        var result = await _application.GetAllProfilesAsync(_caller.Object, ["auserid"],
            new GetOptions(), CancellationToken.None);

        result.Value.Count.Should().Be(1);
        result.Value[0].Id.Should().Be("anid");
    }

    [Fact]
    public async Task WhenChangeProfileAvatarAsyncAndNotExists_ThenReturnsError()
    {
        _caller.Setup(cc => cc.CallerId)
            .Returns("auserid");
        var upload = new FileUpload
        {
            Content = new MemoryStream(),
            ContentType = new FileUploadContentType { MediaType = "acontenttype" },
            Filename = null,
            Size = 0
        };

        var result =
            await _application.ChangeProfileAvatarAsync(_caller.Object, "auserid", upload, CancellationToken.None);

        result.Should().BeError(ErrorCode.EntityNotFound);
    }

    [Fact]
    public async Task WhenChangeProfileAvatarAsyncAndNotOwner_ThenReturnsError()
    {
        _caller.Setup(cc => cc.CallerId)
            .Returns("anotheruserid");
        var upload = new FileUpload
        {
            Content = new MemoryStream(),
            ContentType = new FileUploadContentType { MediaType = "acontenttype" },
            Filename = null,
            Size = 0
        };
        var user = UserProfileRoot.Create(_recorder.Object, _idFactory.Object, ProfileType.Person, "auserid".ToId(),
            PersonName.Create("afirstname", "alastname").Value).Value;
        _repository.Setup(rep => rep.FindByUserIdAsync(It.IsAny<Identifier>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(user.ToOptional());

        var result =
            await _application.ChangeProfileAvatarAsync(_caller.Object, "auserid", upload, CancellationToken.None);

        result.Should().BeError(ErrorCode.RoleViolation);
    }

    [Fact]
    public async Task WhenChangeProfileAvatarAsyncAndNoExistingAvatar_ThenReturnsProfile()
    {
        _caller.Setup(cc => cc.CallerId)
            .Returns("auserid");
        var upload = new FileUpload
        {
            Content = new MemoryStream(),
            ContentType = new FileUploadContentType { MediaType = "acontenttype" },
            Filename = null,
            Size = 0
        };
        var user = UserProfileRoot.Create(_recorder.Object, _idFactory.Object, ProfileType.Person, "auserid".ToId(),
            PersonName.Create("afirstname", "alastname").Value).Value;
        _repository.Setup(rep => rep.FindByUserIdAsync(It.IsAny<Identifier>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(user.ToOptional());
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
            await _application.ChangeProfileAvatarAsync(_caller.Object, "auserid", upload, CancellationToken.None);

        result.Should().BeSuccess();
        result.Value.AvatarUrl.Should().Be("aurl");
        _repository.Verify(rep => rep.SaveAsync(It.Is<UserProfileRoot>(profile =>
            profile.Avatar.Value.ImageId == "animageid".ToId()
            && profile.Avatar.Value.Url == "aurl"
        ), It.IsAny<CancellationToken>()));
        _imagesService.Verify(isv =>
            isv.CreateImageAsync(_caller.Object, upload, "afirstname", It.IsAny<CancellationToken>()));
        _imagesService.Verify(
            isv => isv.DeleteImageAsync(It.IsAny<ICallerContext>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task WhenChangeProfileAvatarAsyncAndExistingAvatar_ThenReturnsProfile()
    {
        _caller.Setup(cc => cc.CallerId)
            .Returns("auserid");
        var upload = new FileUpload
        {
            Content = new MemoryStream(),
            ContentType = new FileUploadContentType { MediaType = "acontenttype" },
            Filename = null,
            Size = 0
        };
        var user = UserProfileRoot.Create(_recorder.Object, _idFactory.Object, ProfileType.Person, "auserid".ToId(),
            PersonName.Create("afirstname", "alastname").Value).Value;
        await user.ChangeAvatarAsync("auserid".ToId(),
            _ => Task.FromResult<Result<Avatar, Error>>(Avatar.Create("anoldimageid".ToId(), "aurl").Value),
            _ => Task.FromResult(Result.Ok));
        _repository.Setup(rep => rep.FindByUserIdAsync(It.IsAny<Identifier>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(user.ToOptional());
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
            await _application.ChangeProfileAvatarAsync(_caller.Object, "auserid", upload, CancellationToken.None);

        result.Should().BeSuccess();
        result.Value.AvatarUrl.Should().Be("aurl");
        _repository.Verify(rep => rep.SaveAsync(It.Is<UserProfileRoot>(profile =>
            profile.Avatar.Value.ImageId == "animageid".ToId()
            && profile.Avatar.Value.Url == "aurl"
        ), It.IsAny<CancellationToken>()));
        _imagesService.Verify(isv =>
            isv.CreateImageAsync(_caller.Object, upload, "afirstname", It.IsAny<CancellationToken>()));
        _imagesService.Verify(
            isv => isv.DeleteImageAsync(_caller.Object, "anoldimageid", It.IsAny<CancellationToken>()));
    }

    [Fact]
    public async Task WhenDeleteProfileAvatarAsyncAndNotExists_ThenReturnsError()
    {
        _caller.Setup(cc => cc.CallerId)
            .Returns("auserid");

        var result =
            await _application.DeleteProfileAvatarAsync(_caller.Object, "auserid", CancellationToken.None);

        result.Should().BeError(ErrorCode.EntityNotFound);
    }

    [Fact]
    public async Task WhenDeleteProfileAvatarAsyncAndNotOwner_ThenReturnsError()
    {
        _caller.Setup(cc => cc.CallerId)
            .Returns("anotheruserid");
        var user = UserProfileRoot.Create(_recorder.Object, _idFactory.Object, ProfileType.Person, "auserid".ToId(),
            PersonName.Create("afirstname", "alastname").Value).Value;
        _repository.Setup(rep => rep.FindByUserIdAsync(It.IsAny<Identifier>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(user.ToOptional());

        var result =
            await _application.DeleteProfileAvatarAsync(_caller.Object, "auserid", CancellationToken.None);

        result.Should().BeError(ErrorCode.RoleViolation);
    }

    [Fact]
    public async Task WhenDeleteProfileAvatarAsync_ThenReturnsProfile()
    {
        _caller.Setup(cc => cc.CallerId)
            .Returns("auserid");
        var user = UserProfileRoot.Create(_recorder.Object, _idFactory.Object, ProfileType.Person, "auserid".ToId(),
            PersonName.Create("afirstname", "alastname").Value).Value;
        await user.ChangeAvatarAsync("auserid".ToId(),
            _ => Task.FromResult<Result<Avatar, Error>>(Avatar.Create("anoldimageid".ToId(), "aurl").Value),
            _ => Task.FromResult(Result.Ok));
        _repository.Setup(rep => rep.FindByUserIdAsync(It.IsAny<Identifier>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(user.ToOptional());
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
            await _application.DeleteProfileAvatarAsync(_caller.Object, "auserid", CancellationToken.None);

        result.Should().BeSuccess();
        result.Value.AvatarUrl.Should().BeNull();
        _repository.Verify(rep => rep.SaveAsync(It.Is<UserProfileRoot>(profile =>
            profile.Avatar.HasValue == false
        ), It.IsAny<CancellationToken>()));
        _imagesService.Verify(
            isv => isv.DeleteImageAsync(_caller.Object, "anoldimageid", It.IsAny<CancellationToken>()));
    }

    [Fact]
    public async Task WhenGetCurrentUserProfileAsyncAndNotAuthenticated_ThenReturnsAnonymousProfile()
    {
        _caller.Setup(cc => cc.IsAuthenticated)
            .Returns(false);

        var result = await _application.GetCurrentUserProfileAsync(_caller.Object, CancellationToken.None);

        result.Value.IsAuthenticated.Should().BeFalse();
        result.Value.Id.Should().Be(CallerConstants.AnonymousUserId);
        result.Value.UserId.Should().Be(CallerConstants.AnonymousUserId);
        result.Value.Address.CountryCode.Should().Be(CountryCodes.Default.ToString());
        result.Value.Roles.Should().BeEmpty();
        result.Value.Features.Should().BeEmpty();
    }

    [Fact]
    public async Task WhenGetCurrentUserProfileAsyncAndAuthenticated_ThenReturnsProfile()
    {
        _caller.Setup(cc => cc.IsAuthenticated)
            .Returns(true);
        var roles = new ICallerContext.CallerRoles(new[] { PlatformRoles.Standard }, []);
        var features = new ICallerContext.CallerFeatures(new[] { PlatformFeatures.Basic }, []);
        _caller.Setup(cc => cc.Roles)
            .Returns(roles);
        _caller.Setup(cc => cc.Features)
            .Returns(features);
        var profile = UserProfileRoot.Create(_recorder.Object, _idFactory.Object, ProfileType.Person, "auserid".ToId(),
            PersonName.Create("afirstname", "alastname").Value).Value;
        _repository.Setup(rep => rep.FindByUserIdAsync(It.IsAny<Identifier>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(profile.ToOptional());

        var result = await _application.GetCurrentUserProfileAsync(_caller.Object, CancellationToken.None);

        result.Value.IsAuthenticated.Should().BeTrue();
        result.Value.Id.Should().Be("anid");
        result.Value.UserId.Should().Be("auserid");
        result.Value.Name.FirstName.Should().Be("afirstname");
        result.Value.Name.LastName.Should().Be("alastname");
        result.Value.DisplayName.Should().Be("afirstname");
        result.Value.Timezone.Should().Be(Timezones.Default.ToString());
        result.Value.Address.CountryCode.Should().Be(CountryCodes.Default.ToString());
        result.Value.Roles.Should().ContainInOrder(PlatformRoles.Standard.Name);
        result.Value.Features.Should().ContainInOrder(PlatformFeatures.Basic.Name);
    }
}