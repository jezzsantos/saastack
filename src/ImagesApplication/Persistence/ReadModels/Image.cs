using Application.Persistence.Common;
using Common;
using QueryAny;

namespace ImagesApplication.Persistence.ReadModels;

[EntityName("Image")]
public class Image : ReadModelEntity
{
    public Optional<string> ContentType { get; set; }

    public Optional<string> CreatedById { get; set; }

    public Optional<string> Description { get; set; }

    public Optional<string> Filename { get; set; }

    public Optional<long> Size { get; set; }
}