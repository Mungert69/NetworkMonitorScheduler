
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
    public class HealthCheckScheduleTask : ScheduledProcessor
    {
        private bool firstRun;
        private ILogger _logger;
        private DaprClient _daprClient;

        public HealthCheckScheduleTask(DaprClient daprClient, ILogger<HealthCheckScheduleTask> logger, IServiceScopeFactory serviceScopeFactory, IConfiguration config) : base(serviceScopeFactory)
        {
            _daprClient = daprClient;
            _logger = logger;
            string scheduleStr = config.GetValue<string>("PingSchedule");
            updateSchedule(scheduleStr);
        }
        public override Task ProcessInScope(IServiceProvider serviceProvider)
        {
            _logger.LogInformation("SCHEDULE : Starting Health Check schedule ");
            IServiceState serviceState = serviceProvider.GetService<IServiceState>();

            //Console.WriteLine("ScheduleService : Ping Processing starts here");
            try
            {
              
                var result=serviceState.CheckHealth();
                if (!result.Success )
                {
                   
                      _logger.LogCritical("Error : Schedule State failed Health Check Message was : "+result.Message);
                   
                    
                }
                else{
                    _logger.LogInformation("Success :: --> All Services Healthy <-- ::");
                }
               


            }
            catch (Exception e)
            {
                _logger.LogError("Error : occured in HealthCheckScheduleTask.ProcesInScope() : Error Was : " + e.Message.ToString());
            }
            //Console.WriteLine("ScheduleService : Ping Processing ends here");
            return Task.CompletedTask;
        }


    }
}
