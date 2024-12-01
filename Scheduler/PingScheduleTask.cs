using Microsoft.Extensions.DependencyInjection;
using NetworkMonitor.Scheduler.Services;
using NetworkMonitor.Objects.ServiceMessage;
using NetworkMonitor.Objects;
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
            string scheduleStr = config.GetValue<string>("PingSchedule") ?? "* * * * *";
            updateSchedule(scheduleStr);
        }
        public override Task ProcessInScope(IServiceProvider serviceProvider)
        {
            string message = " SCHEDULE : Starting Ping schedule . ";
            bool success = true;
            int count=0;
            IServiceState serviceState = serviceProvider.GetService<IServiceState>()!;
            //Console.WriteLine("ScheduleService : Ping Processing starts here");
            try
            {
                ProcessorConnectObj connectObj = new ProcessorConnectObj();
                connectObj.NextRunInterval = RunScheduleInterval();
                
                foreach (ProcessorObj procInst in serviceState.EnabledProcessorInstances)
                {
                    if (procInst.IsReady && procInst.IsEnabled)
                    {
                        //message += " Success : Sent processorConnect event for appID " + procInst.AppID;
                        try
                        {
                            connectObj.AuthKey=procInst.AuthKey;
                            serviceState.RabbitRepo.PublishAsync<ProcessorConnectObj>("processorConnect" + procInst.AppID, connectObj);

                        }
                        catch (Exception e)
                        {
                            _logger.LogError($" Error could not publish event processorConnect {procInst.AppID}");
                            success = false;
                        }
                        count++;
                        procInst.IsReady = false;
                    }
                    else
                    {
                        try
                        {
                            serviceState.RabbitRepo.PublishAsync("processorWakeUp" + procInst.AppID, null);
                            message += " Warning : Processor " + procInst.AppID + " has not signalled it is ready . ";

                        }
                        catch (Exception e)
                        {
                            message += $" Error could not publish event processorWakeUp {procInst.AppID} . Error was : {e.Message}";
                            success = false;
                        }
                    }
                }
            }
            catch (Exception e)
            {
                message += " Error : Failed to run Ping schedule : Error Was : " + e.Message.ToString();
                success = false;
            }
            message+=$"Sent connect events to {count} agents";
            if (success) _logger.LogInformation(message);
            else _logger.LogError(message);
            //Console.WriteLine("ScheduleService : Ping Processing ends here");
            return Task.CompletedTask;
        }
    }
}
