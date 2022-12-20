
using Microsoft.Extensions.DependencyInjection;
using NetworkMonitor.Scheduler.Services;
using NetworkMonitor.Objects.ServiceMessage;
using System;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using Dapr.Client;

namespace NetworkMonitor.Scheduler
{
    public class PingScheduleTask : ScheduledProcessor
    {
        private bool firstRun;
        private ILogger _logger;
        private DaprClient _daprClient;

        public PingScheduleTask(DaprClient daprClient, ILogger<PingScheduleTask> logger, IServiceScopeFactory serviceScopeFactory, IConfiguration config) : base(serviceScopeFactory)
        {
            _daprClient = daprClient;
            firstRun = true;
            _logger = logger;
            string scheduleStr = config.GetValue<string>("PingSchedule");
            updateSchedule(scheduleStr);
        }
        public override Task ProcessInScope(IServiceProvider serviceProvider)
        {
            _logger.LogInformation("SCHEDULE : Starting Ping schedule ");
            IServiceState serviceState = serviceProvider.GetService<IServiceState>();

            //Console.WriteLine("ScheduleService : Ping Processing starts here");
            try
            {
                bool isDaprReady = _daprClient.CheckHealthAsync().Result;
                if (isDaprReady)
                {
                    _logger.LogInformation("Dapr Client Status is healthy");
                    ProcessorConnectObj connectObj = new ProcessorConnectObj();
                    connectObj.NextRunInterval = RunScheduleInterval();
                    foreach (ProcessorInstance procInst in serviceState.ProcessorInstances)
                    {

                        if (procInst.IsReady)
                        {
                            var daprMetadata = new Dictionary<string, string>();
                            daprMetadata.Add("ttlInSeconds", "60");

                            _daprClient.PublishEventAsync<ProcessorConnectObj>("pubsub", "processorConnect" + procInst.ID, connectObj,daprMetadata);
                            _logger.LogInformation("Sent processorConnect event for appID " + procInst.ID);
                            procInst.IsReady = false;

                        }
                        else
                        {
                            _logger.LogWarning("Processor " + procInst.ID + " has not signalled it is ready");
                        }
                    }
                }
                else
                {
                    _logger.LogCritical("Dapr Client Status is not healthy");
                }


            }
            catch (Exception e)
            {
                _logger.LogError("Error : occured in PingScheduleTask.ProcesInScope() : Error Was : "  + e.Message.ToString());
            }
            //Console.WriteLine("ScheduleService : Ping Processing ends here");
            return Task.CompletedTask;
        }


    }
}
