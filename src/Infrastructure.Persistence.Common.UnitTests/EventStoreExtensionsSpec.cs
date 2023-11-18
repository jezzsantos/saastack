using Common;
using Common.Extensions;
using Domain.Common.ValueObjects;
using Infrastructure.Persistence.Interfaces;
using Moq;
using UnitTesting.Common;
using Xunit;

namespace Infrastructure.Persistence.Common.UnitTests;

[Trait("Category", "Unit")]
public class EventStoreExtensionsSpec
{
    private readonly Mock<IEventStore> _eventStore = new();

    [Fact]
    public void WhenVerifyConcurrencyCheckAndNothingStoredAndFirstVersionIsNotFirst_TheReturnsError()
    {
        var result = _eventStore.Object.VerifyConcurrencyCheck("astreamname", Optional<int>.None, 10);

        result.Should().BeError(ErrorCode.EntityExists,
            Resources.EventStoreExtensions_ConcurrencyVerificationFailed_StreamReset.Format("astreamname"));
    }

    [Fact]
    public void WhenVerifyConcurrencyCheckAndNothingStoredAndFirstVersionIsFirst_ThenPasses()
    {
        var result =
            _eventStore.Object.VerifyConcurrencyCheck("astreamname", Optional<int>.None, EventStream.FirstVersion);

        result.Should().BeSuccess();
    }

    [Fact]
    public void WhenVerifyConcurrencyCheckAndFirstVersionIsSameAsStored_TheReturnsError()
    {
        var result = _eventStore.Object.VerifyConcurrencyCheck("astreamname", 2, 2);

        result.Should().BeError(ErrorCode.EntityExists,
            Resources.EventStoreExtensions_ConcurrencyVerificationFailed_StreamAlreadyUpdated.Format("astreamname", 2));
    }

    [Fact]
    public void WhenVerifyConcurrencyCheckAndFirstVersionIsBeforeStored_TheReturnsError()
    {
        var result = _eventStore.Object.VerifyConcurrencyCheck("astreamname", 2, 1);

        result.Should().BeError(ErrorCode.EntityExists,
            Resources.EventStoreExtensions_ConcurrencyVerificationFailed_StreamAlreadyUpdated.Format("astreamname", 1));
    }

    [Fact]
    public void WhenVerifyConcurrencyCheckAndFirstVersionIsAfterStoredButNotContiguous_TheReturnsError()
    {
        var result = _eventStore.Object.VerifyConcurrencyCheck("astreamname", 1, 3);

        result.Should().BeError(ErrorCode.EntityExists,
            Resources.EventStoreExtensions_ConcurrencyVerificationFailed_MissingUpdates.Format("astreamname", 2, 3));
    }

    [Fact]
    public void WhenVerifyConcurrencyCheckAndFirstVersionIsNextAfterStored_ThenPasses()
    {
        var result = _eventStore.Object.VerifyConcurrencyCheck("astreamname", 1, 2);

        result.Should().BeSuccess();
    }
}