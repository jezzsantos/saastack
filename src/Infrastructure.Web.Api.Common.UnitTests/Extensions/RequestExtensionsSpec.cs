using Common.Extensions;
using FluentAssertions;
using Infrastructure.Web.Api.Common.Extensions;
using Infrastructure.Web.Api.Interfaces;
using Xunit;

// ReSharper disable UnusedMember.Local
// ReSharper disable UnusedAutoPropertyAccessor.Local

namespace Infrastructure.Web.Api.Common.UnitTests.Extensions;

[Trait("Category", "Unit")]
public class RequestExtensionsSpec
{
    [Fact]
    public void WhenGetRequestInfoAndNoAttribute_ThenThrows()
    {
        var request = new NoRouteRequest();

        request.Invoking(x => x.GetRequestInfo())
            .Should().Throw<InvalidOperationException>()
            .WithMessage(
                Resources.RequestExtensions_MissingRouteAttribute.Format(nameof(NoRouteRequest),
                    nameof(RouteAttribute)));
    }

    [Fact]
    public void WhenGetRequestInfoAndRequestHasNoProperties_ThenReturnsInfo()
    {
        var request = new HasNoPropertiesRequest();

        var result = request.GetRequestInfo();

        result.Route.Should().Be("/aroute/{unknown}");
        result.Operation.Should().Be(ServiceOperation.Get);
        result.IsTestingOnly.Should().BeFalse();
    }

    [Fact]
    public void WhenGetRequestInfoAndRouteTemplateContainsNoPlaceholdersWithNoDataForGet_ThenReturnsInfo()
    {
        var request = new HasNoPlaceholdersGetRequest();

        var result = request.GetRequestInfo();

        result.Route.Should().Be("/aroute");
        result.Operation.Should().Be(ServiceOperation.Get);
        result.IsTestingOnly.Should().BeFalse();
    }

    [Fact]
    public void WhenGetRequestInfoAndRouteTemplateContainsNoPlaceholdersWithDataForGet_ThenReturnsInfo()
    {
        var datum = new DateTime(2023, 10, 29, 12, 30, 15, DateTimeKind.Utc).ToNearestSecond();
        var request = new HasNoPlaceholdersGetRequest
        {
            Id = "anid",
            ANumberProperty = 999,
            ADateTimeProperty = datum,
            AStringProperty = "avalue"
        };

        var result = request.GetRequestInfo();

        result.Route.Should()
            .Be(
                "/aroute?adatetimeproperty=2023-10-29T12%3a30%3a15Z&anumberproperty=999&astringproperty=avalue&id=anid");
        result.Operation.Should().Be(ServiceOperation.Get);
        result.IsTestingOnly.Should().BeFalse();
    }

    [Fact]
    public void WhenGetRequestInfoAndRouteTemplateContainsNoPlaceholdersWithDataForPost_ThenReturnsInfo()
    {
        var datum = new DateTime(2023, 10, 29, 12, 30, 15, DateTimeKind.Utc).ToNearestSecond();
        var request = new HasNoPlaceholdersPostRequest
        {
            Id = "anid",
            ANumberProperty = 999,
            ADateTimeProperty = datum,
            AStringProperty = "avalue"
        };

        var result = request.GetRequestInfo();

        result.Route.Should().Be("/aroute");
        result.Operation.Should().Be(ServiceOperation.Post);
        result.IsTestingOnly.Should().BeFalse();
    }

    [Fact]
    public void WhenGetRequestInfoAndRouteTemplateHasUnknownPlaceholderWithNoDataForGet_ThenReturnsInfo()
    {
        var request = new HasUnknownPlaceholderGetRequest();

        var result = request.GetRequestInfo();

        result.Route.Should().Be("/aroute/{unknown}");
        result.Operation.Should().Be(ServiceOperation.Get);
        result.IsTestingOnly.Should().BeFalse();
    }

    [Fact]
    public void WhenGetRequestInfoAndRouteTemplateHasUnknownPlaceholderWithDataForGet_ThenReturnsInfo()
    {
        var datum = new DateTime(2023, 10, 29, 12, 30, 15, DateTimeKind.Utc).ToNearestSecond();
        var request = new HasUnknownPlaceholderGetRequest
        {
            Id = "anid",
            ANumberProperty = 999,
            ADateTimeProperty = datum,
            AStringProperty = "avalue"
        };

        var result = request.GetRequestInfo();

        result.Route.Should()
            .Be(
                "/aroute/{unknown}?adatetimeproperty=2023-10-29T12%3a30%3a15Z&anumberproperty=999&astringproperty=avalue&id=anid");
        result.Operation.Should().Be(ServiceOperation.Get);
        result.IsTestingOnly.Should().BeFalse();
    }

