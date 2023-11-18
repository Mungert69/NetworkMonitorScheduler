using System;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using NetworkMonitor.Scheduler.Services;
using NetworkMonitor.Objects.Factory;
using NetworkMonitor.BackgroundService;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace NetworkMonitor.Scheduler
{
    public class ResetScheduleTask : ScheduledProcessor
    {
        private ILogger _logger;
        public ResetScheduleTask(ILogger<ResetScheduleTask> logger, IServiceScopeFactory serviceScopeFactory, IConfiguration config) : base(serviceScopeFactory)
        {
            _logger = logger;
            string scheduleStr = config.GetValue<string>("ResetSchedule") ?? "0 0 * * *";
            updateSchedule(scheduleStr);
        }
        public override Task ProcessInScope(IServiceProvider serviceProvider)
        {
            string message=" SCHEDULE : Starting Reset schedule . ";
            IServiceState serviceState = serviceProvider.GetService<IServiceState>()!;
            try
            {
                serviceState!.ResetReportSent();
                message+=" Success :  Reset Schedule Ran . ";
                _logger.LogInformation(message);
            }
            catch (Exception e)
            {
                message+=" Error : Failed to run Reset schedule : Error Was : " + e.Message.ToString();
                _logger.LogError(message);
            }
            return Task.CompletedTask;
        }
    }
}