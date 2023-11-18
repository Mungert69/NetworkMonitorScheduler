using Microsoft.Extensions.DependencyInjection;
using NetworkMonitor.Objects.Factory;
using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using NetworkMonitor.Scheduler.Services;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using NetworkMonitor.BackgroundService;
using NetworkMonitor.Objects.ServiceMessage;
namespace NetworkMonitor.Scheduler
{
    public class DataSaveScheduleTask : ScheduledProcessor
    {
        private ILogger _logger;
        public DataSaveScheduleTask( ILogger<DataSaveScheduleTask> logger, IServiceScopeFactory serviceScopeFactory, IConfiguration config) : base(serviceScopeFactory)
        {
              _logger = logger;
            string scheduleStr = config.GetValue<string>("DataSaveSchedule") ?? "0 */6 * * *" ;
            updateSchedule(scheduleStr);
        }
        public override Task ProcessInScope(IServiceProvider serviceProvider)
        {
            string message=" SCHEDULE  : Starting Save schedule  . ";
            IServiceState serviceState = serviceProvider.GetService<IServiceState>()!;
            //Console.WriteLine("ScheduleService : Ping Processing starts here");
            try
            {
                    if (serviceState.IsMonitorDataSaveReady)
                    {
                        serviceState.RabbitRepo.Publish( "saveData", null);
                        message+=" Success : Sent saveData event.";
                        _logger.LogInformation(message);
                        serviceState.IsMonitorDataSaveReady = false;
                    }
                    else
                    {
                        var serviceObj=new MonitorDataInitObj(){
                            IsDataSaveReady=true,
                            IsDataSaveMessage=true
                        };
                        serviceState.RabbitRepo.Publish<MonitorDataInitObj>("dataCheck", serviceObj);
                        message+=" Warning : DataSave has not signalled it is ready. Sent dataCheck event .";
                        _logger.LogWarning(message);
                    }
            }
            catch (Exception e)
            {
                message+=" Error : occured in SaveScheduleTask.ProcesInScope() : Error Was : " + e.Message.ToString();
                _logger.LogError(message);
            }
            return Task.CompletedTask;
        }
    }
}
