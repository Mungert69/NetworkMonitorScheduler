
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Dapr.Client;
using NetworkMonitor.Scheduler.Services;

namespace NetworkMonitor.Scheduler
{
    public class AlertScheduleTask : ScheduledProcessor
    {
        private bool firstRun;
        private ILogger _logger;
        private DaprClient _daprClient;

        public AlertScheduleTask(DaprClient daprClient, ILogger<AlertScheduleTask> logger, IServiceScopeFactory serviceScopeFactory, IConfiguration config) : base(serviceScopeFactory)
        {
            _daprClient = daprClient;
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
                bool isDaprReady = _daprClient.CheckHealthAsync().Result;
                var daprMetadata = new Dictionary<string, string>();
                daprMetadata.Add("ttlInSeconds", "60");
                if (isDaprReady)
                {
                    _logger.LogInformation("Dapr Client Status is healthy");
                    if (serviceState.IsAlertServiceReady)
                    {


                        _daprClient.PublishEventAsync("pubsub", "monitorAlert", daprMetadata);
                        _logger.LogInformation("Sent monitorAlert event.");
                        serviceState.IsAlertServiceReady = false;
                    }
                    else
                    {
                        _daprClient.PublishEventAsync("pubsub", "serviceWakeUp", daprMetadata);

                        _logger.LogWarning("AlertService has not signalled it is ready");
                    }
                }
                else
                {
                    _logger.LogCritical("Dapr Client Status is not healthy");
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
