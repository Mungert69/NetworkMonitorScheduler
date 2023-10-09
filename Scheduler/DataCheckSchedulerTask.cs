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
    public class DataCheckScheduleTask : ScheduledProcessor
    {
        private bool firstRun;
        private ILogger _logger;
        public DataCheckScheduleTask(INetLoggerFactory loggerFactory, IServiceScopeFactory serviceScopeFactory, IConfiguration config) : base(serviceScopeFactory)
        {
            firstRun = true;
            _logger = loggerFactory.GetLogger("DataCheckScheduleTask");
            string scheduleStr = config.GetValue<string>("DataCheckSchedule");
            updateSchedule(scheduleStr);
        }
        public override Task ProcessInScope(IServiceProvider serviceProvider)
        {
            _logger.Info("SCHEDULE : Starting DataCheck schedule ");
            IServiceState serviceState = serviceProvider.GetService<IServiceState>();
            //Console.WriteLine("ScheduleService : Payment Processing starts here");
            try
            {

                if (!serviceState.IsMonitorCheckDataReady)
                {
                     _logger.Warn("DataCheck Service has not signalled it is ready sent dataCheck");
                
                }
                else {
                    serviceState.IsMonitorCheckDataReady = false;
                }
                    var serviceObj = new MonitorDataInitObj()
                    {
                        IsDataReady = true,
                        IsDataMessage=true
                    };
                    serviceState.RabbitRepo.Publish<MonitorDataInitObj>("dataCheck", serviceObj);

                    _logger.Info("Sent dataCheck event ");
    
              
            }
            catch (Exception e)
            {
                _logger.Error("Error : occured in DataCheckScheduleTask.ProcesInScope() : Error Was : " + e.Message.ToString());
            }
            //Console.WriteLine("ScheduleService : Ping Processing ends here");
            return Task.CompletedTask;
        }
    }
}