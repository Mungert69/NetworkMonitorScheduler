using NetworkMonitor.Scheduler;
using NetworkMonitor.Scheduler.Services;
using NetworkMonitor.Objects.Factory;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using System.Threading;
using HostInitActions;

namespace NetworkMonitor
{
    public class Startup
    {
        private readonly CancellationTokenSource _cancellationTokenSource;
        public Startup(IConfiguration configuration)
        {
            _cancellationTokenSource = new CancellationTokenSource();
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
                services.AddSingleton<IHostedService, AIScheduleTask>();
            services.AddSingleton<IServiceState, ServiceState>();
            services.AddSingleton(_cancellationTokenSource);
            services.Configure<HostOptions>(s => s.ShutdownTimeout = TimeSpan.FromMinutes(5));
            services.AddControllers();
            services.AddSingleton<INetLoggerFactory, NetLoggerFactory>();
            services.AddAsyncServiceInitialization()
                   .AddInitAction<IServiceState>(async (serviceState) =>
                   {
                       await serviceState.Init();
                   });
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
