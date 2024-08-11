#if TESTINGONLY
using Infrastructure.Web.Api.Interfaces;

namespace Infrastructure.Web.Api.Operations.Shared.TestingOnly;

/// <summary>
///     Tests the use of an empty post body with route parameters
///     Notice the use of <see cref="WebRequest{TRequest,TResponse}" /> instead of <see cref="IWebRequest{TResponse}" />
/// </summary>
[Route("/testingonly/general/body/{astringproperty}/{anumberproperty}/route", OperationMethod.Post,
    isTestingOnly: true)]
public class PostWithRouteParamsAndEmptyBodyTestingOnlyRequest : WebRequest<
    PostWithRouteParamsAndEmptyBodyTestingOnlyRequest, StringMessageTestingOnlyResponse>
{
    public int? ANumberProperty { get; set; }

    public string? AStringProperty { get; set; }
}

#endif