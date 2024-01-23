using Microsoft.Extensions.DependencyInjection;
using NetworkMonitor.Scheduler.Services;
using NetworkMonitor.Objects.ServiceMessage;
using NetworkMonitor.Objects.Factory;
using NetworkMonitor.BackgroundService;
using System;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
namespace NetworkMonitor.Scheduler
{
    public class MonitorCheckScheduleTask: ScheduledProcessor
    {
        private bool firstRun;
        private ILogger _logger;
        public MonitorCheckScheduleTask(ILogger<MonitorCheckScheduleTask> logger, IServiceScopeFactory serviceScopeFactory, IConfiguration config) : base(serviceScopeFactory)
        {
            firstRun = true;
             _logger = logger;
            string scheduleStr = config.GetValue<string>("MonitorCheckSchedule") ?? "* * * * *";
            updateSchedule(scheduleStr);
        }
        public override Task ProcessInScope(IServiceProvider serviceProvider)
        {
            string message=" SCHEDULE : Starting MonitorCheck schedule . ";
            IServiceState serviceState = serviceProvider.GetService<IServiceState>()!;
            //Console.WriteLine("ScheduleService : Payment Processing starts here");
            try
            {
                        if (serviceState.IsMonitorCheckServiceReady)
                        {
                            serviceState.RabbitRepo.PublishAsync( "monitorCheck", null );
                            message+=" Success : Sent monitorCheck event . ";
                            _logger.LogInformation(message);
                            serviceState.IsMonitorCheckServiceReady = false;
                        }
                        else
                        {
                            serviceState.RabbitRepo.PublishAsync("monitorCheck",null );
                           message+=" Warning : MonitorCheck Service has not signalled it is ready sent monitorCheck . ";
                            _logger.LogWarning(message);
                        }
            }
            catch (Exception e)
            {
                message+=" Error : Failed to run MonitorCheck schedule  : Error Was : " + e.Message.ToString();
                _logger.LogError(message);
            }
            //Console.WriteLine("ScheduleService : Ping Processing ends here");
            return Task.CompletedTask;
        }
    }
}