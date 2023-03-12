using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Configuration;
using NetworkMonitor.Objects;
using MailKit.Net.Smtp;
using MetroLog;
using MimeKit;
using NetworkMonitor.Utils.Helpers;
using NetworkMonitor.Objects.Factory;


namespace NetworkMonitor.Scheduler.Services
{
    public class ProcessorInstance
    {
        public string ID;
        public bool IsReady = true;
        public bool IsReportSent=false;
    }
    public interface IServiceState
    {
        bool IsAlertServiceReady { get; set; }
        bool IsMonitorServiceReady { get; set; }

        List<ProcessorInstance> ProcessorInstances { get; }

        ResultObj SetProcessorReady(ProcessorInstance procInst);
        ResultObj CheckHealth();

        ResultObj SendHealthReport(string reportMessage);

        ResultObj ResetReportSent();
    }

    public class ServiceState : IServiceState
    {
        private List<ProcessorInstance> _processorInstances = new List<ProcessorInstance>();

        private Dictionary<string, List<DateTime>> _processorStateChanges = new Dictionary<string, List<DateTime>>();
        private List<DateTime> _monitorServiceStateChanges = new List<DateTime>();
        private List<DateTime> _alertServiceStateChanges = new List<DateTime>();
        private bool _isAlertServiceReady = true;
        private bool _isMonitorServiceReady = true;
        private bool _isMonitorServiceReportSent = false;
        private bool _isAlertServiceReportSent = false;

        private IConfiguration _config;
        private ILogger _logger;

        private SystemParams _systemParams;

        public ServiceState(INetLoggerFactory loggerFactory, IConfiguration config)
        {
            _config = config;
            _logger = loggerFactory.GetLogger("ServiceState");
            _systemParams = SystemParamsHelper.getSystemParams(_config, _logger);
            List<ProcessorObj> processorList = new List<ProcessorObj>();
            _config.GetSection("ProcessorList").Bind(processorList);
            foreach ( var processorObj in processorList)
            {
                ProcessorInstance procInst = new ProcessorInstance();
                procInst.ID = processorObj.AppID;
                procInst.IsReportSent=false;
                _processorInstances.Add(procInst);
                _processorStateChanges.Add(procInst.ID, new List<DateTime>());
                _logger.Info(" Success : added Processor AppID "+processorObj.AppID);

            }
        }

        public ResultObj ResetReportSent()
        {
            _isAlertServiceReportSent = false;
            _isMonitorServiceReportSent = false;
            _processorInstances.ForEach( f => {
                f.IsReportSent=false;
            });
            var result = new ResultObj();
            result.Success = true;
            result.Message="Success : Reset Report Sent flags.";
            return result;
        }

        public bool IsAlertServiceReady
        {
            get => _isAlertServiceReady; set
            {
                _isAlertServiceReady = value;
                _alertServiceStateChanges.Add(DateTime.UtcNow);
            }
        }
        public bool IsMonitorServiceReady
        {
            get => _isMonitorServiceReady; set
            {
                _isMonitorServiceReady = value;
                _monitorServiceStateChanges.Add(DateTime.UtcNow);
            }
        }
        public List<ProcessorInstance> ProcessorInstances
        {
            get => _processorInstances;
        }

        public ResultObj SetProcessorReady(ProcessorInstance procInst)
        {
            var result = new ResultObj();

            try
            {
                _processorInstances.FirstOrDefault(f => f.ID == procInst.ID).IsReady = procInst.IsReady;
                _processorStateChanges[procInst.ID].Add(DateTime.UtcNow);
                result.Success = true;
                result.Message = "Success : Set Processor Ready for AppID " + procInst.ID + " to " + procInst.IsReady;
            }
            catch (Exception e)
            {
                result.Success = false;
                result.Message = "Error : to set Processor Ready. Error was : " + e.Message.ToString();
            }
            return result;

        }


        public ResultObj SendHealthReport(string reportMessage)

