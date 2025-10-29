using System;
using System.Linq.Expressions;
using System.Threading.Tasks;
using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Configurations;
using DotNet.Testcontainers.Containers;
using Hangfire.Common;
using Hangfire.States;
using Hangfire.Storage.Monitoring;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Testcontainers.Xunit;
using Xunit;
using Xunit.Abstractions;

namespace Hangfire.SequentialJob.Tests;

public abstract class HangfireFixture : IAsyncLifetime
{
    // ReSharper disable ClassNeverInstantiated.Global
    public class Mongo(IMessageSink messageSink) : MongoFixture(messageSink);
    public class Postgres(IMessageSink messageSink) : PostgresFixture(messageSink);
    public class SqlServer(IMessageSink messageSink) : SqlServerFixture(messageSink);
    // ReSharper restore ClassNeverInstantiated.Global

    public string EnqueueJob<T>(Expression<Action<T>> job)
    {
        var backgroundJobClient = GetRequiredService<IBackgroundJobClientV2>();
        return backgroundJobClient.Create(job, new EnqueuedState());
    }

    public JobDetailsDto GetJobDetails(string jobId)
    {
        var storage = GetRequiredService<JobStorage>();
        return storage.GetMonitoringApi().JobDetails(jobId);
    }

    public StatisticsDto GetStatistics()
    {
        var storage = GetRequiredService<JobStorage>();
        return storage.GetMonitoringApi().GetStatistics();
    }

    private IHost? _host;

    async Task IAsyncLifetime.InitializeAsync()
    {
        JobFilterProviders.Providers.Add(new SequentialExecutionFilterProvider());

        var app = new HostApplicationBuilder();
        app.Services
            .AddHangfire(ConfigureStorage)
            .AddHangfireServer();
        _host = app.Build();

        await InitializeDbAsync();

        await _host.StartAsync();
    }

    async Task IAsyncLifetime.DisposeAsync()
    {
        if (_host is not null)
        {
            await _host.StopAsync();
        }

        await DisposeDbAsync();
    }

    protected abstract Task InitializeDbAsync();

    protected abstract Task DisposeDbAsync();

    protected abstract void ConfigureStorage(IGlobalConfiguration configuration);

    private T GetRequiredService<T>() where T : notnull
    {
        var host = _host ?? throw new InvalidOperationException($"The host is only available after {nameof(IAsyncLifetime.InitializeAsync)} has run.");
        return host.Services.GetRequiredService<T>();
    }
}

public abstract class HangfireContainerFixture<TBuilderEntity, TContainerEntity>(ContainerFixture<TBuilderEntity, TContainerEntity> fixture) : HangfireFixture
    where TBuilderEntity : IContainerBuilder<TBuilderEntity, TContainerEntity, IContainerConfiguration>, new()
    where TContainerEntity : IContainer
{
    protected TContainerEntity Container => fixture.Container;
    protected override async Task InitializeDbAsync() => await ((IAsyncLifetime)fixture).InitializeAsync();
    protected override async Task DisposeDbAsync() => await ((IAsyncLifetime)fixture).DisposeAsync();
}
