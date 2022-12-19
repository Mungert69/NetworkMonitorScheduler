using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using NetworkMonitor.Objects;
using NetworkMonitor.Objects.ServiceMessage;
using NetworkMonitor.Scheduler.Services;
using System;
using System.Collections.Generic;
using NetworkMonitor.Utils;
using System.Linq;

using Dapr;

namespace NetworkMonitor.Scheduler.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class MessageController : ControllerBase
    {
        private readonly ILogger<MessageController> _logger;
        private IServiceState _serviceState;

        public MessageController(ILogger<MessageController> logger, IServiceState serviceState)
        {
            _logger = logger;
            _serviceState=serviceState;
          
        }
        [Topic("pubsub", "processorReady")]
        [HttpPost("processorReady")]
        [Consumes("application/json")]
        public ActionResult<ResultObj> ProcessorReady([FromBody] ProcessorInitObj processorObj)
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

        [Topic("pubsub", "alertServiceReady")]
        [HttpPost("alertServiceReady")]
        [Consumes("application/json")]
        public ActionResult<ResultObj> AlertServiceReady([FromBody] AlertServiceInitObj alertObj)
        {
            ResultObj result = new ResultObj();
            result.Success = false;
            result.Message = "MessageAPI : AlertServiceReady : ";

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

        [Topic("pubsub", "monitorServiceReady")]
        [HttpPost("monitorServiceReady")]
        [Consumes("application/json")]
        public ActionResult<ResultObj> MonitorServiceReady([FromBody] MonitorServiceInitObj monitorObj)
        {
            ResultObj result = new ResultObj();
            result.Success = false;
            result.Message = "MessageAPI : MonitorServiceReady : ";

            try
            {
                _serviceState.IsMonitorServiceReady = monitorObj.IsServiceReady;
                result.Message += "Success set MonitorServiceReady to " + monitorObj.IsServiceReady;
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




    }
}
