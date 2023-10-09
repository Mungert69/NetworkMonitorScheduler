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
using NetworkMonitor.Objects.ServiceMessage;
namespace NetworkMonitor.Scheduler
{
    public class DataSaveScheduleTask : ScheduledProcessor
    {
        private ILogger _logger;
        public DataSaveScheduleTask( INetLoggerFactory loggerFactory, IServiceScopeFactory serviceScopeFactory, IConfiguration config) : base(serviceScopeFactory)
        {
              _logger = loggerFactory.GetLogger("DataSaveScheduleTask");
            string scheduleStr = config.GetValue<string>("DataSaveSchedule");
            updateSchedule(scheduleStr);
        }
        public override Task ProcessInScope(IServiceProvider serviceProvider)
        {
            _logger.Info("SCHEDULE  : Starting Save schedule ");
            IServiceState serviceState = serviceProvider.GetService<IServiceState>();
            //Console.WriteLine("ScheduleService : Ping Processing starts here");
            try
            {
                    if (serviceState.IsMonitorDataSaveReady)
                    {
                        serviceState.RabbitRepo.Publish( "saveData", null);
                        _logger.Info("Sent saveData event.");
                        serviceState.IsMonitorDataSaveReady = false;
                    }
                    else
                    {
                        var serviceObj=new MonitorDataInitObj(){
                            IsDataSaveReady=true,
                            IsDataSaveMessage=true
                        };
                        serviceState.RabbitRepo.Publish<MonitorDataInitObj>("dataCheck", serviceObj);
                        _logger.Warn("DataSave has not signalled it is ready");
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
