using Microsoft.Extensions.DependencyInjection;
using NetworkMonitor.Objects.Factory;
using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using NetworkMonitor.Scheduler.Services;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using MetroLog;
using NetworkMonitor.BackgroundService;
namespace NetworkMonitor.Scheduler
{
    public class SaveScheduleTask : ScheduledProcessor
    {
        private ILogger _logger;
        public SaveScheduleTask( INetLoggerFactory loggerFactory, IServiceScopeFactory serviceScopeFactory, IConfiguration config) : base(serviceScopeFactory)
        {
              _logger = loggerFactory.GetLogger("SaveScheduleTask");
            string scheduleStr = config.GetValue<string>("SaveSchedule");
            updateSchedule(scheduleStr);
        }
        public override Task ProcessInScope(IServiceProvider serviceProvider)
        {
            _logger.Info("SCHEDULE  : Starting Save schedule ");
            IServiceState serviceState = serviceProvider.GetService<IServiceState>();
            //Console.WriteLine("ScheduleService : Ping Processing starts here");
            try
            {
                    if (serviceState.IsMonitorServiceReady)
                    {
                        serviceState.RabbitRepo.Publish( "monitorSaveData", null);
                        _logger.Info("Sent monitorSaveData event.");
                        serviceState.IsMonitorServiceReady = false;
                    }
                    else
                    {
                        serviceState.RabbitRepo.Publish("serviceWakeUp", null);
                        _logger.Warn("MonitorService has not signalled it is ready");
                    }
            }
            catch (Exception e)
            {
                _logger.Error("Error : occured in SaveScheduleTask.ProcesInScope() : Error Was : " + e.Message.ToString());
            }
            Console.WriteLine("ScheduleService : Saving data processing ends here");
            return Task.CompletedTask;
        }
    }
}
