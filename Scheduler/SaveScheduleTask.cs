
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using NetworkMonitor.Scheduler.Services;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Dapr.Client;


namespace NetworkMonitor.Scheduler
{
    public class SaveScheduleTask : ScheduledProcessor
    {
        private ILogger _logger;
        private DaprClient _daprClient;

        public SaveScheduleTask(DaprClient daprClient, ILogger<SaveScheduleTask> logger, IServiceScopeFactory serviceScopeFactory, IConfiguration config) : base(serviceScopeFactory)
        {
            _daprClient = daprClient;
            _logger = logger;
            string scheduleStr = config.GetValue<string>("SaveSchedule");
            updateSchedule(scheduleStr);
        }

        public override Task ProcessInScope(IServiceProvider serviceProvider)
        {

            _logger.LogInformation("SCHEDULE  : Starting Save schedule ");
            IServiceState serviceState = serviceProvider.GetService<IServiceState>();

            //Console.WriteLine("ScheduleService : Ping Processing starts here");
            try
            {
                bool isDaprReady = _daprClient.CheckHealthAsync().Result;
                var daprMetadata = new Dictionary<string, string>();
                // ttl 6h mins 100s.
                daprMetadata.Add("ttlInSeconds", "21500");
                if (isDaprReady)
                {
                    _logger.LogInformation("Dapr Client Status is healthy");
                    if (serviceState.IsMonitorServiceReady)
                    {


                        _daprClient.PublishEventAsync("pubsub", "monitorSaveData", daprMetadata);
                        _logger.LogInformation("Sent monitorSaveData event.");
                        serviceState.IsMonitorServiceReady = false;
                    }
                    else
                    {
                        _daprClient.PublishEventAsync("pubsub", "serviceWakeUp", daprMetadata);
                        _logger.LogWarning("Processor has not signalled it is ready");
                    }

                }
                else
                {
                    _logger.LogCritical("Dapr Client Status is not healthy");
                }
            }
            catch (Exception e)
            {
                _logger.LogError("Error : occured in SaveScheduleTask.ProcesInScope() : Error Was : " + e.Message.ToString());

            }
            Console.WriteLine("ScheduleService : Saving data processing ends here");
            return Task.CompletedTask;
        }

    }
}
