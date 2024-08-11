#if TESTINGONLY
using Infrastructure.Web.Api.Interfaces;

namespace Infrastructure.Web.Api.Operations.Shared.TestingOnly;

/// <summary>
///     Tests default status code for POST requests, with a location
/// </summary>
[Route("/testingonly/statuses/post2", OperationMethod.Post, isTestingOnly: true)]
public class
    StatusesPostWithLocationTestingOnlyRequest : WebRequest<StatusesPostWithLocationTestingOnlyRequest,
    StatusesTestingOnlyResponse>;
#endif