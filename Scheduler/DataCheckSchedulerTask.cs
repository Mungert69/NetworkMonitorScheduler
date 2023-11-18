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
            string scheduleStr = config.GetValue<string>("DataCheckSchedule") ?? "* * * * *";
            updateSchedule(scheduleStr);
        }
        public override Task ProcessInScope(IServiceProvider serviceProvider)
        {
            string message = " SCHEDULE : Starting DataCheck schedule . ";
            IServiceState serviceState = serviceProvider.GetService<IServiceState>()!;
            //Console.WriteLine("ScheduleService : Payment Processing starts here");
            try
            {

                if (!serviceState.IsMonitorCheckDataReady)
                {
                    var serviceObj = new MonitorDataInitObj()
                    {
                        IsDataReady = true,
                        IsDataMessage = true
                    };
                    serviceState.RabbitRepo.Publish<MonitorDataInitObj>("dataCheck", serviceObj);

                    message += " Warning : DataCheck Service has not signalled it is ready . Sent dataCheck ";
                    _logger.LogWarning(message);

                }
                else
                {
                    message+=" Success : Ran DataCheck schedule . ";
                    _logger.LogInformation(message);
                    serviceState.IsMonitorCheckDataReady = false;
                }



            }
            catch (Exception e)
            {
                message+="Error : occured in DataCheckScheduleTask.ProcesInScope() : Error Was : " + e.Message.ToString();
                _logger.LogError(message);
            }

            return Task.CompletedTask;
        }
    }
}