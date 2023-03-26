
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
using Dapr.Client;

namespace NetworkMonitor.Scheduler
{
    public class PingScheduleTask : ScheduledProcessor
    {
        private bool firstRun;
        private ILogger _logger;
        private DaprClient _daprClient;

        public PingScheduleTask(DaprClient daprClient, INetLoggerFactory loggerFactory, IServiceScopeFactory serviceScopeFactory, IConfiguration config) : base(serviceScopeFactory)
        {
            _daprClient = daprClient;
            firstRun = true;
             _logger = loggerFactory.GetLogger("PingScheduleTask");
            string scheduleStr = config.GetValue<string>("PingSchedule");
            updateSchedule(scheduleStr);
        }
        public override Task ProcessInScope(IServiceProvider serviceProvider)
        {
            _logger.Info("SCHEDULE : Starting Ping schedule ");
            IServiceState serviceState = serviceProvider.GetService<IServiceState>();

            //Console.WriteLine("ScheduleService : Ping Processing starts here");
            try
            {
                bool isDaprReady = _daprClient.CheckHealthAsync().Result;
                if (isDaprReady)
                {
                    //_logger.Info("Dapr Client Status is healthy");
                    ProcessorConnectObj connectObj = new ProcessorConnectObj();
                    connectObj.NextRunInterval = RunScheduleInterval();
                    var daprMetadata = new Dictionary<string, string>();
                    daprMetadata.Add("ttlInSeconds", "60");

                    foreach (ProcessorInstance procInst in serviceState.ProcessorInstances)
                    {
                        if (procInst.IsReady)
                        {

                            _daprClient.PublishEventAsync<ProcessorConnectObj>("pubsub", "processorConnect" + procInst.ID, connectObj, daprMetadata);
                            _logger.Info("Sent processorConnect event for appID " + procInst.ID);
                            procInst.IsReady = false;

                        }
                        else
                        {
                            _daprClient.PublishEventAsync("pubsub", "processorWakeUp" + procInst.ID, daprMetadata);
                            _logger.Warn("Processor " + procInst.ID + " has not signalled it is ready");
                        }
                    }
                }
                else
                {
                    _logger.Fatal("Dapr Client Status is not healthy");
                }


            }
            catch (Exception e)
            {
                _logger.Error("Error : occured in PingScheduleTask.ProcesInScope() : Error Was : " + e.Message.ToString());
            }
            //Console.WriteLine("ScheduleService : Ping Processing ends here");
            return Task.CompletedTask;
        }


    }
}
