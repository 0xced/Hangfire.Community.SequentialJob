using System.Threading.Tasks;
using Xunit.Abstractions;

namespace Hangfire.Testing.InMemory;

public class HangfireInMemoryFixture(IMessageSink messageSink) : HangfireFixture(messageSink)
{
    protected override string ConfigureStorage(IGlobalConfiguration hangfire)
    {
        hangfire.UseInMemoryStorage();
        return "InMemory";
    }

    protected override Task InitializeStorageAsync() => Task.CompletedTask;

    protected override Task DisposeStorageAsync() => Task.CompletedTask;
}