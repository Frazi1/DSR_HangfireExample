using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace DSR_HangfireExample.Jobs
{
    public class LongRunningJobs
    {
        private readonly ILogger<LongRunningJobs> _logger;

        public LongRunningJobs(ILogger<LongRunningJobs> logger)
        {
            _logger = logger;
        }

        public async Task DelayJob(TimeSpan delay)
        {
            _logger.LogInformation("Delay job started");
            await Task.Delay(delay);
            _logger.LogInformation("Delay job finished");
        }
    }
}