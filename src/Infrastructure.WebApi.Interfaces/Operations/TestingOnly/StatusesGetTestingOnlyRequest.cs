#if TESTINGONLY
using JetBrains.Annotations;

namespace Infrastructure.WebApi.Interfaces.Operations.TestingOnly;

[UsedImplicitly]
public class StatusesGetTestingOnlyRequest : IWebRequest<StatusesTestingOnlyResponse>
{
}
#endif