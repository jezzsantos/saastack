#if TESTINGONLY
using System.Net;
using System.Net.Http.Json;
using System.Text;
using Application.Interfaces;
using Application.Resources.Shared;
using Common.Extensions;
using FluentAssertions;
using Infrastructure.Web.Api.Operations.Shared.TestingOnly;
using IntegrationTesting.WebApi.Common;
using JetBrains.Annotations;
using Xunit;

namespace ApiHost1.IntegrationTests;

[UsedImplicitly]
[Trait("Category", "Integration.API")]
[Collection("API")]
public class GeneralApiSpec : WebApiSpec<Program>
{
    public GeneralApiSpec(WebApiSetup<Program> setup) : base(setup)
    {
    }

    [Fact]
    public async Task WhenGeneralPost_ThenReturns()
    {
        var result = await Api.PostAsync(new PostTestingOnlyRequest
        {
            ACamelEnumProperty = TestingOnlyEnum.Value1,
            ACamelIntProperty = 1,
            ACamelStringProperty = "avalue1",
            AnEnumProperty = TestingOnlyEnum.Value2,
            AnEnumQueryProperty = TestingOnlyEnum.Value3,
            AnEnumRouteProperty = TestingOnlyEnum.Value4,
            AnIntProperty = 2,
            AnIntQueryProperty = 3,
            AnIntRouteProperty = 4,
            AStringProperty = "avalue2",
            AStringQueryProperty = "avalue3",
            AStringRouteProperty = "avalue4"
        });

        result.StatusCode.Should().Be(HttpStatusCode.Created);
        result.Headers.Location!.ToString().Should().Be("alocation");
        result.Content.Value.ACamelEnumProperty.Should().Be(TestingOnlyEnum.Value1);
        result.Content.Value.ACamelIntProperty.Should().Be(1);
        result.Content.Value.ACamelStringProperty.Should().Be("avalue1");
        result.Content.Value.AnEnumProperty.Should().Be(TestingOnlyEnum.Value2);
        result.Content.Value.AnEnumQueryProperty.Should().Be(TestingOnlyEnum.Value3);
        result.Content.Value.AnEnumRouteProperty.Should().Be(TestingOnlyEnum.Value4);
        result.Content.Value.AnIntProperty.Should().Be(2);
        result.Content.Value.AnIntQueryProperty.Should().Be(3);
        result.Content.Value.AnIntRouteProperty.Should().Be(4);
        result.Content.Value.AStringProperty.Should().Be("avalue2");
        result.Content.Value.AStringQueryProperty.Should().Be("avalue3");
        result.Content.Value.AStringRouteProperty.Should().Be("avalue4");
    }

    [Fact]
    public async Task WhenGeneralPostCrossPlatform_ThenReturns()
    {
        var content = JsonContent.Create(new
        {
            a_camel_enum = "value1",
            a_camel_int = 1,
            a_camel_string = "avalue1",
            anEnumProperty = "value2",
            anIntProperty = 2,
            aStringProperty = "avalue2"
        });
        var result = await HttpApi.PostAsync(
            "/testingonly/general/post/a/Value4/b/4/c/avalue4"
            + "?anenumqueryproperty=Value3"
            + "&anintqueryproperty=3"
            + "&astringqueryproperty=avalue3", //HACK: notice that all enum values in the route or query must be case sensitive!
            content);

        result.StatusCode.Should().Be(HttpStatusCode.Created);
        result.Headers.Location!.ToString().Should().Be("alocation");

        var response = await result.Content.ReadFromJsonAsync<GeneralTestingOnlyResponse>(SerializerOptions);
        response!.ACamelEnumProperty.Should().Be(TestingOnlyEnum.Value1);
        response.ACamelIntProperty.Should().Be(1);
        response.ACamelStringProperty.Should().Be("avalue1");
        response.AnEnumProperty.Should().Be(TestingOnlyEnum.Value2);
        response.AnEnumQueryProperty.Should().Be(TestingOnlyEnum.Value3);
        response.AnEnumRouteProperty.Should().Be(TestingOnlyEnum.Value4);
        response.AnIntProperty.Should().Be(2);
        response.AnIntQueryProperty.Should().Be(3);
        response.AnIntRouteProperty.Should().Be(4);
        response.AStringProperty.Should().Be("avalue2");
        response.AStringQueryProperty.Should().Be("avalue3");
        response.AStringRouteProperty.Should().Be("avalue4");
    }

