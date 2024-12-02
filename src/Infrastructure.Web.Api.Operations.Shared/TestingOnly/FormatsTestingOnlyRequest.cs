#if TESTINGONLY
using Infrastructure.Web.Api.Interfaces;
using JetBrains.Annotations;

namespace Infrastructure.Web.Api.Operations.Shared.TestingOnly;

/// <summary>
///     Tests response formats
/// </summary>
[Route("/testingonly/formats/roundtrip", OperationMethod.Post, isTestingOnly: true)]
[UsedImplicitly]
public class FormatsTestingOnlyRequest : WebRequest<FormatsTestingOnlyRequest, FormatsTestingOnlyResponse>
{
    public CustomDto? Custom { get; set; }

    public double? Double { get; set; }

    public CustomEnum? Enum { get; set; }

    public int? Integer { get; set; }

    public string? String { get; set; }

    public DateTime? Time { get; set; }
}

public class CustomDto
{
    public double? Double { get; set; }

    public CustomEnum? Enum { get; set; }

    public int? Integer { get; set; }

    public string? String { get; set; }

    public DateTime? Time { get; set; }
}

public enum CustomEnum
{
    None,
    One,
    TwentyOne,
    OneHundredAndOne
}
#endif