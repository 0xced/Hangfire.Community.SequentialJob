using System;
using System.Threading.Tasks;
using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Configurations;
using DotNet.Testcontainers.Containers;
using Hangfire.Common;
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
    public class Sqlite : SqliteFixture;
    public class SqlServer(IMessageSink messageSink) : SqlServerFixture(messageSink);
    // ReSharper restore ClassNeverInstantiated.Global

    private IBackgroundJobClientV2? _backgroundJobClient;
    public IBackgroundJobClientV2 BackgroundJobClient => _backgroundJobClient ?? throw new InvalidOperationException("The background job client is only available after initialization has completed.");

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

        _backgroundJobClient = _host.Services.GetRequiredService<IBackgroundJobClientV2>();

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
}

public abstract class HangfireContainerFixture<TBuilderEntity, TContainerEntity>(ContainerFixture<TBuilderEntity, TContainerEntity> fixture) : HangfireFixture
    where TBuilderEntity : IContainerBuilder<TBuilderEntity, TContainerEntity, IContainerConfiguration>, new()
    where TContainerEntity : IContainer
{
    protected TContainerEntity Container => fixture.Container;
    protected override async Task InitializeDbAsync() => await ((IAsyncLifetime)fixture).InitializeAsync();
    protected override async Task DisposeDbAsync() => await ((IAsyncLifetime)fixture).DisposeAsync();
}
