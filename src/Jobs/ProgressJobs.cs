using System.Linq;
using System.Threading.Tasks;
using Hangfire;
using Hangfire.Console;
using Hangfire.Console.Extensions;
using Hangfire.Tags.Attributes;
using Microsoft.Extensions.Logging;

namespace DSR_HangfireExample.Jobs
{
    public class ProgressJobs
    {
        private readonly ILogger<ProgressJobs> _logger;
        private readonly IPerformingContextAccessor _performingContextAccessor;

        public ProgressJobs(
            ILogger<ProgressJobs> logger,
            IPerformingContextAccessor performingContextAccessor)
        {
            _logger = logger;
            _performingContextAccessor = performingContextAccessor;
        }
        
        [Tag("array-processing") ,Tag("array-processing-job-{0}")]
	    [JobDisplayName("ArrayProcessingJob-{0}")]
        public async Task ArrayProcessingJob(int count)
        {
            _logger.LogInformation("Started processing array of items");
            var items = Enumerable.Range(1, count);
            foreach(var i in items
                .WithProgress(_performingContextAccessor.Get(),"Array progress", count: count)
            )
            {
                _logger.LogTrace("Item {arrayItem} processed", i);
            }
            _logger.LogInformation("Finished processing array of items");
        }
    }
}
