
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.Extensions.Configuration;
using MetroLog;
using Dapr.Client;
using NetworkMonitor.Scheduler.Services;
using NetworkMonitor.Objects.Factory;
using NetworkMonitor.BackgroundService;

namespace NetworkMonitor.Scheduler
{
    public class AlertScheduleTask : ScheduledProcessor
    {
        private bool firstRun;
        private ILogger _logger;
        private DaprClient _daprClient;

        public AlertScheduleTask(DaprClient daprClient,INetLoggerFactory loggerFactory, IServiceScopeFactory serviceScopeFactory, IConfiguration config) : base(serviceScopeFactory)
        {
            _daprClient = daprClient;
             _logger = loggerFactory.GetLogger("AlertScheduleTask");
            firstRun = true;
            string scheduleStr = config.GetValue<string>("AlertSchedule");
            updateSchedule(scheduleStr);
        }

        public override Task ProcessInScope(IServiceProvider serviceProvider)
        {
            _logger.Info("SCHEDULE  : Starting Alert schedule ");
            IServiceState serviceState = serviceProvider.GetService<IServiceState>();
            //Console.WriteLine("ScheduleService : Ping Processing starts here");
            try
            {
                bool isDaprReady = _daprClient.CheckHealthAsync().Result;
                var daprMetadata = new Dictionary<string, string>();
                daprMetadata.Add("ttlInSeconds", "60");
                if (isDaprReady)
                {
                    _logger.Info("Dapr Client Status is healthy");
                    if (serviceState.IsAlertServiceReady)
                    {


                        _daprClient.PublishEventAsync("pubsub", "monitorAlert", daprMetadata);
                        _logger.Info("Sent monitorAlert event.");
                        //serviceState.IsAlertServiceReady = false;
                    }
                    else
                    {
                        _daprClient.PublishEventAsync("pubsub", "serviceWakeUp", daprMetadata);

                        _logger.Warn("AlertService has not signalled it is ready");
                    }
                }
                else
                {
                    _logger.Fatal("Dapr Client Status is not healthy");
                }
            }
            catch (Exception e)
            {
                _logger.Error("Error : occured in AlertScheduleTask.ProcesInScope() . Error was : " + e.Message.ToString());
            }

            return Task.CompletedTask;
        }


    }
}
