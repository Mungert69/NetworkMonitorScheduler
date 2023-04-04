using System;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using NetworkMonitor.Scheduler.Services;
using NetworkMonitor.Objects.Factory;
using NetworkMonitor.BackgroundService;
using Microsoft.Extensions.Configuration;
using MetroLog;

namespace NetworkMonitor.Scheduler
{
    public class ResetScheduleTask : ScheduledProcessor
    {
        private ILogger _logger;
        public ResetScheduleTask(INetLoggerFactory loggerFactory, IServiceScopeFactory serviceScopeFactory, IConfiguration config) : base(serviceScopeFactory)
        {
            _logger = loggerFactory.GetLogger("ResetScheduleTask");
            string scheduleStr = config.GetValue<string>("ResetSchedule");
            updateSchedule(scheduleStr);
        }
        public override Task ProcessInScope(IServiceProvider serviceProvider)
        {
            _logger.Info("SCHEDULE : Starting Reset schedule ");
            IServiceState serviceState = serviceProvider.GetService<IServiceState>();
            try
            {
                serviceState.ResetReportSent();
                _logger.Info("Success :  Reset Schedule Ran ");
            }
            catch (Exception e)
            {
                _logger.Error("Error : occured in ResetScheduleTask.ProcesInScope() : Error Was : " + e.Message.ToString());
            }
            return Task.CompletedTask;
        }
    }
}