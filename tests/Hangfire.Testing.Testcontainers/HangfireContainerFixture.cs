using System.Threading.Tasks;
using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Configurations;
using DotNet.Testcontainers.Containers;
using Testcontainers.Xunit;
using Xunit;
using Xunit.Abstractions;

namespace Hangfire.Testing.Testcontainers;

public abstract class HangfireContainerFixture<TBuilderEntity, TContainerEntity>(IMessageSink messageSink, ContainerFixture<TBuilderEntity, TContainerEntity> fixture) : HangfireFixture(messageSink)
    where TBuilderEntity : IContainerBuilder<TBuilderEntity, TContainerEntity, IContainerConfiguration>, new()
    where TContainerEntity : IContainer
{
    protected TContainerEntity Container => fixture.Container;
    protected override async Task InitializeStorageAsync() => await ((IAsyncLifetime)fixture).InitializeAsync();
    protected override async Task DisposeStorageAsync() => await ((IAsyncLifetime)fixture).DisposeAsync();
}
