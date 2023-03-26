using NetworkMonitor.Scheduler;
using NetworkMonitor.Scheduler.Services;
using NetworkMonitor.Objects.Factory;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using NetworkMonitor.Auth;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using System;
using MetroLog;

namespace NetworkMonitor
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        private IServiceCollection _services;

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            _services = services;

            services.AddSingleton<IHostedService, SaveScheduleTask>();
            services.AddSingleton<IHostedService, AlertScheduleTask>();
            services.AddSingleton<IHostedService, PingScheduleTask>();
            services.AddSingleton<IHostedService, PaymentScheduleTask>();
            services.AddSingleton<IHostedService, MonitorCheckScheduleTask>();
            services.AddSingleton<IHostedService, HealthCheckScheduleTask>();
            services.AddSingleton<IServiceState, ServiceState>();

            services.Configure<HostOptions>(s => s.ShutdownTimeout = TimeSpan.FromMinutes(5));

            services.AddControllers().AddDapr();
            services.AddSingleton<INetLoggerFactory, NetLoggerFactory>();



        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env, IHostApplicationLifetime applicationLifetime)
        {


            app.UseRouting();
            app.UseCloudEvents();
            app.UseEndpoints(endpoints =>
            {
                endpoints.MapSubscribeHandler();
                endpoints.MapControllers();
            });
        }
    }
}
