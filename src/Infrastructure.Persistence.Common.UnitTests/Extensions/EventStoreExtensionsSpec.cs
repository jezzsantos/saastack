using Common;
using Common.Extensions;
using Domain.Common.ValueObjects;
using Infrastructure.Persistence.Common.Extensions;
using Infrastructure.Persistence.Interfaces;
using Moq;
using UnitTesting.Common;
using Xunit;

namespace Infrastructure.Persistence.Common.UnitTests.Extensions;

[Trait("Category", "Unit")]
public class EventStoreExtensionsSpec
{
    private readonly Mock<IEventStore> _eventStore = new();

    [Fact]
    public void WhenVerifyContiguousCheckAndNothingStoredAndFirstVersionIsNotFirst_ThenReturnsError()
    {
        var result = _eventStore.Object.VerifyContiguousCheck("astreamname", Optional<int>.None, 10);

        result.Should().BeError(ErrorCode.EntityExists,
            Resources.EventStore_ConcurrencyVerificationFailed_StreamReset.Format("IEventStoreProxy", "astreamname"));
    }

    [Fact]
    public void WhenVerifyContiguousCheckAndNothingStoredAndFirstVersionIsFirst_ThenPasses()
    {
        var result =
            _eventStore.Object.VerifyContiguousCheck("astreamname", Optional<int>.None, EventStream.FirstVersion);

        result.Should().BeSuccess();
    }

    [Fact]
    public void WhenVerifyContiguousCheckAndFirstVersionIsSameAsStored_ThenPasses()
    {
        var result = _eventStore.Object.VerifyContiguousCheck("astreamname", 2, 2);

        result.Should().BeSuccess();
    }

    [Fact]
    public void WhenVerifyContiguousCheckAndFirstVersionIsBeforeStored_ThenPasses()
    {
        var result = _eventStore.Object.VerifyContiguousCheck("astreamname", 2, 1);

        result.Should().BeSuccess();
    }

    [Fact]
    public void WhenVerifyContiguousCheckAndFirstVersionIsAfterStoredButNotContiguous_ThenReturnsError()
    {
        var result = _eventStore.Object.VerifyContiguousCheck("astreamname", 1, 3);

        result.Should().BeError(ErrorCode.EntityExists,
            Resources.EventStore_ConcurrencyVerificationFailed_MissingUpdates.Format("IEventStoreProxy", "astreamname",
                2, 3));
    }

    [Fact]
    public void WhenVerifyContiguousCheckAndFirstVersionIsNextAfterStored_ThenPasses()
    {
        var result = _eventStore.Object.VerifyContiguousCheck("astreamname", 1, 2);

        result.Should().BeSuccess();
    }
}