using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using Hangfire;
using Hangfire.Common;
using Hangfire.Storage;

namespace DSR_HangfireExample
{
    public interface IRecurringJobCleanupManager : IRecurringJobManager
    {
        void RemoveOutdatedJobs();
    }
    public class RecurringJobCleanupManager : IRecurringJobCleanupManager
    {
        private readonly IRecurringJobManager _manager;
        private readonly JobStorage _storage;
        private readonly HashSet<string> _existingJobs = new();

        public RecurringJobCleanupManager(IRecurringJobManager manager, JobStorage storage)
        {
            _manager = manager;
            _storage = storage;
        }

        public void Trigger(string recurringJobId)
        {
            _manager.Trigger(recurringJobId);
        }

        public void AddOrUpdate(string recurringJobId, Job job, string cronExpression, RecurringJobOptions options)
        {
            _existingJobs.Add(recurringJobId);
            _manager.AddOrUpdate(recurringJobId, job, cronExpression, options);
        }

        public void RemoveIfExists(string recurringJobId)
        {
            _existingJobs.Remove(recurringJobId);
            _manager.RemoveIfExists(recurringJobId);
        }

        public void RemoveOutdatedJobs()
        {
            using var connection = _storage.GetConnection();
            var recurringJobDtos = connection.GetRecurringJobs();

            var outdatedJobs = recurringJobDtos.Select(x => x.Id)
                .Except(_existingJobs)
                .ToArray();
            
            foreach (string outdatedJobId in outdatedJobs)
            {
                _manager.RemoveIfExists(outdatedJobId);
            }
        }
    }
}