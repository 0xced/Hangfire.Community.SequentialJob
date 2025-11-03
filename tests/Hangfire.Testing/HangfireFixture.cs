using System;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Xunit;
using Xunit.Abstractions;
using Xunit.Sdk;

namespace Hangfire.Testing;

public abstract class HangfireFixture(IMessageSink messageSink) : IAsyncLifetime
{
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
        var app = new HostApplicationBuilder();
        app.Services
            .AddHangfire(hangfire =>
            {
                var description = ConfigureStorage(hangfire);
                hangfire.UseLogProvider(new XunitLogProvider(description, Log));
            })
            .AddHangfireServer();
        _host = app.Build();

        await InitializeStorageAsync();

        _backgroundJobClient = _host.Services.GetRequiredService<IBackgroundJobClientV2>();

        await _host.StartAsync();
    }

    async Task IAsyncLifetime.DisposeAsync()
    {
        if (_host is not null)
        {
            await _host.StopAsync();
        }

        await DisposeStorageAsync();
    }

    protected abstract Task InitializeStorageAsync();

    protected abstract Task DisposeStorageAsync();

    protected abstract string ConfigureStorage(IGlobalConfiguration configuration);
}
