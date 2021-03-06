using System.Transactions;
using Hangfire;
using Hangfire.States;
using Hangfire.Tags.Attributes;

namespace DSR_HangfireExample.Jobs
{
    public class ClientEmailJobs
    {
        private readonly IBackgroundJobClient _client;
        private readonly EmailLogic _logic;

        public ClientEmailJobs(IBackgroundJobClient client,
            EmailLogic logic)
        {
            _client = client;
            _logic = logic;
        }

        private static bool IsImportantClient(int clientId) => clientId % 2 == 1;
        
        [Tag("email-jobs-generator")]
        public void CreateEmailClientJobs()
        {
            int[] clients = _logic.GetActiveClients();

            using var transaction = new TransactionScope(TransactionScopeOption.RequiresNew);
            foreach (var client in clients)
            {
                string queue = IsImportantClient(client) ? "10-critical" : "20-normal";
                
                _client.Create<EmailLogic>(
                    x => x.SendClientEmail(client),
                    new EnqueuedState(queue));
            }
            transaction.Complete();
        }
    }
}