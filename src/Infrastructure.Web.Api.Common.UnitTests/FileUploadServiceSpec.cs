using Application.Resources.Shared;
using Common;
using Common.Extensions;
using FluentAssertions;
using Infrastructure.Web.Api.Interfaces;
using JetBrains.Annotations;
using UnitTesting.Common;
using Xunit;

namespace Infrastructure.Web.Api.Common.UnitTests;

[UsedImplicitly]
public class FileUploadServiceSpec
{
    [Trait("Category", "Unit")]
    public class GivenAnyUpload
    {
        private readonly FileUploadService _service;

        public GivenAnyUpload()
        {
            _service = new FileUploadService();
        }

        [Fact]
        public void WhenGetUploadedFileAndNoFile_ThenReturnsError()
        {
            var result = _service.GetUploadedFile(new List<FileUpload>(), 10, new List<string>());

            result.Should().BeError(ErrorCode.Validation, Resources.FileUploadService_NoFiles);
        }

        [Fact]
        public void WhenGetUploadedFileAndNoContent_ThenReturnsError()
        {
            using var stream = new MemoryStream();
            var uploads = new List<FileUpload>
            {
                new()
                {
                    Content = stream,
                    ContentType = "acontenttype",
                    Size = 0,
                    Filename = "afilename"
                }
            };

            var result = _service.GetUploadedFile(uploads, 1000, new List<string>());

            result.Should().BeError(ErrorCode.Validation,
                Resources.FileUploadService_MissingContent);
        }

        [Fact]
        public void WhenGetUploadedFileAndTooLarge_ThenReturnsError()
        {
            const int maxSize = 1_000_000;
            var content = Enumerable.Repeat((byte)0x01, maxSize + 1)
                .ToArray();
            using var stream = new MemoryStream(content);
            var uploads = new List<FileUpload>
            {
                new()
                {
                    Content = stream,
                    ContentType = "acontenttype",
                    Size = content.Length,
                    Filename = "afilename"
                }
            };

            var result = _service.GetUploadedFile(uploads, maxSize, new List<string>());

            result.Should().BeError(ErrorCode.Validation,
                Resources.FileUploadService_InvalidFileSize.Format(1_000_000));
        }

        [Fact]
        public void WhenGetUploadedFileAndNoAllowedContentTypes_ThenReturnsError()
        {
            var content = new byte[] { 0x0, 0x1 };
            using var stream = new MemoryStream(content);
            var uploads = new List<FileUpload>
            {
                new()
                {
                    Content = stream,
                    ContentType = "acontenttype",
                    Size = content.Length,
                    Filename = "afilename"
                }
            };

            var result = _service.GetUploadedFile(uploads, 1000, new List<string>());

            result.Should().BeError(ErrorCode.Validation,
                Resources.FileUploadService_DisallowedFileContent.Format("acontenttype"));
        }
    }

    [Trait("Category", "Unit")]
    public class GivenUnknownFile
    {
        private static readonly byte[] UnknownFileBytes =
        [
            0x00, 0x01
        ];
        private readonly FileUploadService _service;

        public GivenUnknownFile()
        {
            _service = new FileUploadService();
        }

        [Fact]
        public void WhenGetUploadedFileAndNoContentType_ThenReturnsError()
        {
            var content = UnknownFileBytes;
            using var stream = new MemoryStream(content);
            var uploads = new List<FileUpload>
            {
                new()
                {
                    Content = stream,
                    ContentType = null!,
                    Size = content.Length,
                    Filename = "afilename"
                }
            };

            var result = _service.GetUploadedFile(uploads, 100, new List<string> { "allowed" });

            result.Should().BeError(ErrorCode.Validation,
                Resources.FileUploadService_DisallowedFileContent.Format(FileUploadService.UnknownContentType));
        }

        [Fact]
        public void WhenGetUploadedFileAndNotAllowedContentType_ThenReturnsError()
        {
            var content = UnknownFileBytes;
            using var stream = new MemoryStream(content);
            var uploads = new List<FileUpload>
            {
                new()
                {
                    Content = stream,
                    ContentType = "notallowed",
                    Size = content.Length,
                    Filename = "afilename"
                }
            };

            var result = _service.GetUploadedFile(uploads, 100, new List<string> { "allowed" });

            result.Should().BeError(ErrorCode.Validation,
                Resources.FileUploadService_DisallowedFileContent.Format("notallowed"));
        }

        [Fact]
        public void WhenGetUploadedFileAndAllowedContentType_ThenReturns()
        {
            var content = UnknownFileBytes;
            using var stream = new MemoryStream(content);
            var uploads = new List<FileUpload>
            {
                new()
                {
                    Content = stream,
                    ContentType = "allowed",
                    Size = content.Length,
                    Filename = "afilename"
                }
            };

            var result = _service.GetUploadedFile(uploads, 100, new List<string> { "allowed" });

            result.Should().BeSuccess();
            result.Value.ContentType.Should().Be("allowed");
            result.Value.Filename.Should().Be("afilename");
            result.Value.Size.Should().Be(2);
            result.Value.Content.Position.Should().Be(0);
        }
    }

    [Trait("Category", "Unit")]
    public class GivenKnownFile
    {
        private readonly FileUploadService _service;

        public GivenKnownFile()
        {
            _service = new FileUploadService();
        }

