
using Microsoft.Extensions.DependencyInjection;
using NetworkMonitor.Scheduler.Services;
using NetworkMonitor.Objects.ServiceMessage;
using NetworkMonitor.Objects.Factory;
using NetworkMonitor.BackgroundService;
using System;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;

namespace NetworkMonitor.Scheduler
{
    public class HealthCheckScheduleTask : ScheduledProcessor
    {
        private bool _firstRun;
        private ILogger _logger;

        public HealthCheckScheduleTask( ILogger<HealthCheckScheduleTask> logger, IServiceScopeFactory serviceScopeFactory, IConfiguration config) : base(serviceScopeFactory)
        {

             _logger = logger;
            _firstRun=true;
            string scheduleStr = config.GetValue<string>("PingSchedule");
            updateSchedule(scheduleStr);
        }
        public override Task ProcessInScope(IServiceProvider serviceProvider)
        {
            _logger.LogInformation("SCHEDULE : Starting Health Check schedule ");
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

                    _logger.LogCritical("Error : Schedule State failed Health Check Message was : " + result.Message);
                    var resultSend = serviceState.SendHealthReport(result.Message);
                    if (resultSend.Success)
                    {
                        _logger.LogInformation(resultSend.Message);
                    }
                    else
                    {
                        _logger.LogError("Error : Sending Health Report. Error was : " + resultSend.Message);
                    }

                }
                else
                {
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
