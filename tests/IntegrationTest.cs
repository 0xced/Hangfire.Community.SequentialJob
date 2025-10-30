using System;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using AwesomeAssertions;
using AwesomeAssertions.Execution;
using Hangfire.States;
using Hangfire.Storage;
using Hangfire.Storage.Monitoring;
using Xunit;
using Xunit.Abstractions;

namespace Hangfire.SequentialJob.Tests;

public abstract class IntegrationTest : IDisposable
{
    public class Memory(HangfireFixture.Memory fixture, ITestOutputHelper output) : IntegrationTest(fixture, output), IClassFixture<HangfireFixture.Memory>;
    public class Mongo(HangfireFixture.Mongo fixture, ITestOutputHelper output) : IntegrationTest(fixture, output), IClassFixture<HangfireFixture.Mongo>;
    public class Postgres(HangfireFixture.Postgres fixture, ITestOutputHelper output) : IntegrationTest(fixture, output), IClassFixture<HangfireFixture.Postgres>;
    public class Sqlite(HangfireFixture.Sqlite fixture, ITestOutputHelper output) : IntegrationTest(fixture, output), IClassFixture<HangfireFixture.Sqlite>;
    public class SqlServer(HangfireFixture.SqlServer fixture, ITestOutputHelper output) : IntegrationTest(fixture, output), IClassFixture<HangfireFixture.SqlServer>;

    private readonly HangfireFixture _fixture;

    private IBackgroundJobClientV2 BackgroundJobClient => _fixture.BackgroundJobClient;
    private IMonitoringApi MonitoringApi => BackgroundJobClient.Storage.GetMonitoringApi();

    protected IntegrationTest(HangfireFixture fixture, ITestOutputHelper output)
    {
        _fixture = fixture;
        _fixture.Output = output;
    }

    public void Dispose()
    {
        _fixture.Output = null;
    }

    [Fact]
    public async Task Test()
    {
        var jobId1 = BackgroundJobClient.Create<TestJob>(o => o.Run("Hello 1"), new EnqueuedState());
        var jobId2 = BackgroundJobClient.Create<TestJob>(o => o.Run("Hello 2"), new EnqueuedState());

        await WaitAsync(stats => stats.Succeeded == 2, timeout: TimeSpan.FromSeconds(10));

        var job1 = MonitoringApi.JobDetails(jobId1);
        var job2 = MonitoringApi.JobDetails(jobId2);

        using (new AssertionScope())
        {
            job1.History.Select(e => e.StateName).Should().BeEquivalentTo("Succeeded", "Processing", "Enqueued");
            job2.History.Select(e => e.StateName).Should().BeEquivalentTo("Succeeded", "Processing", "Enqueued", "Awaiting", "Enqueued");
        }
    }

    private async Task WaitAsync(Predicate<StatisticsDto> predicate, TimeSpan timeout, [CallerArgumentExpression(nameof(predicate))] string? predicateDescription = null)
    {
        var stopwatch = Stopwatch.StartNew();

        while (!predicate(MonitoringApi.GetStatistics()))
        {
            if (stopwatch.Elapsed > timeout)
            {
                throw new TimeoutException($"Waited \"{predicateDescription}\" for {timeout.TotalSeconds:N0} seconds");
            }

            await Task.Delay(millisecondsDelay: 50);
        }
    }

    [SequentialJob("my-sequence")]
    public class TestJob
    {
        public void Run(string message)
        {
            Console.WriteLine(message);
        }
    }
}
