using System;
using Hangfire.Common;
using Hangfire.States;
using Hangfire.Storage;

namespace DSR_HangfireExample
{
    public class JobExpirationAttribute : JobFilterAttribute, IApplyStateFilter
    {
        public TimeSpan ExpirationDelay { get; }
        public JobExpirationAttribute(int daysToExpire) => ExpirationDelay = TimeSpan.FromDays(daysToExpire);

        public void OnStateApplied(ApplyStateContext context, IWriteOnlyTransaction transaction)
        {
            if (context.NewState.IsFinal) //Succeeded or Deleted states
            {
                context.JobExpirationTimeout = ExpirationDelay;
            }
        }

        public void OnStateUnapplied(ApplyStateContext context, IWriteOnlyTransaction transaction)
        {
        }
    }
}