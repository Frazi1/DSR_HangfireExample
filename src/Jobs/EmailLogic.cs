using System.Linq;
using System.Threading.Tasks;
using DSR_HangfireExample.HangfireExtensions;
using Hangfire;
using Hangfire.Tags.Attributes;

namespace DSR_HangfireExample.Jobs
{
    [Tag("email-jobs")]
    public class EmailLogic
    {
        private readonly EmailService _service;
        public EmailLogic(EmailService service) => _service = service;

        public int[] GetActiveClients()
            => Enumerable.Range(1, 10).ToArray();

        [JobDisplayName("Send email to client {0}")]
        [JobExpiration(365)]
        [Tag("email-client-{0}")]
        public async Task SendClientEmail(int clientId)
        {
            await _service.SendEmail($"client_{clientId}@some.com",
                $"Dear Client {clientId}," +
                "Thanks for using our service!");
        }
    }
}