    [Fact]
    public async Task WhenGeneralGet_ThenReturns()
    {
        var result = await Api.GetAsync(new GetTestingOnlyRequest
        {
            ACamelEnumProperty = TestingOnlyEnum.Value1,
            ACamelIntProperty = 1,
            ACamelStringProperty = "avalue1",
            AnEnumProperty = TestingOnlyEnum.Value2,
            AnEnumQueryProperty = TestingOnlyEnum.Value3,
            AnEnumRouteProperty = TestingOnlyEnum.Value4,
            AnIntProperty = 2,
            AnIntQueryProperty = 3,
            AnIntRouteProperty = 4,
            AStringProperty = "avalue2",
            AStringQueryProperty = "avalue3",
            AStringRouteProperty = "avalue4"
        });

        result.StatusCode.Should().Be(HttpStatusCode.OK);
        result.Content.Value.ACamelEnumProperty.Should().Be(TestingOnlyEnum.Value1);
        result.Content.Value.ACamelIntProperty.Should().Be(1);
        result.Content.Value.ACamelStringProperty.Should().Be("avalue1");
        result.Content.Value.AnEnumProperty.Should().Be(TestingOnlyEnum.Value2);
        result.Content.Value.AnEnumQueryProperty.Should().Be(TestingOnlyEnum.Value3);
        result.Content.Value.AnEnumRouteProperty.Should().Be(TestingOnlyEnum.Value4);
        result.Content.Value.AnIntProperty.Should().Be(2);
        result.Content.Value.AnIntQueryProperty.Should().Be(3);
        result.Content.Value.AnIntRouteProperty.Should().Be(4);
        result.Content.Value.AStringProperty.Should().Be("avalue2");
        result.Content.Value.AStringQueryProperty.Should().Be("avalue3");
        result.Content.Value.AStringRouteProperty.Should().Be("avalue4");
    }

    [Fact]
    public async Task WhenGeneralGetCrossPlatform_ThenReturns()
    {
        var result = await HttpApi.GetAsync(
            "/testingonly/general/get/a/Value4/b/4/c/avalue4"
            + "?a_camel_enum=Value1"
            + "&a_camel_int=1"
            + "&a_camel_string=avalue1"
            + "&anenumproperty=Value2"
            + "&anintproperty=2"
            + "&astringproperty=avalue2"
            + "&anenumqueryproperty=Value3"
            + "&anintqueryproperty=3"
            + "&astringqueryproperty=avalue3"); //HACK: notice that all enum values in the route or query must be case sensitive!

        result.StatusCode.Should().Be(HttpStatusCode.OK);

        var response = await result.Content.ReadFromJsonAsync<GeneralTestingOnlyResponse>(SerializerOptions);
        response!.ACamelEnumProperty.Should().Be(TestingOnlyEnum.Value1);
        response.ACamelIntProperty.Should().Be(1);
        response.ACamelStringProperty.Should().Be("avalue1");
        response.AnEnumProperty.Should().Be(TestingOnlyEnum.Value2);
        response.AnEnumQueryProperty.Should().Be(TestingOnlyEnum.Value3);
        response.AnEnumRouteProperty.Should().Be(TestingOnlyEnum.Value4);
        response.AnIntProperty.Should().Be(2);
        response.AnIntQueryProperty.Should().Be(3);
        response.AnIntRouteProperty.Should().Be(4);
        response.AStringProperty.Should().Be("avalue2");
        response.AStringQueryProperty.Should().Be("avalue3");
        response.AStringRouteProperty.Should().Be("avalue4");
    }

