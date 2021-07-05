using System;
using Hangfire;
using Microsoft.Extensions.Logging;

namespace DSR_HangfireExample
{
    public class EmailService
    {
        private readonly ILogger<EmailService> _logger;
        public EmailService(ILogger<EmailService> logger) => _logger = logger;

        [JobDisplayName("Send email to {0}")]
        public void SendEmail(string to, string body)
        {
            _logger.LogInformation("Email sent to {EmailServiceTo}:{EmailServiceBody}",
                to, body);
        }

        [JobDisplayName("Failing Send email to {0}")]
        public void FailingSendEmail(string to, string body)
        {
            throw new Exception("Job is failed for demo purposes");
        }
    }
}