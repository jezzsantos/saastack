#if TESTINGONLY
using Common.Configuration;
using Infrastructure.Hosting.Common.Extensions;
using Infrastructure.Persistence.Interfaces;
using Infrastructure.Persistence.Interfaces.ApplicationServices;
using Infrastructure.Persistence.Shared.ApplicationServices;
using Microsoft.Extensions.DependencyInjection;

namespace Infrastructure.Web.Hosting.Common.Extensions;

internal static class TestingOnlyHostExtensions
{
    public static void RegisterStoreForTestingOnly(IServiceCollection services, bool usesQueues, bool isMultiTenanted)
    {
        services
            .AddForPlatform<IDataStore, IEventStore, IBlobStore, IQueueStore, IMessageBusStore,
                LocalMachineJsonFileStore>(c =>
                LocalMachineJsonFileStore.Create(c.GetRequiredServiceForPlatform<IConfigurationSettings>(),
                    c.GetService<IMessageMonitor>()));
        if (isMultiTenanted)
        {
            services
                .AddPerHttpRequest<IDataStore, IEventStore, IBlobStore, IQueueStore, IMessageBusStore,
                    LocalMachineJsonFileStore>(c =>
                    LocalMachineJsonFileStore.Create(c.GetRequiredService<IConfigurationSettings>(),
                        c.GetService<IMessageMonitor>()));
        }
        else
        {
            services
                .AddSingleton<IDataStore, IEventStore, IBlobStore, IQueueStore, IMessageBusStore,
                    LocalMachineJsonFileStore>(c =>
                    LocalMachineJsonFileStore.Create(c.GetRequiredServiceForPlatform<IConfigurationSettings>(),
                        c.GetService<IMessageMonitor>()));
        }
    }
}
#endif