using Application.Persistence.Common;
using Common;
using QueryAny;

namespace SigningsApplication.Persistence.ReadModels;

[EntityName("SigningRequest")]
public class SigningRequest : ReadModelEntity
{
    public Optional<string> OrganizationId { get; set; }
}