using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using DSR_HangfireExample.HangfireExtensions;
using DSR_HangfireExample.Jobs;
using Hangfire;
using Hangfire.Console;
using Hangfire.Console.Extensions;
using Hangfire.Dashboard;
using Hangfire.PostgreSql;
using Hangfire.SqlServer;
using Hangfire.Tags;
using Hangfire.Tags.PostgreSql;
using Hangfire.Tags.SqlServer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Sinks.Elasticsearch;

namespace DSR_HangfireExample
{
    public class Startup
    {
        public IConfiguration Configuration { get; }
        public IWebHostEnvironment WebHostEnvironment { get; }

        public Startup(IConfiguration configuration,
            IWebHostEnvironment webHostEnvironment)
        {
            Configuration = configuration;
            WebHostEnvironment = webHostEnvironment;
        }
        
        // This method gets called by the runtime. Use this method to add services to the container.
        // For more information on how to configure your application, visit https://go.microsoft.com/fwlink/?LinkID=398940
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddControllers();
            services.AddLogging(builder =>
            {
                builder.ClearProviders();
                
                var loggerConfiguration = new LoggerConfiguration()
                    .MinimumLevel.Verbose()
                    .WriteTo.Elasticsearch(new ElasticsearchSinkOptions(new Uri(Configuration["ElasticSearchUrl"]))
                    {
                        AutoRegisterTemplate = true,
                        IndexFormat = $"{Assembly.GetExecutingAssembly().GetName().Name.ToLower()}-{DateTime.UtcNow:yyyy-MM}"
                    });
            
                builder.AddSerilog(loggerConfiguration.CreateLogger());
                builder.AddSimpleConsole(options =>
                {
                    options.IncludeScopes = true;
                });
            });
            
            services.AddSingleton<JobActivator, CustomJobActivator>();
            services.AddHangfireConsoleExtensions();
            services.AddHangfire(configuration =>
            {
                switch (Configuration["HangfireStorageType"])
                {
                    case "SqlServer":
                        configuration
                            .UseSqlServerStorage(() =>
                                    new Microsoft.Data.SqlClient.SqlConnection(Configuration.GetConnectionString("SqlServer")),
                                new SqlServerStorageOptions())
                            .UseTagsWithSql();
                        break;
                    case "Postgre":
                        configuration
                            .UsePostgreSqlStorage(Configuration.GetConnectionString("Postgre"),
                                new PostgreSqlStorageOptions
                                {
                                    InvisibilityTimeout = TimeSpan.FromHours(2)
                                })
                            .UseTagsWithPostgreSql();
                        break;
                    default:
                        throw new NotSupportedException($"StorageType {Configuration["HangfireStorageType"]} is not supported");
                }

                configuration
                    .UseSimpleAssemblyNameTypeSerializer()
                    .UseConsole();

                var retryFilter = GlobalJobFilters.Filters
                    .First(x => x.Instance is AutomaticRetryAttribute);
                GlobalJobFilters.Filters.Remove(retryFilter.Instance);
            });
            
            services.AddHangfireServer(options =>
            {
                options.Queues = new[] {"default"};
                options.WorkerCount = 1;
            });
            
            services.AddHangfireServer(options =>
            {
                options.Queues = new[] {"10-critical", "20-normal"};
                options.WorkerCount = 2;
            });

            services.AddSingleton<EmailService>();
            services.AddSingleton<EmailLogic>();
            services.AddSingleton<ClientEmailJobs>();
            services.AddSingleton<LongRunningJobs>();
            services.AddSingleton<ProgressJobs>();

            services.Decorate<IRecurringJobManager, RecurringJobCleanupManager>();
            services.AddSingleton(provider =>
                (IRecurringJobCleanupManager) provider.GetRequiredService<IRecurringJobManager>()
            );
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseRouting();
            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });
            
            app.UseHangfireDashboard(options: new DashboardOptions
            {
                //Empty list to remove default 'LocalRequestsOnlyAuthorizationFilter' filter
                //to allow access to Hangfire Dashboard in docker without authentication
                Authorization = new List<IDashboardAuthorizationFilter>()
            });

            var recurringJobManager = app.ApplicationServices
                .GetRequiredService<IRecurringJobCleanupManager>();

            recurringJobManager.AddOrUpdate<ClientEmailJobs>(
                "SendEmailToAllClients",
                x => x.CreateEmailClientJobs(),
                Cron.Minutely);
            
            recurringJobManager.AddOrUpdate<LongRunningJobs>(
                "LongRunningJob",
                x => x.DelayJob(TimeSpan.FromSeconds(30)),
                Cron.Never);
            
            recurringJobManager.AddOrUpdate<ProgressJobs>(
                "ArrayProcessingJob-10",
                x => x.ArrayProcessingJob(10),
                "*/2 * * * *");

            recurringJobManager.AddOrUpdate<ProgressJobs>(
                "ArrayProcessingJob-10000",
                x => x.ArrayProcessingJob(10_000),
                "*/2 * * * *");

            recurringJobManager.RemoveOutdatedJobs();
        }
    }
}