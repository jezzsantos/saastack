#if TESTINGONLY
using JetBrains.Annotations;

namespace Infrastructure.WebApi.Interfaces.Operations.TestingOnly;

[UsedImplicitly]
public class ValidationsUnvalidatedTestingOnlyRequest : IWebRequest<StringMessageTestingOnlyResponse>
{
    public string? Id { get; set; }
}
#endif