        [Fact]
        public void WhenGetUploadedFileAndNotAllowedContentType_ReturnsError()
        {
            var content = FileUploadService.ImageJpegMagicBytes.Concat(Enumerable.Repeat((byte)0x01, 100))
                .ToArray();
            using var stream = new MemoryStream(content);
            var uploads = new List<FileUpload>
            {
                new()
                {
                    Content = stream,
                    ContentType = HttpConstants.ContentTypes.ImageJpeg,
                    Size = content.Length,
                    Filename = "afilename"
                }
            };

            var result = _service.GetUploadedFile(uploads, 1000, new List<string> { "allowed" });

            result.Should().BeError(ErrorCode.Validation,
                Resources.FileUploadService_DisallowedFileContent.Format(HttpConstants.ContentTypes.ImageJpeg));
        }

        [Fact]
        public void WhenGetUploadedFileAndContentTypeAndContentDiffers_ReturnsError()
        {
            var content = FileUploadService.ImageJpegMagicBytes.Concat(Enumerable.Repeat((byte)0x01, 100))
                .ToArray();
            using var stream = new MemoryStream(content);
            var uploads = new List<FileUpload>
            {
                new()
                {
                    Content = stream,
                    ContentType = HttpConstants.ContentTypes.ImagePng,
                    Size = content.Length,
                    Filename = "afilename"
                }
            };

            var result = _service.GetUploadedFile(uploads, 1000,
                new List<string> { HttpConstants.ContentTypes.ImageJpeg, HttpConstants.ContentTypes.ImagePng });

            result.Should().BeError(ErrorCode.Validation,
                Resources.FileUploadService_InvalidContentTypeForFileType.Format(HttpConstants.ContentTypes.ImageJpeg,
                    HttpConstants.ContentTypes.ImagePng));
        }

        [Fact]
        public void WhenGetUploadedFileAndFilenameExtensionDiffers_ThenReturnsError()
        {
            var content = FileUploadService.ImageJpegMagicBytes.Concat(Enumerable.Repeat((byte)0x01, 100))
                .ToArray();
            using var stream = new MemoryStream(content);
            var uploads = new List<FileUpload>
            {
                new()
                {
                    Content = stream,
                    ContentType = HttpConstants.ContentTypes.ImageJpeg,
                    Size = content.Length,
                    Filename = "afilename.txt"
                }
            };

            var result =
                _service.GetUploadedFile(uploads, 1000, new List<string> { HttpConstants.ContentTypes.ImageJpeg });

            result.Should().BeError(ErrorCode.Validation,
                Resources.FileUploadService_InvalidFileExtensionForFileType);
        }

        [Fact]
        public void WhenGetUploadedFileAndFileExtensionMissing_ThenSetsExtension()
        {
            var content = FileUploadService.ImageJpegMagicBytes.Concat(Enumerable.Repeat((byte)0x01, 100))
                .ToArray();
            using var stream = new MemoryStream(content);
            var uploads = new List<FileUpload>
            {
                new()
                {
                    Content = stream,
                    ContentType = HttpConstants.ContentTypes.ImageJpeg,
                    Size = content.Length,
                    Filename = "afilename"
                }
            };

            var result =
                _service.GetUploadedFile(uploads, 1000, new List<string> { HttpConstants.ContentTypes.ImageJpeg });

            result.Should().BeSuccess();
            result.Value.ContentType.Should().Be(HttpConstants.ContentTypes.ImageJpeg);
            result.Value.Filename.Should().Be("afilename.jpg");
            result.Value.Size.Should().Be(103);
            result.Value.Content.Position.Should().Be(0);
        }

        [Fact]
        public void WhenGetUploadedFileAndContentTypeMissing_ThenSetsContentType()
        {
            var content = FileUploadService.ImageJpegMagicBytes.Concat(Enumerable.Repeat((byte)0x01, 100))
                .ToArray();
            using var stream = new MemoryStream(content);
            var uploads = new List<FileUpload>
            {
                new()
                {
                    Content = stream,
                    ContentType = null!,
                    Size = content.Length,
                    Filename = "afilename.jpg"
                }
            };

            var result =
                _service.GetUploadedFile(uploads, 1000, new List<string> { HttpConstants.ContentTypes.ImageJpeg });

            result.Should().BeSuccess();
            result.Value.ContentType.Should().Be(HttpConstants.ContentTypes.ImageJpeg);
            result.Value.Filename.Should().Be("afilename.jpg");
            result.Value.Size.Should().Be(103L);
            result.Value.Content.Position.Should().Be(0);
        }

        [Fact]
        public void WhenGetUploaded_ThenReturns()
        {
            var content = FileUploadService.ImageJpegMagicBytes.Concat(Enumerable.Repeat((byte)0x01, 100))
                .ToArray();
            using var stream = new MemoryStream(content);
            var uploads = new List<FileUpload>
            {
                new()
                {
                    Content = stream,
                    ContentType = HttpConstants.ContentTypes.ImageJpeg,
                    Size = content.Length,
                    Filename = "afilename.jpg"
                }
            };

            var result =
                _service.GetUploadedFile(uploads, 1000, new List<string> { HttpConstants.ContentTypes.ImageJpeg });

            result.Should().BeSuccess();
            result.Value.ContentType.Should().Be(HttpConstants.ContentTypes.ImageJpeg);
            result.Value.Filename.Should().Be("afilename.jpg");
            result.Value.Size.Should().Be(103L);
            result.Value.Content.Position.Should().Be(0);
        }
    }
}