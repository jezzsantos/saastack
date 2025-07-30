#if TESTINGONLY
using Infrastructure.Web.Api.Interfaces;

namespace Infrastructure.Web.Api.Operations.Shared.TestingOnly;

/// <summary>
///     Tests download of streams
/// </summary>
[Route("/testingonly/download", OperationMethod.Get, isTestingOnly: true)]
public class
    DownloadStreamTestingOnlyRequest : UnTenantedStreamRequest<DownloadStreamTestingOnlyRequest>
{
}
#endif