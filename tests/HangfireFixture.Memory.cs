using System.Threading.Tasks;
using Hangfire.MemoryStorage;

namespace Hangfire.SequentialJob.Tests;

public class MemoryFixture : HangfireFixture
{
    protected override void ConfigureStorage(IGlobalConfiguration hangfire)
    {
        hangfire.UseMemoryStorage();
    }

    protected override Task InitializeDbAsync() => Task.CompletedTask;

    protected override Task DisposeDbAsync() => Task.CompletedTask;
}