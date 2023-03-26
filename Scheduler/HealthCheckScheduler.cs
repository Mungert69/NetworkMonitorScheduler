
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
    public class HealthCheckScheduleTask : ScheduledProcessor
    {
        private bool _firstRun;
        private ILogger _logger;
        private DaprClient _daprClient;

        public HealthCheckScheduleTask(DaprClient daprClient, INetLoggerFactory loggerFactory, IServiceScopeFactory serviceScopeFactory, IConfiguration config) : base(serviceScopeFactory)
        {
            _daprClient = daprClient;
             _logger = loggerFactory.GetLogger("HealthCheckScheduleTask");
            _firstRun=true;
            string scheduleStr = config.GetValue<string>("PingSchedule");
            updateSchedule(scheduleStr);
        }
        public override Task ProcessInScope(IServiceProvider serviceProvider)
        {
            _logger.Info("SCHEDULE : Starting Health Check schedule ");
            if (_firstRun) {
                _firstRun=false;
                return Task.CompletedTask;}
            IServiceState serviceState = serviceProvider.GetService<IServiceState>();

            //Console.WriteLine("ScheduleService : Ping Processing starts here");
            try
            {

                var result = serviceState.CheckHealth();
                if (!result.Success)
                {

                    _logger.Fatal("Error : Schedule State failed Health Check Message was : " + result.Message);
                    var resultSend = serviceState.SendHealthReport(result.Message);
                    if (resultSend.Success)
                    {
                        _logger.Info(resultSend.Message);
                    }
                    else
                    {
                        _logger.Error("Error : Sending Health Report. Error was : " + resultSend.Message);
                    }

                }
                else
                {
                    _logger.Info("Success :: --> All Services Healthy <-- ::");
                }



            }
            catch (Exception e)
            {
                _logger.Error("Error : occured in HealthCheckScheduleTask.ProcesInScope() : Error Was : " + e.Message.ToString());
            }
            //Console.WriteLine("ScheduleService : Ping Processing ends here");
            return Task.CompletedTask;
        }


    }
}
