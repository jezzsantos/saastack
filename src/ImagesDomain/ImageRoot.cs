using Common;
using Common.Extensions;
using Domain.Common.Entities;
using Domain.Common.Extensions;
using Domain.Common.Identity;
using Domain.Events.Shared.Images;
using Domain.Interfaces;
using Domain.Interfaces.Entities;
using Domain.Interfaces.ValueObjects;

namespace ImagesDomain;

public sealed class ImageRoot : AggregateRootBase
{
    public static Result<ImageRoot, Error> Create(IRecorder recorder, IIdentifierFactory idFactory,
        string contentType)
    {
        if (contentType.IsInvalidParameter(Validations.Images.ContentTypes,
                nameof(contentType), Resources.ImageRoot_UnsupportedContentType.Format(contentType), out var error))
        {
            return error;
        }

        var root = new ImageRoot(recorder, idFactory);
        root.RaiseCreateEvent(ImagesDomain.Events.Created(root.Id, contentType));
        return root;
    }

    private ImageRoot(IRecorder recorder, IIdentifierFactory idFactory) : base(recorder, idFactory)
    {
    }

    private ImageRoot(IRecorder recorder, IIdentifierFactory idFactory, ISingleValueObject<string> identifier) : base(
        recorder, idFactory, identifier)
    {
    }

    public string ContentType { get; private set; } = string.Empty;

    public Optional<string> Description { get; private set; }

    public Optional<string> Filename { get; private set; }

    public Optional<long> Size { get; private set; }

    public static AggregateRootFactory<ImageRoot> Rehydrate()
    {
        return (identifier, container, _) => new ImageRoot(container.GetRequiredService<IRecorder>(),
            container.GetRequiredService<IIdentifierFactory>(), identifier);
    }

    public override Result<Error> EnsureInvariants()
    {
        var ensureInvariants = base.EnsureInvariants();
        if (!ensureInvariants.IsSuccessful)
        {
            return ensureInvariants.Error;
        }

        if (ContentType.HasNoValue())
        {
            return Error.RuleViolation(Resources.ImageRoot_MissingContentType);
        }

        return Result.Ok;
    }

    protected override Result<Error> OnStateChanged(IDomainEvent @event, bool isReconstituting)
    {
        switch (@event)
        {
            case Created created:
            {
                ContentType = created.ContentType;
                return Result.Ok;
            }

            case AttributesChanged changed:
            {
                Size = changed.Size;
                Recorder.TraceDebug(null, "Image {Id} changed attributes", Id);
                return Result.Ok;
            }

            case DetailsChanged changed:
            {
                Description = changed.Description;
                Filename = changed.Filename;
                Recorder.TraceDebug(null, "Image {Id} changed details", Id);
                return Result.Ok;
            }

            default:
                return HandleUnKnownStateChangedEvent(@event);
        }
    }

    public Result<Error> ChangeDetails(string? description, string? filename = null)
    {
        if (description.HasNoValue() && filename.HasNoValue())
        {
            return Result.Ok;
        }

        if (description.HasValue())
        {
            if (description.IsInvalidParameter(Validations.Images.Description, nameof(description),
                    Resources.ImageRoot_InvalidDescription, out var error1))
            {
                return error1;
            }
        }

        if (filename.HasValue())
        {
            if (filename.IsInvalidParameter(Validations.Images.Filename, nameof(filename),
                    Resources.ImageRoot_InvalidFilename, out var error2))
            {
                return error2;
            }
        }

        return RaiseChangeEvent(
            ImagesDomain.Events.DetailsChanged(Id, description ?? Description, filename ?? Filename));
    }

    public Result<Error> Delete()
    {
        return RaisePermanentDeleteEvent(Id);
    }

    public Result<Error> SetAttributes(long imageSize)
    {
        if (imageSize.IsInvalidParameter(s => s < Validations.Images.MaxSizeInBytes, nameof(imageSize),
                Resources.ImageRoot_ImageSizeExceeded.Format(imageSize), out var error2))
        {
            return error2;
        }

        return RaiseChangeEvent(ImagesDomain.Events.AttributesChanged(Id, imageSize));
    }

#if TESTINGONLY
    public void TestingOnly_SetContentType(Optional<string> contentType)
    {
        ContentType = contentType;
    }
#endif
}