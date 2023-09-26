#if TESTINGONLY
using JetBrains.Annotations;

namespace Infrastructure.WebApi.Interfaces.Operations.TestingOnly;

[UsedImplicitly]
public class ReturnsErrorTestingOnlyRequest : IWebRequest<StringMessageTestingOnlyResponse>
{
}
#endif