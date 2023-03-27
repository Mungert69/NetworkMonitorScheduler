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
using System.Collections.Generic;
namespace NetworkMonitor.Scheduler
{
    public class PaymentScheduleTask : ScheduledProcessor
    {
        private bool firstRun;
        private ILogger _logger;
        public PaymentScheduleTask(INetLoggerFactory loggerFactory, IServiceScopeFactory serviceScopeFactory, IConfiguration config) : base(serviceScopeFactory)
        {
            firstRun = true;
            _logger = loggerFactory.GetLogger("PaymentScheduleTask");
            string scheduleStr = config.GetValue<string>("PaymentSchedule");
            updateSchedule(scheduleStr);
        }
        public override Task ProcessInScope(IServiceProvider serviceProvider)
        {
            _logger.Info("SCHEDULE : Starting Payment schedule ");
            IServiceState serviceState = serviceProvider.GetService<IServiceState>();
            //Console.WriteLine("ScheduleService : Payment Processing starts here");
            try
            {

                if (serviceState.IsPaymentServiceReady)
                {
                    serviceState.RabbitRepo.Publish("paymentCheck", null);
                    _logger.Info("Sent paymentCheck event ");
                    serviceState.IsPaymentServiceReady = false;
                }
                else
                {
                    serviceState.RabbitRepo.Publish("paymentWakeUp", null);
                    _logger.Warn("Payment Service has not signalled it is ready sent paymentWakeUp");
                }
            }
            catch (Exception e)
            {
                _logger.Error("Error : occured in PaymentScheduleTask.ProcesInScope() : Error Was : " + e.Message.ToString());
            }
            //Console.WriteLine("ScheduleService : Ping Processing ends here");
            return Task.CompletedTask;
        }
    }
}