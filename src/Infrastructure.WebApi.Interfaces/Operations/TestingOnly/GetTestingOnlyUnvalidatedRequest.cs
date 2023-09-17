#if TESTINGONLY
namespace Infrastructure.WebApi.Interfaces.Operations.TestingOnly;

public class GetTestingOnlyUnvalidatedRequest : IWebRequest<GetTestingOnlyResponse>
{
    public string? Id { get; set; }
}
#endif