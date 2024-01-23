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
            string scheduleStr = config.GetValue<string>("DataPurgeSchedule") ?? "0 1 * * 0";
            updateSchedule(scheduleStr);
        }
        public override Task ProcessInScope(IServiceProvider serviceProvider)
        {
            string message=" SCHEDULE  : Starting Data Purge schedule  . ";
            IServiceState serviceState = serviceProvider.GetService<IServiceState>()!;
            try
            {
                    if (serviceState.IsMonitorDataPurgeReady)
                    {
                        serviceState.RabbitRepo.PublishAsync( "dataPurge", null);
                        message+=" Success : Sent purgeData event.";
                        _logger.LogInformation(message);
                        serviceState.IsMonitorDataPurgeReady = false;
                    }
                    else
                    {
                         var serviceObj=new MonitorDataInitObj(){
                            IsDataPurgeReady=true,
                            IsDataPurgeMessage=true
                        };
                        serviceState.RabbitRepo.PublishAsync<MonitorDataInitObj>("dataWakeUp", serviceObj);
                        message+=" Warning : MonitorData has not signalled it is ready. Sent dataWakeUp event. ";
                        _logger.LogWarning(message);
                    }
            }
            catch (Exception e)
            {
                message+=" Error : occured in DataPurgeScheduleTask.ProcesInScope() : Error Was : " + e.Message.ToString();
                _logger.LogError(message);
            }
            return Task.CompletedTask;
        }
    }
}
