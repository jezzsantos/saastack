using Application.Persistence.Interfaces;
using Common;
using FluentAssertions;
using UnitTesting.Common;
using Xunit;

namespace Application.Persistence.Common.UnitTests;

[Trait("Category", "Unit")]
public class EventStreamChangedArgsSpec
{
    private readonly EventStreamChangedArgs _args;

    public EventStreamChangedArgsSpec()
    {
        _args = new EventStreamChangedArgs(new List<EventStreamChangeEvent>());
    }

    [Fact]
    public async Task WhenCompleteAsyncAndNoTasks_ThenReturnsOk()
    {
        var result = await _args.CompleteAsync();

        result.Should().BeSuccess();
    }

    [Fact]
    public async Task WhenCompleteAsyncAndEmptyTasks_ThenReturnsOk()
    {
        _args.AddTasks(_ => new List<Task<Result<Error>>>());

        var result = await _args.CompleteAsync();

        result.Should().BeSuccess();
    }

    [Fact]
    public async Task WhenCompleteAsyncAndTaskSuccessful_ThenReturnsOk()
    {
        _args.AddTasks(_ => new List<Task<Result<Error>>>
        {
            Task.FromResult(Result.Ok)
        });

        var result = await _args.CompleteAsync();

        result.Should().BeSuccess();
    }

    [Fact]
    public async Task WhenCompleteAsyncAndTaskThrows_ThenThrows()
    {
        var exception = new Exception("amessage");
        _args.AddTasks(_ => new List<Task<Result<Error>>>
        {
            Task.FromException<Result<Error>>(exception)
        });

        await _args
            .Invoking(x => x.CompleteAsync())
            .Should().ThrowAsync<Exception>()
            .WithMessage("amessage");
    }

    [Fact]
    public async Task WhenCompleteAsyncAndTaskReturnsError_ThenReturnsError()
    {
        _args.AddTasks(_ => new List<Task<Result<Error>>>
        {
            Task.FromResult<Result<Error>>(Error.RuleViolation("amessage"))
        });

        var result = await _args.CompleteAsync();

        result.Should().BeError(ErrorCode.RuleViolation, "amessage");
    }

    [Fact]
    public async Task WhenCompleteAsyncAndMultipleSuccessfulTasks_ThenReturnsOk()
    {
        _args.AddTasks(_ => new List<Task<Result<Error>>>
        {
            Task.FromResult(Result.Ok),
            Task.FromResult(Result.Ok),
            Task.FromResult(Result.Ok)
        });

        var result = await _args.CompleteAsync();

        result.Should().BeSuccess();
    }

    [Fact]
    public async Task WhenCompleteAsyncAndMultipleFailedTasks_ThenReturnsFirstError()
    {
        _args.AddTasks(_ => new List<Task<Result<Error>>>
        {
            Task.FromResult(Result.Ok),
            Task.FromResult(Result.Ok),
            Task.FromResult<Result<Error>>(Error.RuleViolation("amessage1")),
            Task.FromResult<Result<Error>>(Error.EntityNotFound("amessage2")),
            Task.FromResult<Result<Error>>(Error.Unexpected("amessage3"))
        });

        var result = await _args.CompleteAsync();

        result.Should().BeError(ErrorCode.RuleViolation, "amessage1");
    }
}