    [Fact]
    public async Task WhenGeneralGetSearch_ThenReturns()
    {
        var result = await Api.GetAsync(new SearchTestingOnlyRequest
        {
            ACamelEnumProperty = TestingOnlyEnum.Value1,
            ACamelIntProperty = 1,
            ACamelStringProperty = "avalue1",
            AnEnumProperty = TestingOnlyEnum.Value2,
            AnEnumQueryProperty = TestingOnlyEnum.Value3,
            AnEnumRouteProperty = TestingOnlyEnum.Value4,
            AnIntProperty = 2,
            AnIntQueryProperty = 3,
            AnIntRouteProperty = 4,
            AStringProperty = "avalue2",
            AStringQueryProperty = "avalue3",
            AStringRouteProperty = "avalue4"
        });

        result.StatusCode.Should().Be(HttpStatusCode.OK);
        result.Content.Value.Items.Count.Should().Be(3);
        result.Content.Value.ACamelEnumProperty.Should().Be(TestingOnlyEnum.Value1);
        result.Content.Value.ACamelIntProperty.Should().Be(1);
        result.Content.Value.ACamelStringProperty.Should().Be("avalue1");
        result.Content.Value.AnEnumProperty.Should().Be(TestingOnlyEnum.Value2);
        result.Content.Value.AnEnumQueryProperty.Should().Be(TestingOnlyEnum.Value3);
        result.Content.Value.AnEnumRouteProperty.Should().Be(TestingOnlyEnum.Value4);
        result.Content.Value.AnIntProperty.Should().Be(2);
        result.Content.Value.AnIntQueryProperty.Should().Be(3);
        result.Content.Value.AnIntRouteProperty.Should().Be(4);
        result.Content.Value.AStringProperty.Should().Be("avalue2");
        result.Content.Value.AStringQueryProperty.Should().Be("avalue3");
        result.Content.Value.AStringRouteProperty.Should().Be("avalue4");
    }

    [Fact]
    public async Task WhenGeneralSearchCrossPlatform_ThenReturns()
    {
        var result = await HttpApi.GetAsync(
            "/testingonly/general/search/a/Value4/b/4/c/avalue4"
            + "?a_camel_enum=Value1"
            + "&a_camel_int=1"
            + "&a_camel_string=avalue1"
            + "&anenumproperty=Value2"
            + "&anintproperty=2"
            + "&astringproperty=avalue2"
            + "&anenumqueryproperty=Value3"
            + "&anintqueryproperty=3"
            + "&astringqueryproperty=avalue3"); //HACK: notice that all enum values in the route or query must be case sensitive!

        result.StatusCode.Should().Be(HttpStatusCode.OK);

        var response = await result.Content.ReadFromJsonAsync<GeneralTestingOnlyResponse>(SerializerOptions);
        response!.ACamelEnumProperty.Should().Be(TestingOnlyEnum.Value1);
        response.ACamelIntProperty.Should().Be(1);
        response.ACamelStringProperty.Should().Be("avalue1");
        response.AnEnumProperty.Should().Be(TestingOnlyEnum.Value2);
        response.AnEnumQueryProperty.Should().Be(TestingOnlyEnum.Value3);
        response.AnEnumRouteProperty.Should().Be(TestingOnlyEnum.Value4);
        response.AnIntProperty.Should().Be(2);
        response.AnIntQueryProperty.Should().Be(3);
        response.AnIntRouteProperty.Should().Be(4);
        response.AStringProperty.Should().Be("avalue2");
        response.AStringQueryProperty.Should().Be("avalue3");
        response.AStringRouteProperty.Should().Be("avalue4");
    }

    [Fact]
    public async Task WhenGeneralGetSearchApiWithNoPagination_ThenReturns()
    {
        var result = await Api.GetAsync(new SearchTestingOnlyRequest
        {
            AnEnumRouteProperty = TestingOnlyEnum.Value4,
            AnIntRouteProperty = 4,
            AStringRouteProperty = "avalue4"
        });

        result.StatusCode.Should().Be(HttpStatusCode.OK);
        result.Content.Value.Items.Count.Should().Be(3);
        result.Content.Value.Metadata.Filter!.Fields
            .Should().BeEmpty();
        result.Content.Value.Metadata.Sort.Should().BeNull();
        result.Content.Value.Metadata.Offset.Should().Be(SearchOptions.NoOffset);
        result.Content.Value.Metadata.Limit.Should().Be(SearchOptions.DefaultLimit);
        result.Content.Value.Metadata.Total.Should().Be(3);
    }

