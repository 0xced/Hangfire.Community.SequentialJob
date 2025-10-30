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
using Xunit.Sdk;

namespace Hangfire.SequentialJob.Tests;

public abstract class HangfireFixture(IMessageSink messageSink) : IAsyncLifetime
{
    // ReSharper disable ClassNeverInstantiated.Global
    public class InMemory(IMessageSink messageSink) : InMemoryFixture(messageSink);
    public class Mongo(IMessageSink messageSink) : MongoFixture(messageSink);
    public class Postgres(IMessageSink messageSink) : PostgresFixture(messageSink);
    public class Redis(IMessageSink messageSink) : RedisFixture(messageSink);
    public class Sqlite(IMessageSink messageSink) : SqliteFixture(messageSink);
    public class SqlServer(IMessageSink messageSink) : SqlServerFixture(messageSink);
    // ReSharper restore ClassNeverInstantiated.Global

    private IBackgroundJobClientV2? _backgroundJobClient;
    public IBackgroundJobClientV2 BackgroundJobClient => _backgroundJobClient ?? throw new InvalidOperationException("The background job client is only available after initialization has completed.");

    private IHost? _host;

    public ITestOutputHelper? Output { get; set; }

    private void Log(string message)
    {
        if (Output == null)
        {
            messageSink.OnMessage(new DiagnosticMessage(message));
        }
        else
        {
            Output.WriteLine(message);
        }
    }

    async Task IAsyncLifetime.InitializeAsync()
    {
        JobFilterProviders.Providers.Add(new SequentialExecutionFilterProvider());

        var app = new HostApplicationBuilder();
        app.Services
            .AddHangfire(hangfire =>
            {
                var description = ConfigureStorage(hangfire);
                hangfire.UseLogProvider(new XunitLogProvider(description, Log));
            })
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

    protected abstract string ConfigureStorage(IGlobalConfiguration configuration);
}

public abstract class HangfireContainerFixture<TBuilderEntity, TContainerEntity>(IMessageSink messageSink, ContainerFixture<TBuilderEntity, TContainerEntity> fixture) : HangfireFixture(messageSink)
    where TBuilderEntity : IContainerBuilder<TBuilderEntity, TContainerEntity, IContainerConfiguration>, new()
    where TContainerEntity : IContainer
{
    protected TContainerEntity Container => fixture.Container;
    protected override async Task InitializeDbAsync() => await ((IAsyncLifetime)fixture).InitializeAsync();
    protected override async Task DisposeDbAsync() => await ((IAsyncLifetime)fixture).DisposeAsync();
}
