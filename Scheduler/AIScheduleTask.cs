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

namespace NetworkMonitor.Scheduler
{
    public class AIScheduleTask : ScheduledProcessor
    {
        private ILogger _logger;
        public AIScheduleTask(ILogger<AIScheduleTask> logger, IServiceScopeFactory serviceScopeFactory, IConfiguration config) : base(serviceScopeFactory)
        {

            _logger = logger;
            string scheduleStr = config.GetValue<string>("AISchedule");
            updateSchedule(scheduleStr);
        }
        public override Task ProcessInScope(IServiceProvider serviceProvider)
        {
            _logger.LogInformation("SCHEDULE : Starting AI schedule ");
            IServiceState serviceState = serviceProvider.GetService<IServiceState>();
            //Console.WriteLine("ScheduleService : Payment Processing starts here");
            try
            {
                serviceState.RabbitRepo.Publish("processBlogList", null);
                _logger.LogInformation("Sent processBlogList event ");
            }
            catch (Exception e)
            {
                _logger.LogError("Error : occured in AIScheduleTask.ProcesInScope() : Error Was : " + e.Message.ToString());
            }
            //Console.WriteLine("ScheduleService : Ping Processing ends here");
            return Task.CompletedTask;
        }
    }
}