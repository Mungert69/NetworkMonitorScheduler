using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using CloudNative.CloudEvents;
using CloudNative.CloudEvents.NewtonsoftJson;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NetworkMonitor.Objects.ServiceMessage;
using NetworkMonitor.Objects;
using Microsoft.AspNetCore.Mvc;
using MetroLog;
using NetworkMonitor.Scheduler.Services;
using System;
using System.Threading.Tasks;
using NetworkMonitor.Objects.Factory;
using NetworkMonitor.Utils.Helpers;

namespace NetworkMonitor.Objects.Repository
{
      public interface IRabbitListener
    {
        ResultObj ProcessorReady(ProcessorInitObj processorObj);
        ResultObj PaymentServiceReady(PaymentServiceInitObj paymentObj);
        ResultObj AlertServiceReady(AlertServiceInitObj alertObj);
        ResultObj MonitorServiceReady(MonitorServiceInitObj monitorObj);
        ResultObj MonitorCheckServiceReady(MonitorServiceInitObj monitorObj);
    }
    public class RabbitListener : RabbitListenerBase, IRabbitListener
    {
        private IServiceState _serviceState;
        public RabbitListener( IServiceState serviceState, INetLoggerFactory loggerFactory, SystemParamsHelper systemParamsHelper) : base(DeriveLogger(loggerFactory), DeriveSystemUrl(systemParamsHelper))
       
        {
            _serviceState=serviceState;
	    Setup();
           }

            private static ILogger DeriveLogger(INetLoggerFactory loggerFactory)
        {
            return loggerFactory.GetLogger("RabbitListener"); 
        }

        private static SystemUrl DeriveSystemUrl(SystemParamsHelper systemParamsHelper)
        {
            return systemParamsHelper.GetSystemParams().ThisSystemUrl;
        }
        protected override void InitRabbitMQObjs()
        {
            _rabbitMQObjs.Add(new RabbitMQObj()
            {
                ExchangeName = "processorReady",
                FuncName = "processorReady",
                MessageTimeout = 60000
            });
            _rabbitMQObjs.Add(new RabbitMQObj()
            {
                ExchangeName = "paymentServiceReady",
                FuncName = "paymentServiceReady"
            });
            _rabbitMQObjs.Add(new RabbitMQObj()
            {
                ExchangeName = "alertServiceReady",
                FuncName = "alertServiceReady"
            });
            _rabbitMQObjs.Add(new RabbitMQObj()
            {
                ExchangeName = "monitorServiceReady",
                FuncName = "monitorServiceReady",
                MessageTimeout = 60000
            });
            _rabbitMQObjs.Add(new RabbitMQObj()
            {
                ExchangeName = "monitorCheckServiceReady",
                FuncName = "monitorCheckServiceReady"
            });
        }
        protected override ResultObj DeclareConsumers()
        {
            var result = new ResultObj();
            try
            {
                _rabbitMQObjs.ForEach(rabbitMQObj =>
            {
                rabbitMQObj.Consumer = new EventingBasicConsumer(rabbitMQObj.ConnectChannel);
                switch (rabbitMQObj.FuncName)
                {
                    case "processorReady":
                        rabbitMQObj.ConnectChannel.BasicQos(prefetchSize: 0, prefetchCount: 1, global: false);
                        rabbitMQObj.Consumer.Received += (model, ea) =>
                    {
                        try {
                               result = ProcessorReady(ConvertToObject<ProcessorInitObj>(model, ea));
                        rabbitMQObj.ConnectChannel.BasicAck(ea.DeliveryTag, false);
                        }
                         catch (Exception ex)
                        {
                            _logger.Error(" Error : RabbitListener.DeclareConsumers.processorReady " + ex.Message);
                        }
                     
                    };
                        break;
                    case "paymentServiceReady":
                        rabbitMQObj.ConnectChannel.BasicQos(prefetchSize: 0, prefetchCount: 1, global: false);
                        rabbitMQObj.Consumer.Received += (model, ea) =>
                    {
                        try {
                              result = PaymentServiceReady(ConvertToObject<PaymentServiceInitObj>(model, ea));
                        rabbitMQObj.ConnectChannel.BasicAck(ea.DeliveryTag, false);
                        }
                        catch (Exception ex)
                        {
                            _logger.Error(" Error : RabbitListener.DeclareConsumers.paymentServiceReady " + ex.Message);
                        }
                      
                    };
                        break;
                    case "alertServiceReady":
                        rabbitMQObj.ConnectChannel.BasicQos(prefetchSize: 0, prefetchCount: 1, global: false);
                        rabbitMQObj.Consumer.Received += (model, ea) =>
                    {
                        try {
                              result = AlertServiceReady(ConvertToObject<AlertServiceInitObj>(model, ea));
                        rabbitMQObj.ConnectChannel.BasicAck(ea.DeliveryTag, false);
                        }
                        catch (Exception ex)
                        {
                            _logger.Error(" Error : RabbitListener.DeclareConsumers.alertServiceReady " + ex.Message);
                        }
                      
                    };
                        break;
                    case "monitorServiceReady":
                        rabbitMQObj.ConnectChannel.BasicQos(prefetchSize: 0, prefetchCount: 1, global: false);
                        rabbitMQObj.Consumer.Received += (model, ea) =>
                    {
                        try {
                             result = MonitorServiceReady(ConvertToObject<MonitorServiceInitObj>(model, ea));
                        rabbitMQObj.ConnectChannel.BasicAck(ea.DeliveryTag, false);
                        }
                        catch (Exception ex)
                        {
                            _logger.Error(" Error : RabbitListener.DeclareConsumers.monitorServiceReady " + ex.Message);
                        }
                       
                    };
                        break;
                    case "monitorCheckServiceReady":
                        rabbitMQObj.ConnectChannel.BasicQos(prefetchSize: 0, prefetchCount: 1, global: false);
                        rabbitMQObj.Consumer.Received += (model, ea) =>
                    {
                        try {
                             result = MonitorCheckServiceReady(ConvertToObject<MonitorServiceInitObj>(model, ea));
                        rabbitMQObj.ConnectChannel.BasicAck(ea.DeliveryTag, false);
                        }
                        catch (Exception ex)
                        {
                            _logger.Error(" Error : RabbitListener.DeclareConsumers.monitorCheckServiceReady " + ex.Message);
                        }
                       
                    };
                        break;
                }
            });
                result.Success = true;
                result.Message += " Success : Declared all consumers ";
            }
            catch (Exception e)
            {
                string message = " Error : failed to declate consumers. Error was : " + e.ToString() + " . ";
                result.Message += message;
                Console.WriteLine(result.Message);
                result.Success = false;
            }
            return result;
        }
   
