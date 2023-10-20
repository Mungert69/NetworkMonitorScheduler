using Microsoft.Extensions.DependencyInjection;
using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using NetworkMonitor.Scheduler.Services;
using NetworkMonitor.Objects.Factory;
using NetworkMonitor.BackgroundService;
namespace NetworkMonitor.Scheduler
{
    public class AlertScheduleTask : ScheduledProcessor
    {
        private bool firstRun;
        private ILogger _logger;
        public AlertScheduleTask(ILogger<AlertScheduleTask> logger, IServiceScopeFactory serviceScopeFactory, IConfiguration config) : base(serviceScopeFactory)
        {
            _logger = logger;
            firstRun = true;
            string scheduleStr = config.GetValue<string>("AlertSchedule");
            updateSchedule(scheduleStr);
        }
        public override Task ProcessInScope(IServiceProvider serviceProvider)
        {
            _logger.LogInformation("SCHEDULE  : Starting Alert schedule ");
            IServiceState serviceState = serviceProvider.GetService<IServiceState>();
            //Console.WriteLine("ScheduleService : Ping Processing starts here");
            try
            {
                if (serviceState.IsAlertServiceReady)
                {
                    serviceState.RabbitRepo.Publish("monitorAlert", null);
                    _logger.LogInformation("Sent monitorAlert event.");
                    serviceState.IsAlertServiceReady = false;
                }
                else
                {
                    serviceState.RabbitRepo.Publish("serviceWakeUp", null);
                    _logger.LogWarning("AlertService has not signalled it is ready");
                }
            }
            catch (Exception e)
            {
                _logger.LogError("Error : occured in AlertScheduleTask.ProcesInScope() . Error was : " + e.Message.ToString());
            }
            return Task.CompletedTask;
        }
    }
}
