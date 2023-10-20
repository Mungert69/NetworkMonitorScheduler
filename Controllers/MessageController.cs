using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using NetworkMonitor.Objects;
using NetworkMonitor.Objects.ServiceMessage;
using NetworkMonitor.Scheduler.Services;
using NetworkMonitor.Objects.Factory;
using System;
using System.Collections.Generic;
using NetworkMonitor.Utils;
using System.Linq;


namespace NetworkMonitor.Scheduler.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class MessageController : ControllerBase
    {
        private readonly ILogger _logger;
        private IServiceState _serviceState;

        public MessageController(ILogger<MessageController> logger, IServiceState serviceState)
        {
            _logger = logger;
            _serviceState=serviceState;
          
        }
       
        [HttpGet("ResetReportSent")]
        public ActionResult<ResultObj> ResetReportSent()
        {
            ResultObj result = new ResultObj();
            result.Success = false;
            result.Message = "MessageAPI : ResetReportSent : ";

            try
            {
               
                result=_serviceState.ResetReportSent();
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

 
    }
}
