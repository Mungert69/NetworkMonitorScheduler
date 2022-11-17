
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Dapr.Client;

namespace NetworkMonitor.Scheduler
{
    public class AlertScheduleTask : ScheduledProcessor
    {
        private bool firstRun;
        private ILogger _logger;
        private DaprClient _daprClient;

        public AlertScheduleTask(DaprClient daprClient,ILogger<AlertScheduleTask> logger, IServiceScopeFactory serviceScopeFactory, IConfiguration config) : base(serviceScopeFactory)
        {
            _daprClient=daprClient;
            _logger = logger;
            firstRun = true;
            string scheduleStr = config.GetValue<string>("AlertSchedule");
            updateSchedule(scheduleStr);
        }

        public override Task ProcessInScope(IServiceProvider serviceProvider)
        {
              _logger.LogInformation("SCHEDULE  : Starting Alert schedule ");
               
            //Console.WriteLine("ScheduleService : Ping Processing starts here");
            try
            {
                 bool isDaprReady = _daprClient.CheckHealthAsync().Result;
            if (isDaprReady)
            {
                _logger.LogInformation("Dapr Client Status is healthy");
                 _daprClient.PublishEventAsync("pubsub", "monitorAlert");
                _logger.LogInformation("Sent monitorAlert event.");
            }
            else
            {
                _logger.LogCritical("Dapr Client Status is not healthy");
            }
 }
            catch (Exception e)
            {
                _logger.LogError("Error : occured in AlertScheduleTask.ProcesInScope() : " + e.Message + " Inner Exception : " + e.InnerException.Message);
            }

            return Task.CompletedTask;
        }


    }
}
