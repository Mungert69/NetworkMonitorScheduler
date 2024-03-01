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
    public class PredictScheduleTask : ScheduledProcessor
    {
        private bool firstRun;
        private ILogger _logger;
        public PredictScheduleTask(ILogger<PredictScheduleTask> logger, IServiceScopeFactory serviceScopeFactory, IConfiguration config) : base(serviceScopeFactory)
        {
            _logger = logger;
            firstRun = true;
            string scheduleStr = config.GetValue<string>("PredictSchedule") ?? "*/5 * * * *";
            updateSchedule(scheduleStr);
        }
        public override Task ProcessInScope(IServiceProvider serviceProvider)
        {
            string message=" SCHEDULE  : Starting Predict schedule  . ";
            IServiceState serviceState = serviceProvider.GetService<IServiceState>()!;
            //Console.WriteLine("ScheduleService : Ping Processing starts here");
            try
            {
                if (serviceState.IsPredictServiceReady)
                {
                    serviceState.RabbitRepo.PublishAsync("predictAlert", null);
                    message+=" Success : Sent predictAlert event. ";
                      serviceState.RabbitRepo.PublishAsync("mlCheckLatestHosts", null);
                      message+=" Success : Sent mlCheckLatestHosts event. ";
                    _logger.LogInformation(message);
                    serviceState.IsAlertServiceReady = false;
                }
                else
                {
                    serviceState.RabbitRepo.PublishAsync("predictWakeUp", null);
                    message +=" Warning : PredictService has not signalled it is ready ";
                    _logger.LogWarning(message);
                }
            }
            catch (Exception e)
            {
                message+=" Error : occured in PredictScheduleTask.ProcesInScope() . Error was : " + e.Message.ToString();
                _logger.LogError(message);
            }
            return Task.CompletedTask;
        }
    }
}
