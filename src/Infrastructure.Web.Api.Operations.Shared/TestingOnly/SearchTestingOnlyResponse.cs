#if TESTINGONLY
using Application.Resources.Shared;
using Infrastructure.Web.Api.Interfaces;

namespace Infrastructure.Web.Api.Operations.Shared.TestingOnly;

public class SearchTestingOnlyResponse : SearchResponse
{
    public List<TestResource> Items { get; set; } = [];
}
#endif