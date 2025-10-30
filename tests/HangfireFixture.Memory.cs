using System.Threading.Tasks;
using Hangfire.MemoryStorage;
using Xunit.Abstractions;

namespace Hangfire.SequentialJob.Tests;

public class MemoryFixture(IMessageSink messageSink) : HangfireFixture(messageSink)
{
    protected override string ConfigureStorage(IGlobalConfiguration hangfire)
    {
        hangfire.UseMemoryStorage();
        return "Memory";
    }

    protected override Task InitializeDbAsync() => Task.CompletedTask;

    protected override Task DisposeDbAsync() => Task.CompletedTask;
}