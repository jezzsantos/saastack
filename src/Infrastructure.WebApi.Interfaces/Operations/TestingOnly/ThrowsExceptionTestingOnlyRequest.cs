#if TESTINGONLY
using JetBrains.Annotations;

namespace Infrastructure.WebApi.Interfaces.Operations.TestingOnly;

[UsedImplicitly]
public class ThrowsExceptionTestingOnlyRequest : IWebRequest<StringMessageTestingOnlyResponse>
{
}
#endif