        {
            ResultObj result = new ResultObj();
            var alertMessage = new AlertMessage();
            alertMessage.Message += "\n\nThis message was sent by the messenger running at " + _systemParams.ThisSystemUrl.ExternalUrl + " (" + _systemParams.PublicIPAddress.ToString() + " Health Report message : " + reportMessage;

            string emailFrom = _systemParams.SystemEmail;
            string systemPassword = _systemParams.SystemPassword;
            string systemUser = _systemParams.SystemUser;
            int mailServerPort = _systemParams.MailServerPort;
            bool mailServerUseSSL = _systemParams.MailServerUseSSL;
            string mailServer = _systemParams.MailServer;

            var userInfo = new UserInfo();
            userInfo.Email = emailFrom;
            userInfo.Email_verified = true;
            userInfo.Name = "System Admin";

            alertMessage.UserInfo = userInfo;
            alertMessage.Subject = "Servive Health Report";

            try
            {
                MimeMessage message = new MimeMessage();
                MailboxAddress from = new MailboxAddress("FreeNetworkMonitor Health Monitor",
                emailFrom);
                message.From.Add(from);


                MailboxAddress to = new MailboxAddress(alertMessage.Name,
                alertMessage.EmailTo);
                message.To.Add(to);

                //message.Subject = "Network Monitor Alert : Host Down";
                message.Subject = alertMessage.Subject;
                BodyBuilder bodyBuilder = new BodyBuilder();

                bodyBuilder.TextBody = alertMessage.Message;
                //bodyBuilder.Attachments.Add(_env.WebRootPath + "\\file.png");
                message.Body = bodyBuilder.ToMessageBody();


                SmtpClient client = new SmtpClient();
                client.ServerCertificateValidationCallback = (mysender, certificate, chain, sslPolicyErrors) => { return true; };
                client.CheckCertificateRevocation = false;
                if (mailServerUseSSL)
                {
                    client.Connect(mailServer, mailServerPort, true);
                }
                else
                {
                    client.Connect(mailServer, mailServerPort, MailKit.Security.SecureSocketOptions.StartTls);
                }
                client.Authenticate(systemUser, systemPassword);

                client.Send(message);
                client.Disconnect(true);
                client.Dispose();
                result.Message = "Email with subject " + alertMessage.Subject + " sent ok";
                result.Success = true;
                _logger.Info(result.Message);
            }
            catch (Exception e)
            {
                result.Message = "Email with subject " + alertMessage.Subject + " failed to send . Error was :" + e.Message.ToString();
                result.Success = false;
                _logger.Error(result.Message);
            }
            return result;

        }



        public ResultObj CheckHealth()
        {

            var result = new ResultObj();
            result.Success = true;

            if (_monitorServiceStateChanges.LastOrDefault() < DateTime.UtcNow.AddHours(-6) && !_isMonitorServiceReportSent)
            {
                //alert MonitorService not changing state
                result.Success = false;
                var timeSpan = DateTime.UtcNow - _monitorServiceStateChanges.LastOrDefault();
                result.Message += "Failed : MonitorSerivce has not changed state for " + timeSpan.TotalHours + " h ";
                _isMonitorServiceReportSent=true;
            }
            if (_alertServiceStateChanges.LastOrDefault() < DateTime.UtcNow.AddMinutes(-2) && !_isAlertServiceReportSent)
            {
                //alert MonitorService not changing state
                result.Success = false;
                var timeSpan = DateTime.UtcNow - _alertServiceStateChanges.LastOrDefault();
                result.Message += "Failed : AlertSerivce has not changed state for " + timeSpan.TotalMinutes + " m ";
                _isAlertServiceReportSent=true;
            }

            foreach (var procInst in _processorInstances)
            {
                if (_processorStateChanges[procInst.ID].LastOrDefault() < DateTime.UtcNow.AddMinutes(-2) && !procInst.IsReportSent)
                {
                    //alert MonitorService not changing state
                    result.Success = false;
                    var timeSpan = DateTime.UtcNow - _processorStateChanges[procInst.ID].LastOrDefault();
                    result.Message += "Failed : Processor with AppID " + procInst.ID + " has not changed state for " + timeSpan.TotalMinutes + " m ";
                    procInst.IsReportSent=true;
                }
            }

            return result;
        }

    }
}
