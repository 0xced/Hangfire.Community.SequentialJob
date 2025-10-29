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

namespace Hangfire.SequentialJob.Tests;

public abstract class IntegrationTest(HangfireFixture fixture)
{
    public class Memory(HangfireFixture.Memory fixture) : IntegrationTest(fixture), IClassFixture<HangfireFixture.Memory>;
    public class Mongo(HangfireFixture.Mongo fixture) : IntegrationTest(fixture), IClassFixture<HangfireFixture.Mongo>;
    public class Postgres(HangfireFixture.Postgres fixture) : IntegrationTest(fixture), IClassFixture<HangfireFixture.Postgres>;
    public class Sqlite(HangfireFixture.Sqlite fixture) : IntegrationTest(fixture), IClassFixture<HangfireFixture.Sqlite>;
    public class SqlServer(HangfireFixture.SqlServer fixture) : IntegrationTest(fixture), IClassFixture<HangfireFixture.SqlServer>;

    private readonly IBackgroundJobClientV2 _backgroundJobClient = fixture.BackgroundJobClient;
    private readonly IMonitoringApi _monitoringApi = fixture.BackgroundJobClient.Storage.GetMonitoringApi();

    [Fact]
    public async Task Test()
    {
        var jobId1 = _backgroundJobClient.Create<TestJob>(o => o.Run("Hello 1"), new EnqueuedState());
        var jobId2 = _backgroundJobClient.Create<TestJob>(o => o.Run("Hello 2"), new EnqueuedState());

        await WaitAsync(stats => stats.Succeeded == 2, timeout: TimeSpan.FromSeconds(10));

        var job1 = _monitoringApi.JobDetails(jobId1);
        var job2 = _monitoringApi.JobDetails(jobId2);

        using (new AssertionScope())
        {
            job1.History.Select(e => e.StateName).Should().BeEquivalentTo("Succeeded", "Processing", "Enqueued");
            job2.History.Select(e => e.StateName).Should().BeEquivalentTo("Succeeded", "Processing", "Enqueued", "Awaiting", "Enqueued");
        }
    }

    private async Task WaitAsync(Predicate<StatisticsDto> predicate, TimeSpan timeout, [CallerArgumentExpression(nameof(predicate))] string? predicateDescription = null)
    {
        var stopwatch = Stopwatch.StartNew();

        while (!predicate(_monitoringApi.GetStatistics()))
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

