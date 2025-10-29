using System;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using AwesomeAssertions;
using AwesomeAssertions.Execution;
using Hangfire.Storage.Monitoring;
using Xunit;

namespace Hangfire.SequentialJob.Tests;

public abstract class IntegrationTest(HangfireFixture fixture)
{
    public class Postgres(HangfireFixture.Postgres fixture) : IntegrationTest(fixture), IClassFixture<HangfireFixture.Postgres>;
    public class SqlServer(HangfireFixture.SqlServer fixture) : IntegrationTest(fixture), IClassFixture<HangfireFixture.SqlServer>;

    [Fact]
    public async Task Test()
    {
        var jobId1 = fixture.EnqueueJob<TestJob>(o => o.Run("Hello 1"));
        var jobId2 = fixture.EnqueueJob<TestJob>(o => o.Run("Hello 2"));

        await WaitAsync(stats => stats.Succeeded == 2, timeout: TimeSpan.FromSeconds(10));

        var job1 = fixture.GetJobDetails(jobId1);
        var job2 = fixture.GetJobDetails(jobId2);

        using (new AssertionScope())
        {
            job1.History.Select(e => e.StateName).Should().BeEquivalentTo("Succeeded", "Processing", "Enqueued");
            job2.History.Select(e => e.StateName).Should().BeEquivalentTo("Succeeded", "Processing", "Enqueued", "Awaiting", "Enqueued");
        }
    }

    private async Task WaitAsync(Predicate<StatisticsDto> predicate, TimeSpan timeout, [CallerArgumentExpression(nameof(predicate))] string? predicateDescription = null)
    {
        var stopwatch = Stopwatch.StartNew();

        while (!predicate(fixture.GetStatistics()))
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

