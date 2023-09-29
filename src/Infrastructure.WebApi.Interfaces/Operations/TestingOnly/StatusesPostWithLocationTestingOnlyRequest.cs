#if TESTINGONLY
using JetBrains.Annotations;

namespace Infrastructure.WebApi.Interfaces.Operations.TestingOnly;

[UsedImplicitly]
public class StatusesPostWithLocationTestingOnlyRequest : IWebRequest<StatusesTestingOnlyResponse>
{
}
#endif