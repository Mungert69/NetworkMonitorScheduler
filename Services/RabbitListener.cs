using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using NetworkMonitor.Objects.ServiceMessage;
using NetworkMonitor.Objects;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using NetworkMonitor.Scheduler.Services;
using NetworkMonitor.Objects.Repository;
using System;
using System.Threading.Tasks;
using NetworkMonitor.Objects.Factory;
using NetworkMonitor.Utils.Helpers;

namespace NetworkMonitor.Scheduler.Services
{
    public interface IRabbitListener
    {
        ResultObj ProcessorReady(ProcessorInitObj processorObj);
        ResultObj PaymentServiceReady(PaymentServiceInitObj paymentObj);
        ResultObj AlertServiceReady(AlertServiceInitObj alertObj);
        ResultObj MonitorServiceReady(MonitorServiceInitObj serviveObj);
        ResultObj MonitorDataReady(MonitorDataInitObj dataObj);
                Task Shutdown();
        Task<ResultObj> Setup();
    }
    public class RabbitListener : RabbitListenerBase, IRabbitListener
    {
        private IServiceState _serviceState;
        public RabbitListener(IServiceState serviceState, ILogger<RabbitListenerBase> logger, ISystemParamsHelper systemParamsHelper) : base(logger, DeriveSystemUrl(systemParamsHelper))

        {
            _serviceState = serviceState;
        }


