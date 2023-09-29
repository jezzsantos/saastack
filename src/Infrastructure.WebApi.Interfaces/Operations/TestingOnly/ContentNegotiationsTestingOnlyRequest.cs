#if TESTINGONLY
using JetBrains.Annotations;

namespace Infrastructure.WebApi.Interfaces.Operations.TestingOnly;

[UsedImplicitly]
public class ContentNegotiationsTestingOnlyRequest : IWebRequest<StringMessageTestingOnlyResponse>
{
}
#endif