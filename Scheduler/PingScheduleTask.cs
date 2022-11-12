
using Microsoft.Extensions.DependencyInjection;
using NetworkMonitor.Objects;
using NetworkMonitor.Objects.ServiceMessage;
using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Dapr.Client;

namespace NetworkMonitor.Scheduler
{
    public class PingScheduleTask : ScheduledProcessor
    {
        private bool firstRun;
        private ILogger _logger;
        private DaprClient _daprClient;

        public PingScheduleTask(DaprClient daprClient,ILogger<PingScheduleTask> logger,IServiceScopeFactory serviceScopeFactory, IConfiguration config) : base(serviceScopeFactory)
        {
            _daprClient=daprClient;
            firstRun = true;
            _logger=logger;
            string scheduleStr = config.GetValue<string>("PingSchedule");
            updateSchedule(scheduleStr);
        }
        public override Task ProcessInScope(IServiceProvider serviceProvider)
        {
             _logger.LogInformation("SCHEDULE : Starting Ping schedule ");
               
            //Console.WriteLine("ScheduleService : Ping Processing starts here");
            try
            {
                 bool isDaprReady = _daprClient.CheckHealthAsync().Result;
            if (isDaprReady)
            {
                _logger.LogInformation("Dapr Client Status is healthy");
                  ProcessorConnectObj connectObj = new ProcessorConnectObj();
                connectObj.NextRunInterval = RunScheduleInterval();
                _daprClient.PublishEventAsync<ProcessorConnectObj>("pubsub", "processorConnect", connectObj);
                _logger.LogDebug("Sent processorConnect event.");
            }
            else
            {
                _logger.LogCritical("Dapr Client Status is not healthy");
            }


            }
            catch (Exception e)
            {
                _logger.LogError("Error : occured in PingScheduleTask.ProcesInScope() : Error Was : " + e.Message+ " Inner Exceptoin : "+e.InnerException.Message);
            }
            //Console.WriteLine("ScheduleService : Ping Processing ends here");
            return Task.CompletedTask;
        }


    }
}
