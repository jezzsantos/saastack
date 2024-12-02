#if TESTINGONLY
using Infrastructure.Web.Api.Interfaces;

namespace Infrastructure.Web.Api.Operations.Shared.TestingOnly;

public class GetCallerTestingOnlyResponse : IWebResponse
{
    public required string CallerId { get; set; }
}
#endif