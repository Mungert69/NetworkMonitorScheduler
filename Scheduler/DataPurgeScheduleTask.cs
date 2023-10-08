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
    public class DataPurgeScheduleTask : ScheduledProcessor
    {
        private ILogger _logger;
        public DataPurgeScheduleTask( INetLoggerFactory loggerFactory, IServiceScopeFactory serviceScopeFactory, IConfiguration config) : base(serviceScopeFactory)
        {
              _logger = loggerFactory.GetLogger("DataPurgeScheduleTask");
            string scheduleStr = config.GetValue<string>("DataPurgeSchedule");
            updateSchedule(scheduleStr);
        }
        public override Task ProcessInScope(IServiceProvider serviceProvider)
        {
            _logger.Info("SCHEDULE  : Starting Data Purge schedule ");
            IServiceState serviceState = serviceProvider.GetService<IServiceState>();
            try
            {
                    if (serviceState.IsMonitorDataReady)
                    {
                        serviceState.RabbitRepo.Publish( "dataPurge", null);
                        _logger.Info("Sent purgeData event.");
                        serviceState.IsMonitorDataReady = false;
                    }
                    else
                    {
                        serviceState.RabbitRepo.Publish("dataWakeUp", null);
                        _logger.Warn("MonitorData has not signalled it is ready");
                    }
            }
            catch (Exception e)
            {
                _logger.Error("Error : occured in DataPurgeScheduleTask.ProcesInScope() : Error Was : " + e.Message.ToString());
            }
            Console.WriteLine("ScheduleService : Saving data processing ends here");
            return Task.CompletedTask;
        }
    }
}
