using Common;
using Common.Extensions;
using Domain.Common.ValueObjects;
using Domain.Interfaces;

namespace Domain.Shared;

public delegate Task<Result<Avatar, Error>> CreateAvatarAction(Name name);

public delegate Task<Result<Error>> RemoveAvatarAction(Identifier avatarId);

public sealed class Avatar : ValueObjectBase<Avatar>
{
    public static Result<Avatar, Error> Create(Identifier imageId, string url)
    {
        if (url.IsNotValuedParameter(nameof(url), Resources.Avatar_InvalidUrl, out var error1))
        {
            return error1;
        }

        return new Avatar(imageId, url);
    }

    private Avatar(Identifier imageId, string url)
    {
        ImageId = imageId;
        Url = url;
    }

    public Identifier ImageId { get; }

    public string Url { get; }

    public static ValueObjectFactory<Avatar> Rehydrate()
    {
        return (property, _) =>
        {
            var parts = RehydrateToList(property, false);
            return new Avatar(parts[0].ToId(), parts[1]!);
        };
    }

    protected override IEnumerable<object?> GetAtomicValues()
    {
        return new object[] { ImageId, Url };
    }
}