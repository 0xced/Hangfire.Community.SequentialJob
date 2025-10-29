[assembly: System.CLSCompliant(true)]
[assembly: System.Reflection.AssemblyMetadata("RepositoryUrl", "")]
[assembly: System.Runtime.Versioning.TargetFramework(".NETCoreApp,Version=v8.0", FrameworkDisplayName=".NET 8.0")]
namespace Hangfire.SequentialJob
{
    public class SequentialExecutionFilter : Hangfire.States.IElectStateFilter
    {
        public SequentialExecutionFilter(string sequenceId) { }
        public string DistributedLockName { get; init; }
        public string LastJobIdHashName { get; init; }
        public string SequenceId { get; }
        public string SequenceIdParameterName { get; init; }
        public void OnStateElection(Hangfire.States.ElectStateContext context) { }
    }
    public class SequentialExecutionFilterProvider : Hangfire.Common.IJobFilterProvider
    {
        public SequentialExecutionFilterProvider() { }
        public System.Collections.Generic.IEnumerable<Hangfire.Common.JobFilter> GetFilters(Hangfire.Common.Job? job) { }
    }
    [System.AttributeUsage(System.AttributeTargets.Class)]
    public sealed class SequentialJobAttribute : System.Attribute
    {
        public SequentialJobAttribute(string sequenceId) { }
        public string SequenceId { get; }
    }
}