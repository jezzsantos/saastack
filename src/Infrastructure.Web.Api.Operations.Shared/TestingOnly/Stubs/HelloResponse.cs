using Infrastructure.Web.Api.Interfaces;

namespace Infrastructure.Web.Api.Operations.Shared.TestingOnly.Stubs;

public class HelloResponse : IWebResponse
{
    public string? Message { get; set; }
}