    [Fact]
    public void WhenGetRequestInfoAndRouteTemplateHasUnknownPlaceholderWithDataForPost_ThenReturnsInfo()
    {
        var datum = new DateTime(2023, 10, 29, 12, 30, 15, DateTimeKind.Utc).ToNearestSecond();
        var request = new HasUnknownPlaceholderPostRequest
        {
            Id = "anid",
            ANumberProperty = 999,
            ADateTimeProperty = datum,
            AStringProperty = "avalue"
        };

        var result = request.GetRequestInfo();

        result.Route.Should().Be("/aroute/{unknown}");
        result.Operation.Should().Be(ServiceOperation.Post);
        result.IsTestingOnly.Should().BeFalse();
    }

    [Fact]
    public void WhenGetRequestInfoAndRouteTemplateHasPlaceholdersWithNullDataValuesForGet_ThenReturnsInfo()
    {
        var request = new HasPlaceholdersGetRequest
        {
            Id = null,
            AStringProperty1 = null,
            AStringProperty2 = null,
            AStringProperty3 = null,
            ANumberProperty = null,
            ADateTimeProperty = null
        };

        var result = request.GetRequestInfo();

        result.Route.Should().Be("/aroute/apath1/xxxyyy/apath2/apath3");
        result.Operation.Should().Be(ServiceOperation.Get);
        result.IsTestingOnly.Should().BeFalse();
    }

    [Fact]
    public void WhenGetRequestInfoAndRouteTemplateHasPlaceholdersWithDataValuesForGet_ThenReturnsInfo()
    {
        var datum = new DateTime(2023, 10, 29, 12, 30, 15, DateTimeKind.Utc).ToNearestSecond();
        var request = new HasPlaceholdersGetRequest
        {
            Id = "anid",
            AStringProperty1 = "avalue1",
            AStringProperty2 = "avalue2",
            AStringProperty3 = "avalue3",
            ANumberProperty = 999,
            ADateTimeProperty = datum
        };

        var result = request.GetRequestInfo();

        result.Route.Should()
            .Be(
                "/aroute/anid/apath1/xxx999yyy/apath2/avalue1/avalue2/apath3?adatetimeproperty=2023-10-29T12%3a30%3a15Z&astringproperty3=avalue3");
        result.Operation.Should().Be(ServiceOperation.Get);
        result.IsTestingOnly.Should().BeFalse();
    }

    [Fact]
    public void WhenGetRequestInfoAndRouteTemplateHasPlaceholdersWithSomeDataValuesForGet_ThenReturnsInfo()
    {
        var datum = new DateTime(2023, 10, 29, 12, 30, 15, DateTimeKind.Utc).ToNearestSecond();
        var request = new HasPlaceholdersGetRequest
        {
            Id = "anid",
            AStringProperty1 = "avalue1",
            AStringProperty2 = null,
            AStringProperty3 = null,
            ANumberProperty = null,
            ADateTimeProperty = datum
        };

        var result = request.GetRequestInfo();

        result.Route.Should()
            .Be("/aroute/anid/apath1/xxxyyy/apath2/avalue1/apath3?adatetimeproperty=2023-10-29T12%3a30%3a15Z");
        result.Operation.Should().Be(ServiceOperation.Get);
        result.IsTestingOnly.Should().BeFalse();
    }

    [Fact]
    public void WhenGetRequestInfoAndRouteTemplateHasPlaceholdersWithNullDataValuesForPost_ThenReturnsInfo()
    {
        var request = new HasPlaceholdersPostRequest
        {
            Id = null,
            AStringProperty1 = null,
            AStringProperty2 = null,
            AStringProperty3 = null,
            ANumberProperty = null,
            ADateTimeProperty = null
        };

        var result = request.GetRequestInfo();

        result.Route.Should().Be("/aroute/apath1/xxxyyy/apath2/apath3");
        result.Operation.Should().Be(ServiceOperation.Post);
        result.IsTestingOnly.Should().BeFalse();
    }

    [Fact]
    public void WhenGetRequestInfoAndRouteTemplateHasPlaceholdersWithDataValuesForPost_ThenReturnsInfo()
    {
        var datum = new DateTime(2023, 10, 29, 12, 30, 15, DateTimeKind.Utc).ToNearestSecond();
        var request = new HasPlaceholdersPostRequest
        {
            Id = "anid",
            AStringProperty1 = "avalue1",
            AStringProperty2 = "avalue2",
            AStringProperty3 = "avalue3",
            ANumberProperty = 999,
            ADateTimeProperty = datum
        };

        var result = request.GetRequestInfo();

        result.Route.Should().Be("/aroute/anid/apath1/xxx999yyy/apath2/avalue1/avalue2/apath3");
        result.Operation.Should().Be(ServiceOperation.Post);
        result.IsTestingOnly.Should().BeFalse();
    }

