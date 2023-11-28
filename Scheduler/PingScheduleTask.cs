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
            string scheduleStr = config.GetValue<string>("PingSchedule") ??  "* * * * *";
            updateSchedule(scheduleStr);
        }
        public override Task ProcessInScope(IServiceProvider serviceProvider)
        {
            string message=" SCHEDULE : Starting Ping schedule . ";
            IServiceState serviceState = serviceProvider.GetService<IServiceState>()!;
            //Console.WriteLine("ScheduleService : Ping Processing starts here");
            try
            {
                 ProcessorConnectObj connectObj = new ProcessorConnectObj();
                connectObj.NextRunInterval = RunScheduleInterval();
                foreach (ProcessorInstance procInst in serviceState.ProcessorInstances)
                {
                    if (procInst.IsReady)
                    {
                        message+=" Success : Sent processorConnect event for appID " + procInst.ID;
                        try {
                              serviceState.RabbitRepo.Publish<ProcessorConnectObj>("processorConnect" + procInst.ID, connectObj);
                        _logger.LogInformation(message);
                        }
                        catch (Exception e){
                            _logger.LogError($" Error could not publish event processorConnect {procInst.ID}");

                        }
                      
                        procInst.IsReady = false;
                    }
                    else
                    {
                         try {
                               serviceState.RabbitRepo.Publish("processorWakeUp" + procInst.ID,null);
                           message+=" Warning : Processor " + procInst.ID + " has not signalled it is ready . ";
                        _logger.LogWarning(message);
                 
                          }
                        catch (Exception e){
                            _logger.LogError($" Error could not publish event processorWakeUp {procInst.ID}");

                        }
                           }
                }
            }
            catch (Exception e)
            {
                message+=" Error : Failed to run Ping schedule : Error Was : " + e.Message.ToString();
                _logger.LogError(message);
            }
            //Console.WriteLine("ScheduleService : Ping Processing ends here");
            return Task.CompletedTask;
        }
    }
}