        private static SystemUrl DeriveSystemUrl(ISystemParamsHelper systemParamsHelper)
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
                ExchangeName = "monitorDataReady",
                FuncName = "monitorDataReady",
                MessageTimeout = 60000
            });
            _rabbitMQObjs.Add(new RabbitMQObj()
            {
                ExchangeName = "predictServiceReady",
                FuncName = "predictServiceReady",
                MessageTimeout = 60000
            });

        }
        protected override async Task<ResultObj> DeclareConsumers()
        {
            var result = new ResultObj();
            try
            {
               foreach (var rabbitMQObj in _rabbitMQObjs)
            {
                rabbitMQObj.Consumer = new AsyncEventingBasicConsumer(rabbitMQObj.ConnectChannel);
                if (rabbitMQObj.ConnectChannel != null)
                {
                    switch (rabbitMQObj.FuncName)
                    {
                        case "processorReady":
                            await rabbitMQObj.ConnectChannel.BasicQosAsync(prefetchSize: 0, prefetchCount: 1, global: false);
                            rabbitMQObj.Consumer.ReceivedAsync +=  async(model, ea) =>
                        {
                            try
                            {
                                result = ProcessorReady(ConvertToObject<ProcessorInitObj>(model, ea));
                                await rabbitMQObj.ConnectChannel.BasicAckAsync(ea.DeliveryTag, false);
                            }
                            catch (Exception ex)
                            {
                                _logger.LogError(" Error : RabbitListener.DeclareConsumers.processorReady " + ex.Message);
                            }

                        };
                            break;
                        case "paymentServiceReady":
                            await rabbitMQObj.ConnectChannel.BasicQosAsync(prefetchSize: 0, prefetchCount: 1, global: false);
                            rabbitMQObj.Consumer.ReceivedAsync += async (model, ea) =>
                        {
                            try
                            {
                                result = PaymentServiceReady(ConvertToObject<PaymentServiceInitObj>(model, ea));
                                await rabbitMQObj.ConnectChannel.BasicAckAsync(ea.DeliveryTag, false);
                            }
                            catch (Exception ex)
                            {
                                _logger.LogError(" Error : RabbitListener.DeclareConsumers.paymentServiceReady " + ex.Message);
                            }

                        };
                            break;
                        case "alertServiceReady":
                            await rabbitMQObj.ConnectChannel.BasicQosAsync(prefetchSize: 0, prefetchCount: 1, global: false);
                            rabbitMQObj.Consumer.ReceivedAsync += async (model, ea) =>
                        {
                            try
                            {
                                result = AlertServiceReady(ConvertToObject<AlertServiceInitObj>(model, ea));
                                await rabbitMQObj.ConnectChannel.BasicAckAsync(ea.DeliveryTag, false);
                            }
                            catch (Exception ex)
                            {
                                _logger.LogError(" Error : RabbitListener.DeclareConsumers.alertServiceReady " + ex.Message);
                            }

                        };
                            break;
                        case "monitorServiceReady":
                            await rabbitMQObj.ConnectChannel.BasicQosAsync(prefetchSize: 0, prefetchCount: 1, global: false);
                            rabbitMQObj.Consumer.ReceivedAsync +=  async (model, ea) =>
                        {
                            try
                            {
                                result = MonitorServiceReady(ConvertToObject<MonitorServiceInitObj>(model, ea));
                                await rabbitMQObj.ConnectChannel.BasicAckAsync(ea.DeliveryTag, false);
                            }
                            catch (Exception ex)
                            {
                                _logger.LogError(" Error : RabbitListener.DeclareConsumers.monitorServiceReady " + ex.Message);
                            }

                        };
                            break;
                        case "monitorDataReady":
                            await rabbitMQObj.ConnectChannel.BasicQosAsync(prefetchSize: 0, prefetchCount: 1, global: false);
                            rabbitMQObj.Consumer.ReceivedAsync += async (model, ea) =>
                        {
                            try
                            {
                                result = MonitorDataReady(ConvertToObject<MonitorDataInitObj>(model, ea));
                                await rabbitMQObj.ConnectChannel.BasicAckAsync(ea.DeliveryTag, false);
                            }
                            catch (Exception ex)
                            {
                                _logger.LogError(" Error : RabbitListener.DeclareConsumers.monitorServiceReady " + ex.Message);
                            }

                        };
                            break;
                        case "predictServiceReady":
                            await rabbitMQObj.ConnectChannel.BasicQosAsync(prefetchSize: 0, prefetchCount: 1, global: false);
                            rabbitMQObj.Consumer.ReceivedAsync += async (model, ea) =>
                        {
                            try
                            {
                                result = PredictServiceReady(ConvertToObject<MonitorMLInitObj>(model, ea));
                                await rabbitMQObj.ConnectChannel.BasicAckAsync(ea.DeliveryTag, false);
                            }
                            catch (Exception ex)
                            {
                                _logger.LogError(" Error : RabbitListener.DeclareConsumers.predictServiceReady " + ex.Message);
                            }

                        };
                            break;

                    }
                }
            }
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

        public ResultObj ProcessorReady(ProcessorInitObj? processorObj)
        {
            ResultObj result = new ResultObj();
            result.Success = false;
            result.Message = "MessageAPI : ProcessorrReady : ";
            if (processorObj == null)
            {
                result.Success = false;
                result.Message += " Error : processorObj is null .";
                return result;
            }

            try
            {
                var procInst = new ProcessorObj();
                procInst.AppID = processorObj.AppID;
                procInst.IsReady = processorObj.IsProcessorReady;
                var resultProcesoor = _serviceState.SetProcessorReady(procInst);
                result.Message += resultProcesoor.Message;
                result.Success = resultProcesoor.Success;

                _logger.LogInformation(result.Message);
            }
            catch (Exception e)
            {
                // At the moment this exception is redundent as expection is caught inside SetProcessorReady.
                result.Data = null;
                result.Success = false;
                result.Message += "Error : Failed to set Is ProcessorrReady : Error was : " + e.Message + " ";
                _logger.LogError("Error : Failed to set Is ProcessorrReady : Error was : " + e.Message + " ");
            }
            return result;

        }
        public ResultObj PaymentServiceReady(PaymentServiceInitObj? paymentObj)
        {
            ResultObj result = new ResultObj();
            result.Success = false;
            result.Message = "MessageAPI : PaymentServiceReady : ";
            if (paymentObj == null)
            {
                result.Success = false;
                result.Message += " Error :paymentObj is null .";
                return result;
            }
            try
            {
                _serviceState.IsPaymentServiceReady = paymentObj.IsPaymentServiceReady;
                result.Message += "Success set PaymentServiceReady to " + paymentObj.IsPaymentServiceReady;
                result.Success = true;
                _logger.LogInformation(result.Message);
            }
            catch (Exception e)
            {
                result.Data = null;
                result.Success = false;
                result.Message += "Error : Failed to set PaymentServiceReady : Error was : " + e.Message + " ";
                _logger.LogError("Error : Failed to set PaymentServiceReady : Error was : " + e.Message + " ");
            }
            return result;

        }


        public ResultObj AlertServiceReady([FromBody] AlertServiceInitObj? alertObj)
        {
            ResultObj result = new ResultObj();
            result.Success = false;
            result.Message = "MessageAPI : AlertServiceReady : ";
            if (alertObj == null)
            {
                result.Success = false;
                result.Message += " Error : alertObj is null .";
                return result;
            }
            try
            {
                _serviceState.IsAlertServiceReady = alertObj.IsAlertServiceReady;
                result.Message += "Success set AlertServiceReady to " + alertObj.IsAlertServiceReady;
                result.Success = true;
                _logger.LogInformation(result.Message);
            }
            catch (Exception e)
            {
                result.Data = null;
                result.Success = false;
                result.Message += "Error : Failed to set AlertServiceReady : Error was : " + e.Message + " ";
                _logger.LogError("Error : Failed to set AlertServiceReady : Error was : " + e.Message + " ");
            }
            return result;

        }

        public ResultObj MonitorServiceReady([FromBody] MonitorServiceInitObj? serviceObj)
        {
            ResultObj result = new ResultObj();
            result.Success = false;
            result.Message = "MessageAPI : MonitorServiceReady : ";
            if (serviceObj == null)
            {
                result.Success = false;
                result.Message += " Error : serviceObj is null .";
                return result;
            }
            try
            {
                _serviceState.IsMonitorCheckServiceReady = serviceObj.IsServiceReady;
                result.Message += "Success set MonitorServiceReady to " + serviceObj.IsServiceReady;
                result.Success = true;
                _logger.LogInformation(result.Message);
            }
            catch (Exception e)
            {
                result.Data = null;
                result.Success = false;
                result.Message += "Error : Failed to set MonitorServiceReady : Error was : " + e.Message + " ";
                _logger.LogError("Error : Failed to set MonitorServiceReady : Error was : " + e.Message + " ");
            }
            return result;

        }


        public ResultObj MonitorDataReady([FromBody] MonitorDataInitObj? dataObj)
        {
            ResultObj result = new ResultObj();
            result.Success = false;
            result.Message = "MessageAPI : MonitorDataReady : ";
            if (dataObj == null)
            {
                result.Success = false;
                result.Message += " Error : dataObj is null .";
                return result;
            }
            try
            {
                string message = "";
                if (dataObj.IsDataMessage)
                {
                    _serviceState.IsMonitorCheckDataReady = dataObj.IsDataReady;
                    message += " Data Ready";
                }
                if (dataObj.IsDataSaveMessage)
                {
                    _serviceState.IsMonitorDataSaveReady = dataObj.IsDataSaveReady;
                    message += " Data Save Ready";
                }
                if (dataObj.IsDataPurgeMessage)
                {
                    _serviceState.IsMonitorDataPurgeReady = dataObj.IsDataPurgeReady;
                    message += " Data Purge Ready";
                }

                result.Message += "Success set monitorDataReady " + message;
                result.Success = true;
                _logger.LogInformation(result.Message);
            }
            catch (Exception e)
            {
                result.Data = null;
                result.Success = false;
                result.Message += "Error : Failed to set MonitorDataReady : Error was : " + e.Message + " ";
                _logger.LogError("Error : Failed to set MonitorDataReady : Error was : " + e.Message + " ");
            }
            return result;

        }

  public ResultObj PredictServiceReady([FromBody] MonitorMLInitObj? serviceObj)
        {
            ResultObj result = new ResultObj();
            result.Success = false;
            result.Message = "MessageAPI : PredictServiceReady : ";
            if (serviceObj == null)
            {
                result.Success = false;
                result.Message += " Error : serviceObj is null .";
                return result;
            }
            try
            {
                _serviceState.IsPredictServiceReady = serviceObj.IsMLReady;
                result.Message += "Success set PredictServiceReady to " + serviceObj.IsMLReady;
                result.Success = true;
                _logger.LogInformation(result.Message);
            }
            catch (Exception e)
            {
                result.Data = null;
                result.Success = false;
                result.Message += "Error : Failed to set PredictServiceReady : Error was : " + e.Message + " ";
                _logger.LogError("Error : Failed to set PredictServiceReady : Error was : " + e.Message + " ");
            }
            return result;

        }


    }
}
