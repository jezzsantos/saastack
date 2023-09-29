#if TESTINGONLY
namespace Infrastructure.WebApi.Interfaces.Operations.TestingOnly;

public class FormatsTestingOnlyResponse : IWebResponse
{
    public CustomDto? Custom { get; set; }

    public double? Double { get; set; }

    public CustomEnum? Enum { get; set; }

    public int? Integer { get; set; }

    public string? String { get; set; }

    public DateTime? Time { get; set; }
}

#endif