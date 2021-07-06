using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Threading.Tasks;
using Hangfire;
using Hangfire.Dashboard;
using Hangfire.SqlServer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace DSR_HangfireExample
{
    public class Startup
    {
        public IWebHostEnvironment WebHostEnvironment { get; }

        public Startup(IWebHostEnvironment webHostEnvironment)
        {
            WebHostEnvironment = webHostEnvironment;
        }
        // This method gets called by the runtime. Use this method to add services to the container.
        // For more information on how to configure your application, visit https://go.microsoft.com/fwlink/?LinkID=398940
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddControllers();
            DbConnectionStringBuilder builder = new()
            {
                ["Server"] = WebHostEnvironment.EnvironmentName == "Docker" ? "db" :"localhost",
                ["Database"] ="hangfire",
                ["User ID"] = "sa",
                ["Password"] = "Your_password123"
            };

            services.AddHangfire(configuration =>
            {
                configuration
                    .UseSqlServerStorage(
                        () => new Microsoft.Data.SqlClient.SqlConnection(builder.ConnectionString))
                    .UseSimpleAssemblyNameTypeSerializer()
                    ;

                var retryFilter = GlobalJobFilters.Filters
                    .First(x => x.Instance is AutomaticRetryAttribute);
                GlobalJobFilters.Filters.Remove(retryFilter.Instance);
            });
            
            services.AddHangfireServer((provider, options) =>
            {
            });

            services.AddSingleton<EmailService>();
            services.AddSingleton<EmailLogic>();
            services.AddSingleton<ClientEmailJobs>();

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

            recurringJobManager.RemoveOutdatedJobs();
        }
    }
}