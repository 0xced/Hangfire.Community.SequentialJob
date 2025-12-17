[Hangfire](https://www.hangfire.io) extension that guarantees **sequential execution** of specific jobs

[![NuGet](https://img.shields.io/nuget/v/Hangfire.Community.SequentialJob.svg?label=NuGet&logo=NuGet)](https://www.nuget.org/packages/Hangfire.Community.SequentialJob/) [![Continuous Integration](https://img.shields.io/github/actions/workflow/status/0xced/Hangfire.Community.SequentialJob/continuous-integration.yml?branch=main&label=Continuous%20Integration&logo=GitHub)](https://github.com/0xced/Hangfire.Community.SequentialJob/actions/workflows/continuous-integration.yml)

## Why sequential jobs?

Some workloads must never overlap, for example:

- Importing data from an external system where each run depends on the previous one.
- Updating shared resources that are not fully concurrency-safe.
- Long-running workflows where the business rules require strict ordering.

With this package, you can keep using Hangfire's background processing and retries, while ensuring that certain jobs follow a strict sequence.

## Getting started

Add the [Hangfire.Community.SequentialJob](https://www.nuget.org/packages/Hangfire.Community.SequentialJob/) NuGet package to your project using the NuGet Package Manager or run the following command:

```sh
dotnet add package Hangfire.Community.SequentialJob
```

By decorating your Hangfire jobs with the `SequentialJobAttribute`, you can ensure that all jobs sharing the same sequence identifier are processed **one after another, in enqueueing order**, instead of running in parallel.

```csharp
[SequentialJob("orders")]
public class OrdersProcessor
{
    public async Task<string> RunAsync(int orderId)
    {
        // Actually process the order …
        
        return $"Order {orderId} processed";
    }
}
```

> [!TIP] 
> The sequence identifier can contain a [composite format string](https://learn.microsoft.com/en-us/dotnet/standard/base-types/composite-formatting) that will be replaced by the job actual arguments.

The example below uses one sequence per customer. For customer 1234, the sequence identifier will be `orders-1234`.

```csharp
[SequentialJob("orders-{1}")]
public class OrdersProcessor
{
    public async Task<string> RunAsync(int orderId, int customerId)
    {
        // Actually process the order …
        
        return $"Order {orderId} processed for customer {customerId}";
    }
}
```

## Credits

Thanks to Damien Braillard ([@DamienBraillard](https://github.com/DamienBraillard)) who worked with me on this package.
