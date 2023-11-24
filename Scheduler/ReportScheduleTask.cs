using Microsoft.Extensions.DependencyInjection;
using NetworkMonitor.Objects.Factory;
using NetworkMonitor.Objects.ServiceMessage;
using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using NetworkMonitor.Scheduler.Services;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using NetworkMonitor.BackgroundService;
namespace NetworkMonitor.Scheduler
{
    public class ReportScheduleTask : ScheduledProcessor
    {
        private ILogger _logger;
        public ReportScheduleTask(ILogger<ReportScheduleTask> logger, IServiceScopeFactory serviceScopeFactory, IConfiguration config) : base(serviceScopeFactory)
        {
            _logger = logger;
            string scheduleStr = config.GetValue<string>("ReportScheduleTask") ?? "0 13 * * 1";
            updateSchedule(scheduleStr);
        }
        public override Task ProcessInScope(IServiceProvider serviceProvider)
        {
            string message = "SCHEDULE  : Starting Report Schedule . ";
            IServiceState serviceState = serviceProvider.GetService<IServiceState>()!;
            try
            {
                serviceState!.RabbitRepo.Publish("createHostSummaryReport", null);
                message += " Success : Sent report event. ";
                _logger.LogInformation(message);
            }
            catch (Exception e)
            {
                message += " Error : Failed to run Report Schedule : Error Was : " + e.Message.ToString();
                _logger.LogError(message);
            }

            return Task.CompletedTask;
        }
    }
}
