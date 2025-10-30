using System;
using Hangfire.Mongo;
using Hangfire.Mongo.Migration.Strategies;
using MongoDB.Driver;
using Testcontainers.MongoDb;
using Testcontainers.Xunit;
using Xunit.Abstractions;

namespace Hangfire.SequentialJob.Tests;

public class MongoFixture(IMessageSink messageSink) : HangfireContainerFixture<MongoDbBuilder, MongoDbContainer>(messageSink, new DbFixture(messageSink))
{
    protected override string ConfigureStorage(IGlobalConfiguration hangfire)
    {
        var connectionString = new MongoUrlBuilder(Container.GetConnectionString()) { DatabaseName = "admin" }.ToString();
        var storageOptions = new MongoStorageOptions
        {
            MigrationOptions = new MongoMigrationOptions { MigrationStrategy = new DropMongoMigrationStrategy() },
            QueuePollInterval = TimeSpan.FromSeconds(1),
        };
        hangfire.UseMongoStorage(connectionString, storageOptions);
        return "Mongo";
    }

    private class DbFixture(IMessageSink messageSink) : ContainerFixture<MongoDbBuilder, MongoDbContainer>(messageSink)
    {
        protected override MongoDbBuilder Configure(MongoDbBuilder builder) => builder.WithImage("mongo:8").WithReplicaSet();
    }
}