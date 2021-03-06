using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.OpenApi.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using Hangfire;
using Hangfire.SqlServer;
using HangFireTest.Job;
using TaskScheduler = System.Threading.Tasks.TaskScheduler;

namespace HangFireTest
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            GlobalConfiguration.Configuration
                .UseSqlServerStorage(Configuration.GetValue<string>("Hangfire:ConnectionString"), new SqlServerStorageOptions
                {
                    CommandBatchMaxTimeout = TimeSpan.FromMinutes(5),
                    SlidingInvisibilityTimeout = TimeSpan.FromMinutes(5),
                    QueuePollInterval = TimeSpan.Zero,
                    UseRecommendedIsolationLevel = true,
                    PrepareSchemaIfNecessary = true,
                    DisableGlobalLocks = true,
                });

            services.AddSingleton<ITaskScheduler, Job.TaskScheduler>();

            services.AddControllers();
            services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new OpenApiInfo { Title = "HangFireTest", Version = "v1" });
            });
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
                app.UseSwagger();
                app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "HangFireTest v1"));
            }

            BackgroundJobServerOptions serverOptions = new BackgroundJobServerOptions
            {
                WorkerCount = 5,
                HeartbeatInterval = new TimeSpan(0, 1, 0),
                ServerCheckInterval = new TimeSpan(0, 1, 0),
                SchedulePollingInterval = new TimeSpan(0, 1, 0),
                ServerName = $"{Environment.MachineName}",
                Queues = new[] {
                    Configuration.GetValue<string>("Hangfire:Jobs:NameTask1"),
                    Configuration.GetValue<string>("Hangfire:Jobs:NameTask2")
                }
            };

            app.UseHangfireServer(serverOptions);

            app.UseHangfireDashboard();

            app.UseRouting();

            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });
        }
    }
}
