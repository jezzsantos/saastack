#if TESTINGONLY
using Infrastructure.Web.Api.Interfaces;

namespace Infrastructure.Web.Api.Operations.Shared.BackEndForFrontEnd.TestingOnly;

public class BeffeTestingOnlyResponse : IWebResponse
{
    public required string CallerId { get; set; }
}
#endif