using System.Runtime.CompilerServices;
using AwesomeAssertions;

namespace Hangfire.Community.SequentialJob.Tests;

public class Initializer
{
    [ModuleInitializer]
    public static void ConfigurationEquivalency()
    {
        AssertionEngine.Configuration.Equivalency.Modify(options => options.WithStrictOrdering());
    }
}