using NetworkMonitor.Scheduler;
using NetworkMonitor.Scheduler.Services;
using NetworkMonitor.Objects.Factory;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;
using HostInitActions;
using NetworkMonitor.Objects.Repository;
using NetworkMonitor.Utils.Helpers;

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
             services.AddLogging(builder =>
               {
                   builder.AddSimpleConsole(options =>
                        {
                            options.TimestampFormat = "yyyy-MM-dd HH:mm:ss ";
                            options.IncludeScopes = true;
                        });
               });

            services.AddSingleton<ISystemParamsHelper, SystemParamsHelper>();
            services.AddSingleton<IHostedService, DataSaveScheduleTask>();
            services.AddSingleton<IHostedService, MonitorCheckScheduleTask>();
            services.AddSingleton<IHostedService, AlertScheduleTask>();
            services.AddSingleton<IHostedService, PingScheduleTask>();
            services.AddSingleton<IHostedService, PaymentScheduleTask>();
            services.AddSingleton<IHostedService, DataCheckScheduleTask>();
            services.AddSingleton<IHostedService, DataPurgeScheduleTask>();
            services.AddSingleton<IHostedService,  ReportScheduleTask>();
            services.AddSingleton<IHostedService, HealthCheckScheduleTask>();
            services.AddSingleton<IHostedService, ResetScheduleTask>();
            services.AddSingleton<IHostedService, AIScheduleTask>();
            services.AddSingleton<IRabbitRepo, RabbitRepo>();
             services.AddSingleton<IFileRepo, FileRepo>();        
            services.AddSingleton<IRabbitListener, RabbitListener>();
            services.AddSingleton<IServiceState, ServiceState>();
            services.AddSingleton<IProcessorStateRabbitListner, ProcessorStateRabbitListner>();
            services.AddSingleton<IProcessorState, ProcessorState>();
          
            services.AddSingleton(_cancellationTokenSource);
            services.Configure<HostOptions>(s => s.ShutdownTimeout = TimeSpan.FromMinutes(5));
            services.AddControllers();
            services.AddAsyncServiceInitialization()
                   .AddInitAction<IServiceState>(async (serviceState) =>
                   {
                       await serviceState.Init();
                   })
                   .AddInitAction<IRabbitListener>((rabbitListener) =>
                    {
                        return Task.CompletedTask; 
                    })
                    .AddInitAction<IProcessorStateRabbitListner>((processorStateRabbitListener) =>
                    {
                        return Task.CompletedTask;
                    });
        }
       
    }
}
