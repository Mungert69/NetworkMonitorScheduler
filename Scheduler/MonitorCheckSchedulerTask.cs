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
    public class MonitorCheckScheduleTask: ScheduledProcessor
    {
        private bool firstRun;
        private ILogger _logger;
        private DaprClient _daprClient;

        public MonitorCheckScheduleTask(DaprClient daprClient, INetLoggerFactory loggerFactory, IServiceScopeFactory serviceScopeFactory, IConfiguration config) : base(serviceScopeFactory)
        {
            _daprClient = daprClient;
            firstRun = true;
             _logger = loggerFactory.GetLogger("MonitorCheckScheduleTask");
            string scheduleStr = config.GetValue<string>("MonitorCheckSchedule");
            updateSchedule(scheduleStr);
        }
        public override Task ProcessInScope(IServiceProvider serviceProvider)
        {
            _logger.Info("SCHEDULE : Starting MonitorCheck schedule ");
            IServiceState serviceState = serviceProvider.GetService<IServiceState>();

            //Console.WriteLine("ScheduleService : Payment Processing starts here");
            try
            {
                bool isDaprReady = _daprClient.CheckHealthAsync().Result;
                if (isDaprReady)
                {
                    //_logger.Info("Dapr Client Status is healthy");
                    var daprMetadata = new Dictionary<string, string>();
                    daprMetadata.Add("ttlInSeconds", "60");

         
                        if (serviceState.IsMonitorCheckServiceReady)
                        {

                            _daprClient.PublishEventAsync("pubsub", "monitorCheck" , daprMetadata);
                            _logger.Info("Sent monitorCheck event ");
                            //serviceState.IsPaymentServiceReady = false;

                        }
                        else
                        {
                            _daprClient.PublishEventAsync("pubsub", "monitorCheckWakeUp" , daprMetadata);
                            _logger.Warn("MonitorCheck Service has not signalled it is ready");
                        }
                    
                }
                else
                {
                    _logger.Fatal("Dapr Client Status is not healthy");
                }


            }
            catch (Exception e)
            {
                _logger.Error("Error : occured in MonitorCheckScheduleTask.ProcesInScope() : Error Was : " + e.Message.ToString());
            }
            //Console.WriteLine("ScheduleService : Ping Processing ends here");
            return Task.CompletedTask;
        }


    }
}