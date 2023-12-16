using Infrastructure.Web.Api.Interfaces;

#if TESTINGONLY
namespace Infrastructure.Web.Api.Operations.Shared.TestingOnly;

public class StringMessageTestingOnlyResponse : IWebResponse
{
    public string? Message { get; set; }
}
#endif