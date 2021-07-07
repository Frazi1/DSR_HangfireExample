using System;
using System.Threading.Tasks;
using Hangfire.Tags.Attributes;
using Microsoft.Extensions.Logging;

namespace DSR_HangfireExample.Jobs
{
    [Tag("long-running-jobs")]
    public class LongRunningJobs
    {
        private readonly ILogger<LongRunningJobs> _logger;

        public LongRunningJobs(ILogger<LongRunningJobs> logger)
        {
            _logger = logger;
        }

        [Tag("delay-job-{0}")]
        public async Task DelayJob(TimeSpan delay)
        {
            _logger.LogInformation("Delay job started");
            await Task.Delay(delay);
            _logger.LogInformation("Delay job finished");
        }
    }
}