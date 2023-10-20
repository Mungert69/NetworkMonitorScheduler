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
    public class PingScheduleTask : ScheduledProcessor
    {
        private bool firstRun;
        private ILogger _logger;
        public PingScheduleTask(ILogger<PingScheduleTask> logger, IServiceScopeFactory serviceScopeFactory, IConfiguration config) : base(serviceScopeFactory)
        {
            firstRun = true;
            _logger = logger;
            string scheduleStr = config.GetValue<string>("PingSchedule");
            updateSchedule(scheduleStr);
        }
        public override Task ProcessInScope(IServiceProvider serviceProvider)
        {
            _logger.LogInformation("SCHEDULE : Starting Ping schedule ");
            IServiceState serviceState = serviceProvider.GetService<IServiceState>();
            //Console.WriteLine("ScheduleService : Ping Processing starts here");
            try
            {
                 ProcessorConnectObj connectObj = new ProcessorConnectObj();
                connectObj.NextRunInterval = RunScheduleInterval();
                foreach (ProcessorInstance procInst in serviceState.ProcessorInstances)
                {
                    if (procInst.IsReady)
                    {
                        serviceState.RabbitRepo.Publish<ProcessorConnectObj>("processorConnect" + procInst.ID, connectObj);
                        _logger.LogInformation("Sent processorConnect event for appID " + procInst.ID);
                        procInst.IsReady = false;
                    }
                    else
                    {
                        serviceState.RabbitRepo.Publish("processorWakeUp" + procInst.ID,null);
                        _logger.LogWarning("Processor " + procInst.ID + " has not signalled it is ready");
                    }
                }
            }
            catch (Exception e)
            {
                _logger.LogError("Error : occured in PingScheduleTask.ProcesInScope() : Error Was : " + e.Message.ToString());
            }
            //Console.WriteLine("ScheduleService : Ping Processing ends here");
            return Task.CompletedTask;
        }
    }
}
