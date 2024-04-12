using Infrastructure.Web.Api.Interfaces;

namespace Infrastructure.Web.Api.Common.UnitTests;

[Route("/aroute", OperationMethod.Get)]
public class TestRequest : IWebRequest<TestResponse>
{
    public int ANumberProperty { get; set; }

    public string? AStringProperty { get; set; }

    public string? Id { get; set; }
}

public class TestResponse : IWebResponse;