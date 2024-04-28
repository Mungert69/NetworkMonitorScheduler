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
            string scheduleStr = config.GetValue<string>("AISchedule") ?? "0 6 * * *";
            updateSchedule(scheduleStr);
        }
        public override Task ProcessInScope(IServiceProvider serviceProvider)
        {
            string message=" SCHEDULE : Starting AI schedule . ";
            IServiceState serviceState = serviceProvider.GetService<IServiceState>()!;
            //Console.WriteLine("ScheduleService : Payment Processing starts here");
            try
            {
                serviceState.RabbitRepo.PublishAsync("processBlogList", null);
                message+=" Success : Sent processBlogList event ";
                 serviceState.RabbitRepo.PublishAsync("fillUserTokens", null);
                 message+=" Success : Sent fillUserTokens event ";
                _logger.LogInformation(message);
            }
            catch (Exception e)
            {
                message+="Error : occured in AIScheduleTask.ProcesInScope() : Error Was : " + e.Message.ToString();
                _logger.LogError(message);
            }
            //Console.WriteLine("ScheduleService : Ping Processing ends here");
            return Task.CompletedTask;
        }
    }
}