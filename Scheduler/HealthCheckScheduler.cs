
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
        private bool _noHealthCheck=false;

        public HealthCheckScheduleTask( ILogger<HealthCheckScheduleTask> logger, IServiceScopeFactory serviceScopeFactory, IConfiguration config) : base(serviceScopeFactory)
        {

             _logger = logger;
            _firstRun=true;
            string scheduleStr = config.GetValue<string>("PingSchedule") ?? "* * * * *";
            _noHealthCheck = config.GetValue<bool?>("NoHealthCheck") ?? false;
            updateSchedule(scheduleStr);
        }
        public override Task ProcessInScope(IServiceProvider serviceProvider)
        {
            if (_noHealthCheck){
                _logger.LogWarning("SCHEDULE : Warning Health Check is turn off!");
                return Task.CompletedTask;
            }
            string message=" SCHEDULE : Starting Health Check schedule . ";

            if (_firstRun) {
                _firstRun=false;
                message+=" Success : First Run skip . ";
                return Task.CompletedTask;}
            IServiceState serviceState = serviceProvider.GetService<IServiceState>()!;

            //Console.WriteLine("ScheduleService : Ping Processing starts here");
            try
            {

                var result = serviceState.CheckHealth();
                if (!result.Success)
                {

                    _logger.LogCritical(message+" Error : Schedule State failed Health Check Message was : " + result.Message);

                }
                else
                {
                    _logger.LogInformation(message+" Success :: --> All Services Healthy <-- ::");
                }



            }
            catch (Exception e)
            {
                _logger.LogError(message+" Error : Failed to Run Health Check schedule : Error Was : " + e.Message.ToString());
            }
            //Console.WriteLine("ScheduleService : Ping Processing ends here");
            return Task.CompletedTask;
        }


    }
}
