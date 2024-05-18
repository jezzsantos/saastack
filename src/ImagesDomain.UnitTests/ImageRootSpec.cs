using Common;
using Common.Extensions;
using Domain.Common.Identity;
using Domain.Common.ValueObjects;
using Domain.Events.Shared.Images;
using Domain.Interfaces.Entities;
using FluentAssertions;
using Moq;
using UnitTesting.Common;
using Xunit;

namespace ImagesDomain.UnitTests;

[Trait("Category", "Unit")]
public class ImageRootSpec
{
    private readonly Mock<IIdentifierFactory> _idFactory;
    private readonly Mock<IRecorder> _recorder;

    public ImageRootSpec()
    {
        _recorder = new Mock<IRecorder>();
        _idFactory = new Mock<IIdentifierFactory>();
        _idFactory.Setup(idf => idf.Create(It.IsAny<IIdentifiableEntity>()))
            .Returns("anid".ToId());
    }

    [Fact]
    public void WhenCreateAndContentTypeInvalid_ThenReturnsError()
    {
        var result = ImageRoot.Create(_recorder.Object, _idFactory.Object, "auserid".ToId(), "aninvalidcontenttype");

        result.Should().BeError(ErrorCode.Validation,
            Resources.ImageRoot_UnsupportedContentType.Format("aninvalidcontenttype"));
    }

    [Fact]
    public void WhenCreate_ThenAssigned()
    {
        var result = ImageRoot.Create(_recorder.Object, _idFactory.Object, "auserid".ToId(), "image/jpeg");

        result.Value.ContentType.Should().Be("image/jpeg");
        result.Value.Description.Should().BeNone();
        result.Value.Filename.Should().BeNone();
        result.Value.Size.Should().BeNone();
    }

    [Fact]
    public void WhenEnsureInvariantsAndNoContentType_ThenReturnsError()
    {
        var image = ImageRoot.Create(_recorder.Object, _idFactory.Object, "auserid".ToId(), "image/jpeg").Value;
#if TESTINGONLY
        image.TestingOnly_SetContentType(Optional<string>.None);
#endif

        var result = image.EnsureInvariants();

        result.Should().BeError(ErrorCode.RuleViolation, Resources.ImageRoot_MissingContentType);
    }

    [Fact]
    public void WhenChangeDetailsAndDescriptionInvalid_ThenReturnsError()
    {
        var image = ImageRoot.Create(_recorder.Object, _idFactory.Object, "auserid".ToId(), "image/jpeg").Value;

        var result = image.ChangeDetails("aninvaliddescription^");

        result.Should().BeError(ErrorCode.Validation, Resources.ImageRoot_InvalidDescription);
    }

    [Fact]
    public void WhenChangeDetailsAndFilenameInvalid_ThenReturnsError()
    {
        var image = ImageRoot.Create(_recorder.Object, _idFactory.Object, "auserid".ToId(), "image/jpeg").Value;

        var result = image.ChangeDetails(null, "aninvalidfilename^");

        result.Should().BeError(ErrorCode.Validation, Resources.ImageRoot_InvalidFilename);
    }

    [Fact]
    public void WhenChangeDetails_ThenChanges()
    {
        var image = ImageRoot.Create(_recorder.Object, _idFactory.Object, "auserid".ToId(), "image/jpeg").Value;

        var result = image.ChangeDetails("adescription", "afilename");

        result.Should().BeSuccess();
        image.Description.Should().Be("adescription");
        image.Filename.Should().Be("afilename");
        image.Events.Last().Should().BeOfType<DetailsChanged>();
    }

    [Fact]
    public void WhenSetAttributesAndSizeTooLarge_ThenReturnsError()
    {
        var image = ImageRoot.Create(_recorder.Object, _idFactory.Object, "auserid".ToId(), "image/jpeg").Value;

        var size = Validations.Images.MaxSizeInBytes + 1;
        var result = image.SetAttributes(size);

        result.Should().BeError(ErrorCode.Validation, Resources.ImageRoot_ImageSizeExceeded.Format(size));
    }

    [Fact]
    public void WhenSetAttributes_ThenAttributed()
    {
        var image = ImageRoot.Create(_recorder.Object, _idFactory.Object, "auserid".ToId(), "image/jpeg").Value;

        var size = Validations.Images.MaxSizeInBytes - 1;
        var result = image.SetAttributes(size);

        result.Should().BeSuccess();
        image.Size.Should().Be(size);
        image.Events.Last().Should().BeOfType<AttributesChanged>();
    }

    [Fact]
    public void WhenDeleteByAnotherUser_ThenReturnsError()
    {
        var image = ImageRoot.Create(_recorder.Object, _idFactory.Object, "auserid".ToId(), "image/jpeg").Value;

        var result = image.Delete("anotheruserid".ToId());

        result.Should().BeError(ErrorCode.RuleViolation, Resources.ImageRoot_NotCreator);
    }
    
    [Fact]
    public void WhenDelete_ThenDeleted()
    {
        var image = ImageRoot.Create(_recorder.Object, _idFactory.Object, "auserid".ToId(), "image/jpeg").Value;

        var result = image.Delete("auserid".ToId());

        result.Should().BeSuccess();
        image.Events.Last().Should().BeOfType<Deleted>();
    }
}