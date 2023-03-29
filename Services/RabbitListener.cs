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
using System.Collections.Generic;
using NetworkMonitor.Utils;
using System.Text;

namespace NetworkMonitor.Objects.Repository
{
    public class RabbitListener : RabbitListenerBase
    {
        private IServiceState _serviceState;
        public RabbitListener(ILogger logger,SystemUrl systemUrl, IServiceState serviceState) : base(logger,systemUrl)
       
        {
            _serviceState=serviceState;
	    Setup();
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
                        result = ProcessorReady(ConvertToObject<ProcessorInitObj>(model, ea));
                        rabbitMQObj.ConnectChannel.BasicAck(ea.DeliveryTag, false);
                    };
                        break;
                    case "paymentServiceReady":
                        rabbitMQObj.ConnectChannel.BasicQos(prefetchSize: 0, prefetchCount: 1, global: false);
                        rabbitMQObj.Consumer.Received += (model, ea) =>
                    {
                        result = PaymentServiceReady(ConvertToObject<PaymentServiceInitObj>(model, ea));
                        rabbitMQObj.ConnectChannel.BasicAck(ea.DeliveryTag, false);
                    };
                        break;
                    case "alertServiceReady":
                        rabbitMQObj.ConnectChannel.BasicQos(prefetchSize: 0, prefetchCount: 1, global: false);
                        rabbitMQObj.Consumer.Received += (model, ea) =>
                    {
                        result = AlertServiceReady(ConvertToObject<AlertServiceInitObj>(model, ea));
                        rabbitMQObj.ConnectChannel.BasicAck(ea.DeliveryTag, false);
                    };
                        break;
                    case "monitorServiceReady":
                        rabbitMQObj.ConnectChannel.BasicQos(prefetchSize: 0, prefetchCount: 1, global: false);
                        rabbitMQObj.Consumer.Received += (model, ea) =>
                    {
                        result = MonitorServiceReady(ConvertToObject<MonitorServiceInitObj>(model, ea));
                        rabbitMQObj.ConnectChannel.BasicAck(ea.DeliveryTag, false);
                    };
                        break;
                    case "monitorCheckServiceReady":
                        rabbitMQObj.ConnectChannel.BasicQos(prefetchSize: 0, prefetchCount: 1, global: false);
                        rabbitMQObj.Consumer.Received += (model, ea) =>
                    {
                        result = MonitorCheckServiceReady(ConvertToObject<MonitorServiceInitObj>(model, ea));
                        rabbitMQObj.ConnectChannel.BasicAck(ea.DeliveryTag, false);
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
                result=_serviceState.SetProcessorReady( procInst);
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
                _serviceState.IsAlertServiceReady = paymentObj.IsPaymentServiceReady;
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
