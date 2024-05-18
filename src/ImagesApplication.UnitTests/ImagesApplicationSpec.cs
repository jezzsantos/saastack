using Application.Interfaces;
using Application.Interfaces.Services;
using Application.Persistence.Interfaces;
using Application.Resources.Shared;
using Common;
using Domain.Common.Identity;
using Domain.Common.ValueObjects;
using Domain.Interfaces.Entities;
using FluentAssertions;
using ImagesApplication.Persistence;
using ImagesDomain;
using Moq;
using UnitTesting.Common;
using Xunit;

namespace ImagesApplication.UnitTests;

[Trait("Category", "Unit")]
public class ImagesApplicationSpec
{
    private readonly ImagesApplication _application;
    private readonly Mock<ICallerContext> _caller;
    private readonly Mock<IIdentifierFactory> _idFactory;
    private readonly Mock<IRecorder> _recorder;
    private readonly Mock<IImagesRepository> _repository;

    public ImagesApplicationSpec()
    {
        _caller = new Mock<ICallerContext>();
        _recorder = new Mock<IRecorder>();
        _idFactory = new Mock<IIdentifierFactory>();
        _idFactory.Setup(idf => idf.Create(It.IsAny<IIdentifiableEntity>()))
            .Returns("anid".ToId());
        var hostSettings = new Mock<IHostSettings>();
        hostSettings.Setup(hs => hs.MakeImagesApiGetUrl(It.IsAny<string>()))
            .Returns("aurl");
        _repository = new Mock<IImagesRepository>();
        _repository.Setup(rep => rep.SaveAsync(It.IsAny<ImageRoot>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((ImageRoot root, CancellationToken _) => root);

        _application =
            new ImagesApplication(_recorder.Object, _idFactory.Object, hostSettings.Object, _repository.Object);
    }

    [Fact]
    public async Task WhenDeleteImageAsyncAndNotExists_ThenReturnsError()
    {
        _repository.Setup(rep => rep.LoadAsync(It.IsAny<Identifier>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Error.EntityNotFound());

        var result = await _application.DeleteImageAsync(_caller.Object, "animageid", CancellationToken.None);

        result.Should().BeError(ErrorCode.EntityNotFound);
    }

    [Fact]
    public async Task WhenDeleteImageAsync_ThenDeletes()
    {
        _caller.Setup(cc => cc.CallerId).Returns("auserid");
        var image = ImageRoot.Create(_recorder.Object, _idFactory.Object, "auserid".ToId(), "image/jpeg");
        _repository.Setup(rep => rep.LoadAsync(It.IsAny<Identifier>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(image);

        var result = await _application.DeleteImageAsync(_caller.Object, "animageid", CancellationToken.None);

        result.Should().BeSuccess();
    }

    [Fact]
    public async Task WhenDownloadImageAsyncAndNotExists_ThenReturnsError()
    {
        _repository.Setup(rep => rep.LoadAsync(It.IsAny<Identifier>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Error.EntityNotFound());

        var result = await _application.DownloadImageAsync(_caller.Object, "animageid", CancellationToken.None);

        result.Should().BeError(ErrorCode.EntityNotFound);
    }

    [Fact]
    public async Task WhenDownloadImageAsync_ThenReturnsImage()
    {
        var image = ImageRoot.Create(_recorder.Object, _idFactory.Object, "auserid".ToId(), "image/jpeg");
        _repository.Setup(rep => rep.LoadAsync(It.IsAny<Identifier>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(image);
        var blob = new Blob { ContentType = "image/jpeg" };
        _repository.Setup(rep =>
                rep.DownloadImageAsync(It.IsAny<Identifier>(), It.IsAny<Stream>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(blob);

        var result = await _application.DownloadImageAsync(_caller.Object, "animageid", CancellationToken.None);

        result.Value.Stream.Should().NotBeNull();
        result.Value.ContentType.Should().Be("image/jpeg");
        _repository.Verify(rep =>
            rep.DownloadImageAsync("anid".ToId(), It.IsAny<Stream>(), It.IsAny<CancellationToken>()));
    }

    [Fact]
    public async Task WhenGetImageAsyncAndNotExists_ThenReturnsError()
    {
        _repository.Setup(rep => rep.LoadAsync(It.IsAny<Identifier>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Error.EntityNotFound());

        var result = await _application.GetImageAsync(_caller.Object, "animageid", CancellationToken.None);

        result.Should().BeError(ErrorCode.EntityNotFound);
    }

    [Fact]
    public async Task WhenGetImageAsync_ThenReturns()
    {
        var image = ImageRoot.Create(_recorder.Object, _idFactory.Object, "auserid".ToId(), "image/jpeg").Value;
        image.ChangeDetails("adescription", "afilename");
        _repository.Setup(rep => rep.LoadAsync(It.IsAny<Identifier>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(image);

        var result = await _application.GetImageAsync(_caller.Object, "animageid", CancellationToken.None);

        result.Should().BeSuccess();
        result.Value.ContentType.Should().Be("image/jpeg");
        result.Value.Description.Should().Be("adescription");
        result.Value.Filename.Should().Be("afilename");
        result.Value.Url.Should().Be("aurl");
    }

    [Fact]
    public async Task WhenUpdateImageAsyncAndNotExists_ThenReturnsError()
    {
        _repository.Setup(rep => rep.LoadAsync(It.IsAny<Identifier>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Error.EntityNotFound());

        var result =
            await _application.UpdateImageAsync(_caller.Object, "animageid", "adescription", CancellationToken.None);

        result.Should().BeError(ErrorCode.EntityNotFound);
    }

    [Fact]
    public async Task WhenUpdateImageAsyncWithNewDescription_ThenReturns()
    {
        var image = ImageRoot.Create(_recorder.Object, _idFactory.Object, "auserid".ToId(), "image/jpeg").Value;
        image.ChangeDetails("adescription", "afilename");
        _repository.Setup(rep => rep.LoadAsync(It.IsAny<Identifier>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(image);

        var result =
            await _application.UpdateImageAsync(_caller.Object, "animageid", "anewdescription", CancellationToken.None);

        result.Should().BeSuccess();
        result.Value.ContentType.Should().Be("image/jpeg");
        result.Value.Description.Should().Be("anewdescription");
        result.Value.Filename.Should().Be("afilename");
        result.Value.Url.Should().Be("aurl");
        _repository.Verify(rep => rep.SaveAsync(It.Is<ImageRoot>(img =>
            img.ContentType == "image/jpeg"
            && img.Description == "anewdescription"
        ), It.IsAny<CancellationToken>()));
    }

    [Fact]
    public async Task WhenUpdateImageAsyncWithNoDescription_ThenReturns()
    {
        var image = ImageRoot.Create(_recorder.Object, _idFactory.Object, "auserid".ToId(), "image/jpeg").Value;
        image.ChangeDetails("adescription", "afilename");
        _repository.Setup(rep => rep.LoadAsync(It.IsAny<Identifier>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(image);

        var result =
            await _application.UpdateImageAsync(_caller.Object, "animageid", null, CancellationToken.None);

        result.Should().BeSuccess();
        result.Value.ContentType.Should().Be("image/jpeg");
        result.Value.Description.Should().Be("adescription");
        result.Value.Filename.Should().Be("afilename");
        result.Value.Url.Should().Be("aurl");
        _repository.Verify(rep => rep.SaveAsync(It.IsAny<ImageRoot>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task WhenUploadImage_ThenReturns()
    {
        var content = new MemoryStream();
        var upload = new FileUpload
        {
            Content = content,
            ContentType = new FileUploadContentType { MediaType = "image/jpeg" },
            Filename = "afilename",
            Size = 99
        };

        var result =
            await _application.UploadImageAsync(_caller.Object, upload, "adescription", CancellationToken.None);

        result.Should().BeSuccess();
        result.Value.ContentType.Should().Be("image/jpeg");
        result.Value.Description.Should().Be("adescription");
        result.Value.Filename.Should().Be("afilename");
        result.Value.Url.Should().Be("aurl");
        _repository.Verify(rep =>
            rep.UploadImageAsync("anid".ToId(), "image/jpeg", content, It.IsAny<CancellationToken>()));
        _repository.Verify(rep => rep.SaveAsync(It.Is<ImageRoot>(img =>
            img.ContentType == "image/jpeg"
            && img.Description == "adescription"
            && img.Filename == "afilename"
        ), It.IsAny<CancellationToken>()));
    }
}