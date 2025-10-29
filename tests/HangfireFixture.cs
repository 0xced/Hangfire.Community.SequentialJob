using System;
using System.Data.Common;
using System.Linq.Expressions;
using System.Threading.Tasks;
using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Configurations;
using DotNet.Testcontainers.Containers;
using Hangfire.Common;
using Hangfire.Mongo;
using Hangfire.Mongo.Migration.Strategies;
using Hangfire.PostgreSql;
using Hangfire.States;
using Hangfire.Storage.Monitoring;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using MongoDB.Driver;
using Npgsql;
using Testcontainers.MongoDb;
using Testcontainers.MsSql;
using Testcontainers.PostgreSql;
using Testcontainers.Xunit;
using Xunit;
using Xunit.Abstractions;

namespace Hangfire.SequentialJob.Tests;

public abstract class HangfireFixture : IAsyncLifetime
{
    public class Mongo(IMessageSink messageSink) : HangfireContainerFixture<MongoDbBuilder, MongoDbContainer>(new MongoFixture(messageSink))
    {
        protected override void ConfigureStorage(IGlobalConfiguration hangfire)
        {
            var connectionString = new MongoUrlBuilder(Container.GetConnectionString()) { DatabaseName = "admin" }.ToString();
            var storageOptions = new MongoStorageOptions
            {
                MigrationOptions = new MongoMigrationOptions { MigrationStrategy = new DropMongoMigrationStrategy() },
                QueuePollInterval = TimeSpan.FromSeconds(1),
            };
            hangfire.UseMongoStorage(connectionString, storageOptions);
        }

        private class MongoFixture(IMessageSink messageSink) : ContainerFixture<MongoDbBuilder, MongoDbContainer>(messageSink)
        {
            protected override MongoDbBuilder Configure(MongoDbBuilder builder) => builder.WithImage("mongo:8").WithReplicaSet();
        }
    }

    public class Postgres(IMessageSink messageSink) : HangfireContainerFixture<PostgreSqlBuilder, PostgreSqlContainer>(new PostgresFixture(messageSink))
    {
        protected override void ConfigureStorage(IGlobalConfiguration hangfire) => hangfire.UsePostgreSqlStorage(postgres => postgres.UseNpgsqlConnection(Container.GetConnectionString()));

        private class PostgresFixture(IMessageSink messageSink) : DbContainerFixture<PostgreSqlBuilder, PostgreSqlContainer>(messageSink)
        {
            public override DbProviderFactory DbProviderFactory => NpgsqlFactory.Instance;
            protected override PostgreSqlBuilder Configure(PostgreSqlBuilder builder) => builder.WithImage("postgres:18");
        }
    }

    public class SqlServer(IMessageSink messageSink) : HangfireContainerFixture<MsSqlBuilder, MsSqlContainer>(new SqlServerFixture(messageSink))
    {
        protected override void ConfigureStorage(IGlobalConfiguration hangfire) => hangfire.UseSqlServerStorage(Container.GetConnectionString());

        private class SqlServerFixture(IMessageSink messageSink) : DbContainerFixture<MsSqlBuilder, MsSqlContainer>(messageSink)
        {
            public override DbProviderFactory DbProviderFactory => SqlClientFactory.Instance;
            protected override MsSqlBuilder Configure(MsSqlBuilder builder) => builder.WithImage("mcr.microsoft.com/mssql/server:2022-CU21-ubuntu-22.04");
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

