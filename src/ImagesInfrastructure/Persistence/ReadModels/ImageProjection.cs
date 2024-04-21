using Application.Persistence.Common.Extensions;
using Application.Persistence.Interfaces;
using Common;
using Domain.Events.Shared.Images;
using Domain.Interfaces;
using Domain.Interfaces.Entities;
using ImagesApplication.Persistence.ReadModels;
using ImagesDomain;
using Infrastructure.Persistence.Common;
using Infrastructure.Persistence.Interfaces;

namespace ImagesInfrastructure.Persistence.ReadModels;

public class ImageProjection : IReadModelProjection
{
    private readonly IReadModelProjectionStore<Image> _images;

    public ImageProjection(IRecorder recorder, IDomainFactory domainFactory, IDataStore store)
    {
        _images = new ReadModelProjectionStore<Image>(recorder, domainFactory, store);
    }

    public Type RootAggregateType => typeof(ImageRoot);

    public async Task<Result<bool, Error>> ProjectEventAsync(IDomainEvent changeEvent,
        CancellationToken cancellationToken)
    {
        switch (changeEvent)
        {
            case Created e:
                return await _images.HandleCreateAsync(e.RootId, dto => { dto.ContentType = e.ContentType; },
                    cancellationToken);

            case DetailsChanged e:
                return await _images.HandleUpdateAsync(e.RootId,
                    dto =>
                    {
                        dto.Description = e.Description;
                        dto.Filename = e.Filename;
                    }, cancellationToken);

            case AttributesChanged e:
                return await _images.HandleUpdateAsync(e.RootId, dto => { dto.Size = e.Size; },
                    cancellationToken);

            default:
                return false;
        }
    }
}