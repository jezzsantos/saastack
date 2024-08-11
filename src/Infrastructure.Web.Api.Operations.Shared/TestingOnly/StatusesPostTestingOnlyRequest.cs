#if TESTINGONLY
using Infrastructure.Web.Api.Interfaces;

namespace Infrastructure.Web.Api.Operations.Shared.TestingOnly;

/// <summary>
///     Tests default status code for POST requests, without a location
/// </summary>
[Route("/testingonly/statuses/post1", OperationMethod.Post, isTestingOnly: true)]
public class StatusesPostTestingOnlyRequest : WebRequest<StatusesPostTestingOnlyRequest, StatusesTestingOnlyResponse>;
#endif