    [Fact]
    public async Task WhenGeneralGetSearchApiWithPagination_ThenReturns()
    {
        var result = await Api.GetAsync(new SearchTestingOnlyRequest
        {
            Limit = 50,
            Offset = 1,
            Filter = nameof(TestResource.AProperty),
            Sort = $"-{nameof(TestResource.AProperty)}",
            Embed = "anembeddedresourcename",
            AnEnumRouteProperty = TestingOnlyEnum.Value4,
            AnIntRouteProperty = 4,
            AStringRouteProperty = "avalue4"
        });

        result.StatusCode.Should().Be(HttpStatusCode.OK);
        result.Content.Value.Items.Count.Should().Be(3);
        result.Content.Value.Metadata.Filter!.Fields.Count.Should().Be(1);
        result.Content.Value.Metadata.Filter!.Fields[0].Should().Be(nameof(TestResource.AProperty));
        result.Content.Value.Metadata.Sort!.By.Should().Be(nameof(TestResource.AProperty));
        result.Content.Value.Metadata.Sort.Direction.Should().Be(SortDirection.Descending);
        result.Content.Value.Metadata.Offset.Should().Be(1);
        result.Content.Value.Metadata.Limit.Should().Be(50);
        result.Content.Value.Metadata.Total.Should().Be(3);
    }

    [Fact]
    public async Task WhenPostWithEmptyBody_ThenReturns()
    {
        var result = await Api.PostAsync(new PostWithEmptyBodyTestingOnlyRequest());

        result.StatusCode.Should().Be(HttpStatusCode.Created);
        result.Content.Value.Message.Should().Be("amessage");
    }

    [Fact]
    public async Task WhenPostWithFormData_ThenReturns()
    {
        var result = await Api.PostAsync(new OpenApiPostMultiPartFormDataTestingOnlyRequest
        {
            Id = "anid",
            RequiredField = "avalue"
        });

        result.StatusCode.Should().Be(HttpStatusCode.Created);
        result.Content.Value.Message.Should().Be("amessageavalue");
    }

    [Fact]
    public async Task WhenPostWithUrlEncoded_ThenReturns()
    {
        var result = await Api.PostAsync(new OpenApiPostFormUrlEncodedTestingOnlyRequest
        {
            Id = "anid",
            RequiredField = "avalue"
        });

        result.StatusCode.Should().Be(HttpStatusCode.Created);
        result.Content.Value.Message.Should().Be("amessageavalue");
    }

    [Fact]
    public async Task WhenPostWithRouteParamsAndEmptyBody_ThenReturns()
    {
        var result = await HttpApi.PostAsync("/testingonly/general/body/avalue/99/route",
            JsonContent.Create(new PostWithRouteParamsAndEmptyBodyTestingOnlyRequest()));

        result.StatusCode.Should().Be(HttpStatusCode.Created);
        var content = await result.Content.ReadAsStringAsync();
        content.Should().Be("{\"message\":\"amessageavalue99\"}");
    }

    [Fact]
    public async Task WhenGetWithSimpleArray_ThenReturns()
    {
        var result = await Api.GetAsync(new GetWithSimpleArrayTestingOnlyRequest
        {
            AnArray = ["a", "b", "c"]
        });

        result.StatusCode.Should().Be(HttpStatusCode.OK);
        result.Content.Value.Message.Should().Be("a, b, c");
    }

    [Fact]
    public async Task WhenGetWithSimpleArrayInSimpleArray_ThenReturns()
    {
        var result = await HttpApi.GetAsync("/testingonly/general/get/array?anarray=a&anarray=b&anarray=c");

        result.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await result.Content.ReadAsStringAsync();
        content.Should().Be("{\"message\":\"a, b, c\"}");
    }

