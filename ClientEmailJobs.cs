using System;
using System.Runtime.InteropServices.ComTypes;
using System.Transactions;
using Hangfire;

namespace DSR_HangfireExample
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

        public void CreateEmailClientJobs()
        {
            int[] clients = _logic.GetActiveClients();

            using var transaction = new TransactionScope(TransactionScopeOption.RequiresNew);
            foreach (var client in clients)
            {
                _client.Enqueue<EmailLogic>(x => x.SendClientEmail(client));
            }
            transaction.Complete();
        }
    }
}