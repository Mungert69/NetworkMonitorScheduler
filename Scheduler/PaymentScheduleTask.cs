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
    public class PaymentScheduleTask : ScheduledProcessor
    {
        private bool firstRun;
        private ILogger _logger;
        public PaymentScheduleTask(ILogger<PaymentScheduleTask> logger, IServiceScopeFactory serviceScopeFactory, IConfiguration config) : base(serviceScopeFactory)
        {
            firstRun = true;
            _logger = logger;
            string scheduleStr = config.GetValue<string>("PaymentSchedule");
            updateSchedule(scheduleStr);
        }
        public override Task ProcessInScope(IServiceProvider serviceProvider)
        {
            _logger.LogInformation("SCHEDULE : Starting Payment schedule ");
            IServiceState serviceState = serviceProvider.GetService<IServiceState>();
            //Console.WriteLine("ScheduleService : Payment Processing starts here");
            try
            {

                if (serviceState.IsPaymentServiceReady)
                {
                    serviceState.RabbitRepo.Publish("paymentCheck", null);
                    _logger.LogInformation("Sent paymentCheck event ");
                    serviceState.IsPaymentServiceReady = false;
                }
                else
                {
                    serviceState.RabbitRepo.Publish("paymentWakeUp", null);
                    _logger.LogWarning("Payment Service has not signalled it is ready sent paymentWakeUp");
                }
            }
            catch (Exception e)
            {
                _logger.LogError("Error : occured in PaymentScheduleTask.ProcesInScope() : Error Was : " + e.Message.ToString());
            }
            //Console.WriteLine("ScheduleService : Ping Processing ends here");
            return Task.CompletedTask;
        }
    }
}