    [Fact]
    public async Task WhenGetWithSimpleArrayInAxiosArray_ThenReturnsArray()
    {
        var result = await HttpApi.GetAsync("/testingonly/general/get/array?anarray[]=a&anarray[]=b&anarray[]=c");

        result.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await result.Content.ReadAsStringAsync();
        content.Should().Be("{\"message\":\"a, b, c\"}");
    }

    [Fact]
    public async Task WhenPostWithEmptyBodyAndMissingRequiredProperties_ThenThrows()
    {
        try
        {
            await HttpApi.PostEmptyJsonAsync("/testingonly/general/body/empty/required");
        }
        catch (Exception ex)
        {
            ex.Should().BeOfType<InvalidOperationException>()
                .Which.Message.Should().Contain(
                    "JSON deserialization for type 'Infrastructure.Web.Api.Operations.Shared.TestingOnly.PostWithEmptyBodyAndRequiredPropertiesTestingOnlyRequest' was missing required properties, including the following: requiredField");
        }
    }

    [Fact]
    public async Task WhenRedirectWithPostAndLocationForError_ThenReturnsError()
    {
        var result = await Api.PostAsync(new PostWithRedirectTestingOnlyRequest
        {
            Result = "error"
        });

        result.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        result.Content.HasValue.Should().BeFalse();
        result.Headers.Location.Should().BeNull();
    }

    [Fact]
    public async Task WhenRedirectWithPostAndLocationForRedirect_ThenReturnsRedirect()
    {
        var result = await Api.PostAsync(new PostWithRedirectTestingOnlyRequest
        {
            Result = "redirect"
        });

        result.StatusCode.Should().Be(HttpStatusCode.Redirect);
        result.Content.HasValue.Should().BeTrue();
        result.Headers.Location.Should().Be("aurl");
    }

    [Fact]
    public async Task WhenRedirectWithPostAndLocationForNoRedirect_ThenReturnsContent()
    {
        var result = await Api.PostAsync(new PostWithRedirectTestingOnlyRequest
        {
            Result = "noredirect"
        });

        result.StatusCode.Should().Be(HttpStatusCode.OK);
        result.Content.Value.Message.Should().Be("amessage");
        result.Headers.Location.Should().BeNull();
    }

    [Fact]
    public async Task WhenRedirectWithGetAndLocationForError_ThenReturnsError()
    {
        var result = await Api.GetAsync(new GetWithRedirectTestingOnlyRequest
        {
            Result = "error"
        });

        result.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        result.Content.HasValue.Should().BeFalse();
        result.Headers.Location.Should().BeNull();
    }

    [Fact]
    public async Task WhenRedirectWithGetAndLocationForRedirect_ThenReturnsRedirect()
    {
        var result = await Api.GetAsync(new GetWithRedirectTestingOnlyRequest
        {
            Result = "redirect"
        });

        result.StatusCode.Should().Be(HttpStatusCode.Redirect);
        result.Content.HasValue.Should().BeTrue();
        result.Headers.Location.Should().Be("aurl");
    }

    [Fact]
    public async Task WhenRedirectWithGetAndLocationForNoRedirect_ThenReturnsContent()
    {
        var result = await Api.GetAsync(new GetWithRedirectTestingOnlyRequest
        {
            Result = "noredirect"
        });

        result.StatusCode.Should().Be(HttpStatusCode.OK);
        result.Content.Value.Message.Should().Be("amessage");
        result.Headers.Location.Should().BeNull();
    }

    [Fact]
    public async Task WhenDownloadStream_ThenReturnsStream()
    {
        var result = await Api.GetAsync(new DownloadStreamTestingOnlyRequest());

        result.StatusCode.Should().Be(HttpStatusCode.OK);
        result.Content.HasValue.Should().BeFalse();
        var raw = await result.RawContent!.ReadFullyAsync(CancellationToken.None);
        Encoding.UTF8.GetString(raw).Should().Be("adownload");
    }
}
#endif