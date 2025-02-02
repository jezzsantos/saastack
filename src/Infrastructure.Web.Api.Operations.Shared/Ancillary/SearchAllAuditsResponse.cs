using Application.Resources.Shared;
using Infrastructure.Web.Api.Interfaces;

namespace Infrastructure.Web.Api.Operations.Shared.Ancillary;

public class SearchAllAuditsResponse : SearchResponse
{
    public List<Audit> Audits { get; set; } = [];
}