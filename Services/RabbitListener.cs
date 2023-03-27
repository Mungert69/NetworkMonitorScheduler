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
    public class RabbitListener
    {
        private string _instanceName;
        private IModel _publishChannel;
        private ILogger _logger;
        private IServiceState _serviceState;
        private ConnectionFactory _factory;
        private IConnection _connection;
        List<RabbitMQObj> _rabbitMQObjs = new List<RabbitMQObj>();
        public RabbitListener(ILogger logger, IServiceState serviceState,  string instanceName, string hostname)
        {
            _logger = logger;
            _serviceState=serviceState;
            _instanceName = instanceName;
            _factory = new ConnectionFactory
            {
                HostName = hostname,
                UserName = "usercommonxf1",
                Password = "test12",
                VirtualHost = "/vhostuser",
                AutomaticRecoveryEnabled = true,
                Port = 5672
            };
            init();
        }
        public void init()
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
            _connection = _factory.CreateConnection();
            _publishChannel = _connection.CreateModel();
            _rabbitMQObjs.ForEach(r => r.ConnectChannel = _connection.CreateModel());
            var results = new List<ResultObj>();
            results.Add(DeclareQueues());
            results.Add(DeclareConsumers());
            results.Add(BindChannelToConsumer());
            bool flag = true;
            string messages = "";
            results.ForEach(f => messages += f.Message);
            results.ForEach(f => flag = f.Success && flag);
            if (flag) _logger.Info("Success : Setup RabbitListener messages were : " + messages);
            else _logger.Fatal("Error : Failed to setup RabbitListener messages were : " + messages);
        }
        private ResultObj DeclareQueues()
        {
            var result = new ResultObj();
            result.Message = " RabbitRepo DeclareQueues : ";
            try
            {
                _rabbitMQObjs.ForEach(rabbitMQObj =>
                    {
                        var args = new Dictionary<string, object>();
                        if (rabbitMQObj.MessageTimeout != 0)
                        {
                            args.Add("x-message-ttl", rabbitMQObj.MessageTimeout);
                        }
                        else args = null;
                        rabbitMQObj.QueueName = _instanceName + "-" + rabbitMQObj.ExchangeName;
                        rabbitMQObj.ConnectChannel.ExchangeDeclare(exchange: rabbitMQObj.ExchangeName, type: ExchangeType.Fanout, durable: true);
                        rabbitMQObj.ConnectChannel.QueueDeclare(queue: rabbitMQObj.QueueName,
                                             durable: true,
                                             exclusive: false,
                                             autoDelete: true,
                                             arguments: args
                                             );
                        rabbitMQObj.ConnectChannel.QueueBind(queue: rabbitMQObj.QueueName,
                                          exchange: rabbitMQObj.ExchangeName,
                                          routingKey: string.Empty);
                    });
                result.Success = true;
                result.Message += " Success : Declared all queues ";
            }
            catch (Exception e)
            {
                string message = " Error : failed to declate queues. Error was : " + e.ToString() + " . ";
                result.Message += message;
                Console.WriteLine(result.Message);
                result.Success = false;
            }
            return result;
        }
        private ResultObj DeclareConsumers()
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
                        result = AlertServiceReady(ConvertToList<AlertServiceInitObj>(model, ea));
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
        private ResultObj BindChannelToConsumer()
        {
            var result = new ResultObj();
            result.Message = " RabbitRepo BindChannelToConsumer : ";
            try
            {
                _rabbitMQObjs.ForEach(rabbitMQObj =>
                    {
                        rabbitMQObj.ConnectChannel.BasicConsume(queue: rabbitMQObj.QueueName,
                            autoAck: false,
                            consumer: rabbitMQObj.Consumer
                            );
                    });
                result.Success = true;
                result.Message += " Success :  bound all consumers to queues ";
            }
            catch (Exception e)
            {
                string message = " Error : failed to bind all consumers to queues. Error was : " + e.ToString() + " . ";
                result.Message += message;
                Console.WriteLine(result.Message);
                result.Success = false;
            }
            return result;
        }
        private T ConvertToObject<T>(object sender, BasicDeliverEventArgs @event) where T : class
        {
            T result = null;
            try
            {
                string json = Encoding.UTF8.GetString(@event.Body.ToArray(), 0, @event.Body.ToArray().Length);
                var cloudEvent = JsonConvert.DeserializeObject<CloudEvent>(json);
                JObject dataAsJObject = (JObject)cloudEvent.Data;
                result = dataAsJObject.ToObject<T>();
            }
            catch (Exception e)
            {
                _logger.Error("Error : Unable to convert Object. Error was : " + e.ToString());
            }
            return result;
        }

        private string ConvertToString(object sender, BasicDeliverEventArgs @event)
        {
            string result = null;
            try
            {
                string json = Encoding.UTF8.GetString(@event.Body.ToArray(), 0, @event.Body.ToArray().Length);
                var cloudEvent = JsonConvert.DeserializeObject<CloudEvent>(json);
                result = (string)cloudEvent.Data;
            }
            catch (Exception e)
            {
                _logger.Error("Error : Unable to convert Object. Error was : " + e.ToString());
            }
            return result;
        }

        private T ConvertToList<T>(object sender, BasicDeliverEventArgs @event) where T : class
        {
            T result = null;
            try
            {
                string json = Encoding.UTF8.GetString(@event.Body.ToArray(), 0, @event.Body.ToArray().Length);
                var cloudEvent = JsonConvert.DeserializeObject<CloudEvent>(json);
                JArray dataAsJObject = (JArray)cloudEvent.Data;
                result = dataAsJObject.ToObject<T>();
            }
            catch (Exception e)
            {
                _logger.Error("Error : Unable to convert Object. Error was : " + e.ToString());
            }
            return result;
        }



        public string PublishJsonZ<T>(string exchangeName, T obj) where T : class
        {
            var datajson = JsonUtils.writeJsonObjectToString<T>(obj);
            string datajsonZ = StringCompressor.Compress(datajson);
            CloudEvent cloudEvent = new CloudEvent
            {
                Id = "event-id",
                Type = "event-type",
                Source = new Uri("https://srv1.mahadeva.co.uk"),
                Time = DateTimeOffset.UtcNow,
                Data = datajsonZ
            };
            var formatter = new JsonEventFormatter();
            var json = formatter.ConvertToJObject(cloudEvent);
            string message = json.ToString();
            var body = Encoding.UTF8.GetBytes(message);
            _publishChannel.BasicPublish(exchange: exchangeName,
                                 routingKey: string.Empty,
                                 basicProperties: null,
                                 // body: formatter.EncodeBinaryModeEventData(cloudEvent));
                                 body: body);
            return datajsonZ;
        }
        public void Publish<T>(string exchangeName, T obj) where T : class
        {
            CloudEvent cloudEvent = new CloudEvent
            {
                Id = "event-id",
                Type = "event-type",
                Source = new Uri("https://srv1.mahadeva.co.uk"),
                Time = DateTimeOffset.UtcNow,
                Data = obj
            };
            var formatter = new JsonEventFormatter();
            var json = formatter.ConvertToJObject(cloudEvent);
            string message = json.ToString();
            var body = Encoding.UTF8.GetBytes(message);
            _publishChannel.BasicPublish(exchange: exchangeName,
                                 routingKey: string.Empty,
                                 basicProperties: null,
                                 // body: formatter.EncodeBinaryModeEventData(cloudEvent));
                                 body: body);
        }
        public void Publish(string exchangeName, Object obj)
        {
            CloudEvent cloudEvent = new CloudEvent
            {
                Id = "event-id",
                Type = "event-type",
                Source = new Uri("https://srv1.mahadeva.co.uk"),
                Time = DateTimeOffset.UtcNow,
                Data = obj
            };
            var formatter = new JsonEventFormatter();
            var json = formatter.ConvertToJObject(cloudEvent);
            string message = json.ToString();
            var body = Encoding.UTF8.GetBytes(message);
            _publishChannel.BasicPublish(exchange: exchangeName,
                                 routingKey: string.Empty,
                                 basicProperties: null,
                                 // body: formatter.EncodeBinaryModeEventData(cloudEvent));
                                 body: body);
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