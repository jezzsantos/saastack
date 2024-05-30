using Application.Interfaces;
using Application.Persistence.Shared.ReadModels;
using FluentAssertions;
using Infrastructure.Web.Api.Operations.Shared.Ancillary;
using Infrastructure.Web.Interfaces.Clients;
using Infrastructure.Worker.Api.IntegrationTests.Stubs;
using Microsoft.Extensions.DependencyInjection;
using UnitTesting.Common;
using Xunit;
using StringExtensions = Common.Extensions.StringExtensions;

namespace Infrastructure.Worker.Api.IntegrationTests;

public abstract class DeliverEmailSpecBase<TSetup> : ApiWorkerSpec<TSetup>
    where TSetup : class, IApiWorkerSpec
{
    private readonly StubServiceClient _serviceClient;

    protected DeliverEmailSpecBase(TSetup setup) : base(setup, OverrideDependencies)
    {
#if TESTINGONLY
        setup.QueueStore.DestroyAllAsync(WorkerConstants.Queues.Emails, CancellationToken.None).GetAwaiter()
            .GetResult();
#endif
        _serviceClient = setup.GetRequiredService<IServiceClient>().As<StubServiceClient>();
        _serviceClient.Reset();
    }

    [Fact]
    public async Task WhenMessageQueuedContainingInvalidContent_ThenApiNotCalled()
    {
        await Setup.QueueStore.PushAsync(WorkerConstants.Queues.Emails, "aninvalidmessage",
            CancellationToken.None);

        Setup.WaitForQueueProcessingToComplete();

#if TESTINGONLY
        (await Setup.QueueStore.CountAsync(WorkerConstants.Queues.Emails, CancellationToken.None))
            .Should().Be(0);
#endif
        _serviceClient.LastPostedMessage.Should().BeNone();
    }

    [Fact]
    public async Task WhenMessageQueued_ThenApiCalled()
    {
        var message = StringExtensions.ToJson(new EmailMessage
        {
            Html = new QueuedEmailHtmlMessage
            {
                Subject = "asubject",
                HtmlBody = "abody",
                ToEmailAddress = "arecipientemailaddress",
                FromEmailAddress = "asenderemailaddress"
            }
        })!;
        await Setup.QueueStore.PushAsync(WorkerConstants.Queues.Emails, message, CancellationToken.None);

        Setup.WaitForQueueProcessingToComplete();

#if TESTINGONLY
        (await Setup.QueueStore.CountAsync(WorkerConstants.Queues.Emails, CancellationToken.None))
            .Should().Be(0);
#endif
        _serviceClient.LastPostedMessage.Value.Should()
            .BeEquivalentTo(new DeliverEmailRequest { Message = message });
    }

    private static void OverrideDependencies(IServiceCollection services)
    {
        services.AddSingleton<IServiceClient, StubServiceClient>();
    }
}