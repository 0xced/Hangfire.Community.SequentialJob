using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Hangfire.Storage;
using Hangfire.Storage.Monitoring;
using Xunit.Abstractions;

namespace Hangfire.Testing;

public abstract class HangfireTest : IDisposable
{
    private readonly HangfireFixture _fixture;

    protected HangfireTest(HangfireFixture fixture, ITestOutputHelper output)
    {
        _fixture = fixture;
        _fixture.Output = output;
    }

    public void Dispose()
    {
        _fixture.Output = null;
    }

    protected IBackgroundJobClientV2 BackgroundJobClient => _fixture.BackgroundJobClient;
    protected JobStorage Storage => BackgroundJobClient.Storage;
    protected IMonitoringApi MonitoringApi => Storage.GetMonitoringApi();
    protected IStorageConnection Connection => Storage.GetConnection();

    protected async Task WaitAsync(Predicate<StatisticsDto> predicate, TimeSpan timeout, [CallerArgumentExpression(nameof(predicate))] string? predicateDescription = null)
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
}