using Microsoft.Extensions.DependencyInjection;
using NetworkMonitor.Objects.Factory;
using NetworkMonitor.Objects.ServiceMessage;
using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using NetworkMonitor.Scheduler.Services;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using NetworkMonitor.BackgroundService;
namespace NetworkMonitor.Scheduler
{
    public class DataPurgeScheduleTask : ScheduledProcessor
    {
        private ILogger _logger;
        public DataPurgeScheduleTask( ILogger<DataPurgeScheduleTask> logger, IServiceScopeFactory serviceScopeFactory, IConfiguration config) : base(serviceScopeFactory)
        {
              _logger = logger;
            string scheduleStr = config.GetValue<string>("DataPurgeSchedule");
            updateSchedule(scheduleStr);
        }
        public override Task ProcessInScope(IServiceProvider serviceProvider)
        {
            _logger.LogInformation("SCHEDULE  : Starting Data Purge schedule ");
            IServiceState serviceState = serviceProvider.GetService<IServiceState>();
            try
            {
                    if (serviceState.IsMonitorDataPurgeReady)
                    {
                        serviceState.RabbitRepo.Publish( "dataPurge", null);
                        _logger.LogInformation("Sent purgeData event.");
                        serviceState.IsMonitorDataPurgeReady = false;
                    }
                    else
                    {
                         var serviceObj=new MonitorDataInitObj(){
                            IsDataPurgeReady=true,
                            IsDataPurgeMessage=true
                        };
                        serviceState.RabbitRepo.Publish<MonitorDataInitObj>("dataWakeUp", serviceObj);
                        _logger.LogWarning("MonitorData has not signalled it is ready");
                    }
            }
            catch (Exception e)
            {
                _logger.LogError("Error : occured in DataPurgeScheduleTask.ProcesInScope() : Error Was : " + e.Message.ToString());
            }
            Console.WriteLine("ScheduleService : Saving data processing ends here");
            return Task.CompletedTask;
        }
    }
}
