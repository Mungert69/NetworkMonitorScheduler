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
    public class DataCheckScheduleTask : ScheduledProcessor
    {
        private bool firstRun;
        private ILogger _logger;
        public DataCheckScheduleTask(ILogger<DataCheckScheduleTask> logger, IServiceScopeFactory serviceScopeFactory, IConfiguration config) : base(serviceScopeFactory)
        {
            firstRun = true;
            _logger = logger;
            string scheduleStr = config.GetValue<string>("DataCheckSchedule");
            updateSchedule(scheduleStr);
        }
        public override Task ProcessInScope(IServiceProvider serviceProvider)
        {
            _logger.LogInformation("SCHEDULE : Starting DataCheck schedule ");
            IServiceState serviceState = serviceProvider.GetService<IServiceState>();
            //Console.WriteLine("ScheduleService : Payment Processing starts here");
            try
            {

                if (!serviceState.IsMonitorCheckDataReady)
                {
                     _logger.LogWarning("DataCheck Service has not signalled it is ready sent dataCheck");
                
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

                    _logger.LogInformation("Sent dataCheck event ");
    
              
            }
            catch (Exception e)
            {
                _logger.LogError("Error : occured in DataCheckScheduleTask.ProcesInScope() : Error Was : " + e.Message.ToString());
            }
            //Console.WriteLine("ScheduleService : Ping Processing ends here");
            return Task.CompletedTask;
        }
    }
}