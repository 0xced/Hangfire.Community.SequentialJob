using System.Collections.Generic;
using System.Reflection;
using Hangfire.Common;

namespace Hangfire.SequentialJob;

/// <summary>
/// A <see cref="IJobFilterProvider"/> that provides <see cref="SequentialExecutionFilter"/> filters for jobs decorated with <see cref="SequentialJobAttribute"/>.
/// </summary>
public class SequentialExecutionFilterProvider : IJobFilterProvider
{
    /// <inheritdoc/>
    public IEnumerable<JobFilter> GetFilters(Job? job)
    {
        var attribute = job?.Type.GetCustomAttribute<SequentialJobAttribute>();
        if (attribute != null)
        {
            var filter = new SequentialExecutionFilter(attribute.SequenceId);
            yield return new JobFilter(filter, JobFilterScope.Method, order: null);
        }
    }
}