using Microsoft.Extensions.DependencyInjection;
using NetworkMonitor.Scheduler.Services;
using NetworkMonitor.Objects.ServiceMessage;
using NetworkMonitor.Objects.Factory;
using NetworkMonitor.BackgroundService;
using System;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using MetroLog;
using System.Collections.Generic;
namespace NetworkMonitor.Scheduler
{
    public class MonitorCheckScheduleTask: ScheduledProcessor
    {
        private bool firstRun;
        private ILogger _logger;
        public MonitorCheckScheduleTask(INetLoggerFactory loggerFactory, IServiceScopeFactory serviceScopeFactory, IConfiguration config) : base(serviceScopeFactory)
        {
            firstRun = true;
             _logger = loggerFactory.GetLogger("MonitorCheckScheduleTask");
            string scheduleStr = config.GetValue<string>("MonitorCheckSchedule");
            updateSchedule(scheduleStr);
        }
        public override Task ProcessInScope(IServiceProvider serviceProvider)
        {
            _logger.Info("SCHEDULE : Starting MonitorCheck schedule ");
            IServiceState serviceState = serviceProvider.GetService<IServiceState>();
            //Console.WriteLine("ScheduleService : Payment Processing starts here");
            try
            {
                        if (serviceState.IsMonitorCheckServiceReady)
                        {
                            serviceState.RabbitRepo.Publish( "monitorCheck", null );
                            _logger.Info("Sent monitorCheck event ");
                            serviceState.IsMonitorCheckServiceReady = false;
                        }
                        else
                        {
                            serviceState.RabbitRepo.Publish("monitorCheck",null );
                            _logger.Warn("MonitorCheck Service has not signalled it is ready sent monitorCheck");
                        }
            }
            catch (Exception e)
            {
                _logger.Error("Error : occured in MonitorCheckScheduleTask.ProcesInScope() : Error Was : " + e.Message.ToString());
            }
            //Console.WriteLine("ScheduleService : Ping Processing ends here");
            return Task.CompletedTask;
        }
    }
}