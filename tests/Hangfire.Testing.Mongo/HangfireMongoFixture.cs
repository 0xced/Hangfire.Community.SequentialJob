using System;
using Hangfire.Mongo;
using Hangfire.Mongo.Migration.Strategies;
using Hangfire.Testing.Testcontainers;
using MongoDB.Driver;
using Testcontainers.MongoDb;
using Testcontainers.Xunit;
using Xunit.Abstractions;

namespace Hangfire.Testing.Mongo;

public class HangfireMongoFixture : HangfireContainerFixture<MongoDbBuilder, MongoDbContainer>
{
    public HangfireMongoFixture(IMessageSink messageSink) : base(messageSink, CreateMongoFixture(messageSink, out var fixture))
    {
        fixture.ConfigureContainer = ConfigureContainer;
    }

    private static MongoFixture CreateMongoFixture(IMessageSink messageSink, out MongoFixture fixture)
    {
        fixture = new MongoFixture(messageSink);
        return fixture;
    }

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

    protected virtual MongoDbBuilder ConfigureContainer(MongoDbBuilder builder) => builder.WithImage("mongo:8").WithReplicaSet();

    private class MongoFixture(IMessageSink messageSink) : ContainerFixture<MongoDbBuilder, MongoDbContainer>(messageSink)
    {
        public Func<MongoDbBuilder, MongoDbBuilder> ConfigureContainer { get; set; } = b => b;
        protected override MongoDbBuilder Configure(MongoDbBuilder builder) => ConfigureContainer(builder);
    }
}