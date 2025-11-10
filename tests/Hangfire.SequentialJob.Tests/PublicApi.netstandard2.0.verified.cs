[assembly: System.CLSCompliant(true)]
[assembly: System.Reflection.AssemblyMetadata("RepositoryUrl", "")]
[assembly: System.Runtime.Versioning.TargetFramework(".NETStandard,Version=v2.0", FrameworkDisplayName=".NET Standard 2.0")]
namespace Hangfire.SequentialJob
{
    public sealed class SequentialJobAttribute : Hangfire.Common.JobFilterAttribute, Hangfire.States.IElectStateFilter
    {
        public SequentialJobAttribute(string sequenceId, string? distributedLockName = null, string? sequenceIdParameterName = null, string? lastJobIdHashName = null, int timeoutInSeconds = 10) { }
        public string DistributedLockName { get; }
        public string LastJobIdHashName { get; }
        public string SequenceId { get; }
        public string SequenceIdParameterName { get; }
        public System.TimeSpan Timeout { get; }
    }
}