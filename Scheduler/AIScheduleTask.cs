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

namespace NetworkMonitor.Scheduler
{
    public class AIScheduleTask : ScheduledProcessor
    {
        private bool firstRun;
        private ILogger _logger;
        public AIScheduleTask(INetLoggerFactory loggerFactory, IServiceScopeFactory serviceScopeFactory, IConfiguration config) : base(serviceScopeFactory)
        {
            firstRun = true;
            _logger = loggerFactory.GetLogger("AISchedulerTask");
            string scheduleStr = config.GetValue<string>("AIScheduler");
            updateSchedule(scheduleStr);
        }
        public override Task ProcessInScope(IServiceProvider serviceProvider)
        {
            _logger.Info("SCHEDULE : Starting AI schedule ");
            IServiceState serviceState = serviceProvider.GetService<IServiceState>();
            //Console.WriteLine("ScheduleService : Payment Processing starts here");
            try
            {
                serviceState.RabbitRepo.Publish("processBlogList", null);
                _logger.Info("Sent processBlogList event ");
            }
            catch (Exception e)
            {
                _logger.Error("Error : occured in AISchedulerTask.ProcesInScope() : Error Was : " + e.Message.ToString());
            }
            //Console.WriteLine("ScheduleService : Ping Processing ends here");
            return Task.CompletedTask;
        }
    }
}