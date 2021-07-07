using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Hangfire;
using Hangfire.Dashboard;
using Hangfire.PostgreSql;
using Hangfire.SqlServer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Sinks.Elasticsearch;

namespace DSR_HangfireExample
{
    public class Startup
    {
        public IWebHostEnvironment WebHostEnvironment { get; }

        public Startup(IWebHostEnvironment webHostEnvironment)
        {
            WebHostEnvironment = webHostEnvironment;
        }

        private static string GetConnectionString(string server, string database, string user, string password)
        {
            DbConnectionStringBuilder connectionStringBuilder = new()
            {
                ["Server"] = server,
                ["Database"] = database,
                ["User ID"] = user,
                ["Password"] = password
            };
            return connectionStringBuilder.ConnectionString;
        }
        
        private string SqlDbHost => WebHostEnvironment.EnvironmentName == "Docker" ? "sql_db" :"localhost";
        private string PostgresDbHost => WebHostEnvironment.EnvironmentName == "Docker" ? "postgres_db" :"localhost";
        private string ElasticSearchHost => WebHostEnvironment.EnvironmentName == "Docker" ? "elasticsearch" :"localhost";

        // This method gets called by the runtime. Use this method to add services to the container.
        // For more information on how to configure your application, visit https://go.microsoft.com/fwlink/?LinkID=398940
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddControllers();
            services.AddLogging(builder =>
            {
                var loggerConfiguration = new LoggerConfiguration()
                    .WriteTo.Elasticsearch(new ElasticsearchSinkOptions(new Uri($"http://{ElasticSearchHost}:9200"))
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
            services.AddHangfire(configuration =>
            {
                configuration
                    .UseSqlServerStorage(
                        () => new Microsoft.Data.SqlClient.SqlConnection(
                            GetConnectionString(SqlDbHost, "hangfire", "sa", "Your_password123")),
                        new SqlServerStorageOptions())
                    //Uncomment to use Postgre Storage
                    // .UsePostgreSqlStorage(GetConnectionString(PostgresDbHost, "hangfire", "postgres", "test"),
                    //     new PostgreSqlStorageOptions
                    //     {
                    //         InvisibilityTimeout = TimeSpan.FromHours(2)
                    //     })
                    .UseSimpleAssemblyNameTypeSerializer()
                    ;

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

            recurringJobManager.RemoveOutdatedJobs();
        }
    }
}