          public ResultObj ProcessorReady(ProcessorInitObj processorObj)
        {
            ResultObj result = new ResultObj();
            result.Success = false;
            result.Message = "MessageAPI : ProcessorrReady : ";

            try
            {
                var procInst=new ProcessorInstance();
                procInst.ID=processorObj.AppID;
                procInst.IsReady=processorObj.IsProcessorReady;
                var resultProcesoor=_serviceState.SetProcessorReady( procInst);
                result.Message+=resultProcesoor.Message;
                result.Success=resultProcesoor.Success;

                _logger.Info(result.Message);
            }
            catch (Exception e)
            {
                // At the moment this exception is redundent as expection is caught inside SetProcessorReady.
                result.Data = null;
                result.Success = false;
                result.Message += "Error : Failed to set Is ProcessorrReady : Error was : " + e.Message + " ";
                _logger.Error("Error : Failed to set Is ProcessorrReady : Error was : " + e.Message + " ");
            }
            return result;

        }
   public ResultObj PaymentServiceReady( PaymentServiceInitObj paymentObj)
        {
            ResultObj result = new ResultObj();
            result.Success = false;
            result.Message = "MessageAPI : PaymentServiceReady : ";

            try
            {
                _serviceState.IsPaymentServiceReady= paymentObj.IsPaymentServiceReady;
                result.Message += "Success set PaymentServiceReady to " + paymentObj.IsPaymentServiceReady;
                result.Success = true;
                _logger.Info(result.Message);
            }
            catch (Exception e)
            {
                result.Data = null;
                result.Success = false;
                result.Message += "Error : Failed to set PaymentServiceReady : Error was : " + e.Message + " ";
                _logger.Error("Error : Failed to set PaymentServiceReady : Error was : " + e.Message + " ");
            }
            return result;

        }


        public ResultObj AlertServiceReady([FromBody] AlertServiceInitObj alertObj)
        {
            ResultObj result = new ResultObj();
            result.Success = false;
            result.Message = "MessageAPI : AlertServiceReady : ";

            try
            {
                _serviceState.IsAlertServiceReady = alertObj.IsAlertServiceReady;
                result.Message += "Success set AlertServiceReady to " + alertObj.IsAlertServiceReady;
                result.Success = true;
                _logger.Info(result.Message);
            }
            catch (Exception e)
            {
                result.Data = null;
                result.Success = false;
                result.Message += "Error : Failed to set AlertServiceReady : Error was : " + e.Message + " ";
                _logger.Error("Error : Failed to set AlertServiceReady : Error was : " + e.Message + " ");
            }
            return result;

        }

          public ResultObj MonitorServiceReady([FromBody] MonitorServiceInitObj monitorObj)
        {
            ResultObj result = new ResultObj();
            result.Success = false;
            result.Message = "MessageAPI : MonitorServiceReady : ";

            try
            {
                _serviceState.IsMonitorServiceReady = monitorObj.IsServiceReady;
                result.Message += "Success set MonitorServiceReady to " + monitorObj.IsServiceReady;
                result.Success = true;
                _logger.Info(result.Message);
            }
            catch (Exception e)
            {
                result.Data = null;
                result.Success = false;
                result.Message += "Error : Failed to set MonitorServiceReady : Error was : " + e.Message + " ";
                _logger.Error("Error : Failed to set MonitorServiceReady : Error was : " + e.Message + " ");
            }
            return result;

        }

       public ResultObj MonitorCheckServiceReady([FromBody] MonitorServiceInitObj monitorObj)
        {
            ResultObj result = new ResultObj();
            result.Success = false;
            result.Message = "MessageAPI : MonitorCheckServiceReady : ";

            try
            {
                _serviceState.IsMonitorCheckServiceReady = monitorObj.IsMonitorCheckServiceReady;
                result.Message += "Success set MonitorCheckServiceReady to " + monitorObj.IsMonitorCheckServiceReady;
                result.Success = true;
                _logger.Info(result.Message);
            }
            catch (Exception e)
            {
                result.Data = null;
                result.Success = false;
                result.Message += "Error : Failed to set MonitorCheckServiceReady : Error was : " + e.Message + " ";
                _logger.Error("Error : Failed to set MonitorCheckServiceReady : Error was : " + e.Message + " ");
            }
            return result;

        }


   }
}
