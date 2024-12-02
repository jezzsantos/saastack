using System.ComponentModel.DataAnnotations;
using Infrastructure.Web.Api.Interfaces;
using JetBrains.Annotations;

#if TESTINGONLY
namespace Infrastructure.Web.Api.Operations.Shared.TestingOnly;

public class OpenApiTestingOnlyResponse : IWebResponse
{
    [Required] public string AnAnnotatedRequiredField { get; set; } = "avalue";

    public string AnInitializedField { get; set; } = "avalue";

    public OpenApiResponseObject? AnNullableObject { get; set; }

    public string? ANullableField { get; set; }

    public bool? ANullableValueTypeField { get; set; }

    public required string ARequiredField { get; set; }

    public bool AValueTypeField { get; set; }

    public required string Message { get; set; }
}

[UsedImplicitly]
public class OpenApiResponseObject
{
    [Required] public string AnAnnotatedRequiredField { get; set; } = "avalue";

    public string AnInitializedField { get; set; } = "avalue";

    public string? ANullableField { get; set; }

    public bool? ANullableValueTypeField { get; set; }

    public required string ARequiredField { get; set; }

    public bool AValueTypeField { get; set; }
}
#endif