using Common;
using Common.Configuration;
using Infrastructure.Web.Api.Interfaces;

namespace TestingStubApiHost.Api;

public abstract class StubApiBase : IWebApiService
{
    protected StubApiBase(IRecorder recorder, IConfigurationSettings settings)
    {
        Recorder = recorder;
        Settings = settings;
    }

    protected IRecorder Recorder { get; }

    protected IConfigurationSettings Settings { get; }
}