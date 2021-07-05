using Hangfire;
using Microsoft.AspNetCore.Mvc;

namespace DSR_HangfireExample
{
    [ApiController]
    [Route("[controller]/[action]")]
    public class EnqueueController : ControllerBase
    {
        private readonly IBackgroundJobClient _client;
        public EnqueueController(IBackgroundJobClient client) => _client = client;

        [HttpPost]
        public ActionResult<string> CreateSendEmailJob(
            [FromQuery] string to,
            [FromBody] string body)
        {
            string jobId = _client.Enqueue<EmailService>(
                x => x.SendEmail(to, body));
            return jobId;
        }

        [HttpPost]
        public ActionResult<string> CreateFailingSendEmailJob(
            [FromQuery] string to,
            [FromBody] string body)
        {
            string jobId = _client.Enqueue<EmailService>(
                x => x.FailingSendEmail(to, body));
            return jobId;
        }
    }
}