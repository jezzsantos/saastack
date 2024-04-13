using System.Net;
using ApiHost1;
using Application.Resources.Shared;
using Common.Extensions;
using FluentAssertions;
using Infrastructure.Web.Api.Common;
using Infrastructure.Web.Api.Common.Extensions;
using Infrastructure.Web.Api.Operations.Shared.Images;
using Infrastructure.Web.Interfaces.Clients;
using IntegrationTesting.WebApi.Common;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace ImagesInfrastructure.IntegrationTests;

[Trait("Category", "Integration.Web")]
[Collection("API")]
public class ImagesApiSpec : WebApiSpec<Program>
{
    public ImagesApiSpec(WebApiSetup<Program> setup) : base(setup, OverrideDependencies)
    {
        EmptyAllRepositories();
    }

    [Fact]
    public async Task WhenUploadImage_ThenReturnsImage()
    {
        var login = await LoginUserAsync();

        var image = await UploadImage(login, "adescription");

        image.ContentType.Should().Be(HttpContentTypes.ImagePng);
        image.Description.Should().Be("adescription");
        image.Filename.Should().Be("afilename.png");
        image.Url.Should().Be($"https://localhost:5001/images/{image.Id}/download");
    }

    [Fact]
    public async Task WhenGetImage_ThenReturnsImage()
    {
        var login = await LoginUserAsync();

        var image = await UploadImage(login, "adescription");

        var result = await Api.GetAsync(new GetImageRequest
        {
            Id = image.Id
        }, req => req.SetJWTBearerToken(login.AccessToken));

        result.Content.Value.Image!.ContentType.Should().Be(HttpContentTypes.ImagePng);
        result.Content.Value.Image.Description.Should().Be("adescription");
        result.Content.Value.Image.Filename.Should().Be("afilename.png");
        result.Content.Value.Image.Url.Should().Be($@"https://localhost:5001/images/{image.Id}/download");
    }

    [Fact]
    public async Task WhenDeleteImage_ThenReturnsImage()
    {
        var login = await LoginUserAsync();

        var image = await UploadImage(login, "adescription");

        var result = await Api.DeleteAsync(new DeleteImageRequest
        {
            Id = image.Id
        }, req => req.SetJWTBearerToken(login.AccessToken));

        result.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task WhenDownloadImage_ThenReturnsImage()
    {
        var login = await LoginUserAsync();

        var image = await UploadImage(login, "adescription");

        var result = await Api.GetAsync(new DownloadImageRequest
        {
            Id = image.Id
        }, req => req.SetJWTBearerToken(login.AccessToken));

        result.StatusCode.Should().Be(HttpStatusCode.OK);
        var expectedImage = GetTestImage().ReadFully();
        result.RawContent!.ReadFully().Should().BeEquivalentTo(expectedImage);
    }

    [Fact]
    public async Task WhenUpdateImage_ThenReturnsImage()
    {
        var login = await LoginUserAsync();

        var image = await UploadImage(login, "adescription");

        var result = await Api.PutAsync(new UpdateImageRequest
        {
            Id = image.Id,
            Description = "anewdescription"
        }, req => req.SetJWTBearerToken(login.AccessToken));

        result.Content.Value.Image!.ContentType.Should().Be(HttpContentTypes.ImagePng);
        result.Content.Value.Image.Description.Should().Be("anewdescription");
        result.Content.Value.Image.Filename.Should().Be("afilename.png");
        result.Content.Value.Image.Url.Should().Be($"https://localhost:5001/images/{image.Id}/download");
    }

    private async Task<Image> UploadImage(LoginDetails login, string description)
    {
        var result = await Api.PostAsync(new UploadImageRequest
            {
                Description = description
            }, new PostFile(GetTestImage(), HttpContentTypes.ImagePng, "afilename"),
            req => req.SetJWTBearerToken(login.AccessToken));

        return result.Content.Value.Image!;
    }

    private static void OverrideDependencies(IServiceCollection services)
    {
        // Do nothing here
    }
}