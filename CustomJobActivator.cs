using System;
using Hangfire;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace DSR_HangfireExample
{
    public class CustomJobActivatorScope : JobActivatorScope
    {
        private readonly IServiceScope _serviceScope;
        private readonly IDisposable _logScope;

        public CustomJobActivatorScope(
            IServiceScope serviceScope,
            BackgroundJob backgroundJob)
        {
            _serviceScope = serviceScope;

            var logger = serviceScope.ServiceProvider
                .GetRequiredService<ILogger<CustomJobActivatorScope>>();
            
            _logScope = logger.BeginScope("{HangfireJobID}", backgroundJob.Id);
        }

        public override object Resolve(Type type)
        {
            return ActivatorUtilities.CreateInstance(_serviceScope.ServiceProvider, type);
        }

        public override void DisposeScope()
        {
            _logScope.Dispose();
            _serviceScope.Dispose();
        }
    }
    
    public class CustomJobActivator : JobActivator
    {
        private readonly IServiceScopeFactory _serviceScopeFactory;
        
        public CustomJobActivator(IServiceScopeFactory serviceScopeFactory) 
            => _serviceScopeFactory = serviceScopeFactory;

        public override JobActivatorScope BeginScope(JobActivatorContext context) 
            => new CustomJobActivatorScope(
                _serviceScopeFactory.CreateScope(),
                context.BackgroundJob);
    }
}