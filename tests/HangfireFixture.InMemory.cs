using System.Threading.Tasks;
using Xunit.Abstractions;

namespace Hangfire.SequentialJob.Tests;

public class InMemoryFixture(IMessageSink messageSink) : HangfireFixture(messageSink)
{
    protected override string ConfigureStorage(IGlobalConfiguration hangfire)
    {
        hangfire.UseInMemoryStorage();
        return "InMemory";
    }

    protected override Task InitializeDbAsync() => Task.CompletedTask;

    protected override Task DisposeDbAsync() => Task.CompletedTask;
}