    [Fact]
    public void WhenGetRequestInfoAndRouteTemplateHasPlaceholdersWithSomeDataValuesForPost_ThenReturnsInfo()
    {
        var datum = new DateTime(2023, 10, 29, 12, 30, 15, DateTimeKind.Utc).ToNearestSecond();
        var request = new HasPlaceholdersPostRequest
        {
            Id = "anid",
            AStringProperty1 = "avalue1",
            AStringProperty2 = null,
            AStringProperty3 = null,
            ANumberProperty = null,
            ADateTimeProperty = datum
        };

        var result = request.GetRequestInfo();

        result.Route.Should().Be("/aroute/anid/apath1/xxxyyy/apath2/avalue1/apath3");
        result.Operation.Should().Be(ServiceOperation.Post);
        result.IsTestingOnly.Should().BeFalse();
    }

    [Fact]
    public void WhenToUrl_ThenReturnsUrl()
    {
        var datum = new DateTime(2023, 10, 29, 12, 30, 15, DateTimeKind.Utc).ToNearestSecond();
        var request = new HasPlaceholdersPostRequest
        {
            Id = "anid",
            AStringProperty1 = "avalue1",
            AStringProperty2 = null,
            AStringProperty3 = null,
            ANumberProperty = null,
            ADateTimeProperty = datum
        };

        var result = request.ToUrl();

        result.Should().Be("/aroute/anid/apath1/xxxyyy/apath2/avalue1/apath3");
    }

    private class NoRouteRequest : IWebRequest<TestResponse>;

    [Route("/aroute/{unknown}", ServiceOperation.Get)]
    private class HasNoPropertiesRequest : IWebRequest<TestResponse>;

    [Route("/aroute", ServiceOperation.Get)]
    private class HasNoPlaceholdersGetRequest : IWebRequest<TestResponse>
    {
        public DateTime? ADateTimeProperty { get; set; }

        public int? ANumberProperty { get; set; }

        public string? AStringProperty { get; set; }

        public string? Id { get; set; }
    }

    [Route("/aroute", ServiceOperation.Post)]
    private class HasNoPlaceholdersPostRequest : IWebRequest<TestResponse>
    {
        public DateTime? ADateTimeProperty { get; set; }

        public int? ANumberProperty { get; set; }

        public string? AStringProperty { get; set; }

        public string? Id { get; set; }
    }

    [Route("/aroute/{unknown}", ServiceOperation.Get)]
    private class HasUnknownPlaceholderGetRequest : IWebRequest<TestResponse>
    {
        public DateTime? ADateTimeProperty { get; set; }

        public int? ANumberProperty { get; set; }

        public string? AStringProperty { get; set; }

        public string? Id { get; set; }
    }

    [Route("/aroute/{unknown}", ServiceOperation.Post)]
    private class HasUnknownPlaceholderPostRequest : IWebRequest<TestResponse>
    {
        public DateTime? ADateTimeProperty { get; set; }

        public int? ANumberProperty { get; set; }

        public string? AStringProperty { get; set; }

        public string? Id { get; set; }
    }

    [Route("/aroute/{id}/apath1/xxx{anumberproperty}yyy/apath2/{astringproperty1}/{astringproperty2}/apath3",
        ServiceOperation.Get)]
    private class HasPlaceholdersGetRequest : IWebRequest<TestResponse>
    {
        public DateTime? ADateTimeProperty { get; set; }

        public int? ANumberProperty { get; set; }

        public string? AStringProperty1 { get; set; }

        public string? AStringProperty2 { get; set; }

        public string? AStringProperty3 { get; set; }

        public string? Id { get; set; }
    }

    [Route("/aroute/{id}/apath1/xxx{anumberproperty}yyy/apath2/{astringproperty1}/{astringproperty2}/apath3",
        ServiceOperation.Post)]
    private class HasPlaceholdersPostRequest : IWebRequest<TestResponse>
    {
        public DateTime? ADateTimeProperty { get; set; }

        public int? ANumberProperty { get; set; }

        public string? AStringProperty1 { get; set; }

        public string? AStringProperty2 { get; set; }

        public string? AStringProperty3 { get; set; }

        public string? Id { get; set; }
    }
}