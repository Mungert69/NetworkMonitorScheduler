using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using NetworkMonitor.Objects.ServiceMessage;
using NetworkMonitor.Objects;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using NetworkMonitor.Scheduler.Services;
using NetworkMonitor.Objects.Repository;
using System;
using System.Threading;
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
        Task<ResultObj> Setup(CancellationToken cancellationToken);
    }
    public class RabbitListener : RabbitListenerBase, IRabbitListener
    {
        private IServiceState _serviceState;
        private readonly IBackendMessageHmacService _backendHmac;
        public RabbitListener(IServiceState serviceState, ILogger<RabbitListenerBase> logger, SystemParams systemParams, IBackendMessageHmacService backendHmac) : base(logger, DeriveSystemUrl(systemParams))

        {
            _serviceState = serviceState;
            _backendHmac = backendHmac;
        }


        private static SystemUrl DeriveSystemUrl(SystemParams systemParams)
        {
            return systemParams.ThisSystemUrl;
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
                await Parallel.ForEachAsync(_rabbitMQObjs, async (rabbitMQObj, cancellationToken) =>
                 {

                     if (rabbitMQObj.ConnectChannel != null)
                     {

                         rabbitMQObj.Consumer = new AsyncEventingBasicConsumer(rabbitMQObj.ConnectChannel);
                         await rabbitMQObj.ConnectChannel.BasicConsumeAsync(
                                 queue: rabbitMQObj.QueueName,
                                 autoAck: false,
                                 consumer: rabbitMQObj.Consumer
                             );


                         switch (rabbitMQObj.FuncName)
                         {
                             case "processorReady":
                                 await RegisterConsumerHandlerAsync(rabbitMQObj, 1, "processorReady", (model, ea) =>
                                 {
                                     result = ProcessorReady(ConvertToObject<ProcessorInitObj>(model, ea));
                                     return Task.CompletedTask;
                                 });
                                 break;
                             case "paymentServiceReady":
                                 await RegisterConsumerHandlerAsync(rabbitMQObj, 1, "paymentServiceReady", async (model, ea) =>
                                 {
                                     result = await PaymentServiceReadyAsync(ConvertToObject<PaymentServiceInitObj>(model, ea));
                                 });
                                 break;
                             case "alertServiceReady":
                                 await RegisterConsumerHandlerAsync(rabbitMQObj, 1, "alertServiceReady", async (model, ea) =>
                                 {
                                     result = await AlertServiceReadyAsync(ConvertToObject<AlertServiceInitObj>(model, ea));
                                 });
                                 break;
                             case "monitorServiceReady":
                                 await RegisterConsumerHandlerAsync(rabbitMQObj, 1, "monitorServiceReady", async (model, ea) =>
                                 {
                                     result = await MonitorServiceReadyAsync(ConvertToObject<MonitorServiceInitObj>(model, ea));
                                 });
                                 break;
                             case "monitorDataReady":
                                 await RegisterConsumerHandlerAsync(rabbitMQObj, 1, "monitorDataReady", async (model, ea) =>
                                 {
                                     result = await MonitorDataReadyAsync(ConvertToObject<MonitorDataInitObj>(model, ea));
                                 });
                                 break;
                             case "predictServiceReady":
                                 await RegisterConsumerHandlerAsync(rabbitMQObj, 1, "predictServiceReady", async (model, ea) =>
                                 {
                                     result = await PredictServiceReady(ConvertToObject<MonitorMLInitObj>(model, ea));
                                 });
                                 break;

                         }
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

        public ResultObj ProcessorReady(ProcessorInitObj? processorObj)
        {
            ResultObj result = new ResultObj();
            result.Success = false;
            result.Message = "MessageAPI : ProcessorReady : ";
            if (processorObj == null)
            {
                result.Success = false;
                result.Message += " Error : processorObj is null .";
                _logger.LogWarning(result.Message);
                return result;
            }
            bool enforcePublisherIdentity = _systemUrl.RequirePublisherUserId;
            if (string.IsNullOrWhiteSpace(processorObj.AppID))
            {
                result.Success = false;
                result.Message += " Error : AppID is missing.";
                _logger.LogWarning(result.Message);
                return result;
            }
            var isSystemProcessor = _serviceState.IsSystemProcessor(processorObj.AppID);
            if (enforcePublisherIdentity && !isSystemProcessor && !ValidatePublisherIdentityForApp(
                result,
                processorObj.AppID,
                "ProcessorReady",
                allowDefaultPublisher: true))
            {
                _logger.LogWarning(result.Message);
                return result;
            }

            try
            {
                var procInst = new ProcessorObj();
                procInst.AppID = processorObj.AppID;
                procInst.IsReady = processorObj.IsProcessorReady;
                var resultProcessor = _serviceState.SetProcessorReady(procInst);
                result.Message += resultProcessor.Message;
                result.Success = resultProcessor.Success;

                if (!resultProcessor.Success)
                {
                    var isSystemProcessorFailure = _serviceState.IsSystemProcessor(procInst.AppID);
                    if (isSystemProcessorFailure)
                    {
                        _logger.LogError(result.Message);
                    }
                    else
                    {
                        _logger.LogWarning(result.Message);
                    }
                }
                else
                {
                    _logger.LogInformation(result.Message);
                }
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

        private async Task<ResultObj> PaymentServiceReadyAsync(PaymentServiceInitObj? paymentObj)
        {
            var result = new ResultObj { Success = false, Message = "MessageAPI : PaymentServiceReady : " };
            if (!await ValidateBackendHmacAsync("paymentServiceReady", paymentObj, result)) return result;
            return PaymentServiceReady(paymentObj);
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

        private async Task<ResultObj> AlertServiceReadyAsync(AlertServiceInitObj? alertObj)
        {
            var result = new ResultObj { Success = false, Message = "MessageAPI : AlertServiceReady : " };
            if (!await ValidateBackendHmacAsync("alertServiceReady", alertObj, result)) return result;
            return AlertServiceReady(alertObj);
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

        private async Task<ResultObj> MonitorServiceReadyAsync(MonitorServiceInitObj? serviceObj)
        {
            var result = new ResultObj { Success = false, Message = "MessageAPI : MonitorServiceReady : " };
            if (!await ValidateBackendHmacAsync("monitorServiceReady", serviceObj, result)) return result;
            return MonitorServiceReady(serviceObj);
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

        private async Task<ResultObj> MonitorDataReadyAsync(MonitorDataInitObj? dataObj)
        {
            var result = new ResultObj { Success = false, Message = "MessageAPI : MonitorDataReady : " };
            if (!await ValidateBackendHmacAsync("monitorDataReady", dataObj, result)) return result;
            return MonitorDataReady(dataObj);
        }

        public async Task<ResultObj> PredictServiceReady([FromBody] MonitorMLInitObj? serviceObj)
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
            if (!await ValidateBackendHmacAsync("predictServiceReady", serviceObj, result)) return result;
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

        private async Task<bool> ValidateBackendHmacAsync(string operation, IBackendSignedMessage? message, ResultObj result)
        {
            if (MessageSecurityPolicyRegistry.Requires(operation, operation, MessageProtection.BackendHmac) &&
                message != null &&
                await _backendHmac.VerifyAsync(operation, operation, message).ConfigureAwait(false)) return true;

            result.Success = false;
            result.Message += " Error : invalid backend HMAC.";
            _logger.LogWarning("Scheduler message rejected. Operation={Operation}.", operation);
            return false;
        }


    }
}
