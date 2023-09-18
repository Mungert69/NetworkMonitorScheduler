using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Threading;
using Microsoft.Extensions.Configuration;
using NetworkMonitor.Objects;
using MailKit.Net.Smtp;
using MetroLog;
using MimeKit;
using NetworkMonitor.Utils.Helpers;
using NetworkMonitor.Objects.Factory;
using NetworkMonitor.Objects.Repository;
using NCrontab;

namespace NetworkMonitor.Scheduler.Services
{
    public class ProcessorInstance
    {
        public string ID;
        public bool IsReady = true;
        public bool IsReportSent = false;
    }
    public interface IServiceState
    {
        Task Init();
        bool IsAlertServiceReady { get; set; }
        bool IsPaymentServiceReady { get; set; }
        bool IsMonitorCheckServiceReady { get; set; }
        bool IsMonitorServiceReady { get; set; }
        List<ProcessorInstance> ProcessorInstances { get; }
        ResultObj SetProcessorReady(ProcessorInstance procInst);
        ResultObj CheckHealth();
        ResultObj SendHealthReport(string reportMessage);
        ResultObj ResetReportSent();
        public IRabbitRepo RabbitRepo { get; }
    }
    public class ServiceState : IServiceState
    {
        private List<ProcessorInstance> _processorInstances = new List<ProcessorInstance>();
        private Dictionary<string, List<DateTime>> _processorStateChanges = new Dictionary<string, List<DateTime>>();
        private List<DateTime> _monitorServiceStateChanges = new List<DateTime>();
        private List<DateTime> _monitorCheckServiceStateChanges = new List<DateTime>();
        private List<DateTime> _alertServiceStateChanges = new List<DateTime>();
        private List<DateTime> _paymentServiceStateChanges = new List<DateTime>();
        private bool _isAlertServiceReady = true;
        private bool _isPaymentServiceReady = true;
        private bool _isMonitorServiceReady = true;
        private bool _isMonitorCheckServiceReady = true;
        private bool _isMonitorServiceReportSent = false;
        private bool _isMonitorCheckServiceReportSent = false;
        private bool _isAlertServiceReportSent = false;
        private bool _isPaymentServiceReportSent = false;
        private IConfiguration _config;
        private ILogger _logger;
        private SystemParams _systemParams;
        private IRabbitRepo _rabbitRepo;
        private CancellationToken _token;
        private TimeSpan _pingScheduleInterval;
        private TimeSpan _monitorCheckInterval;
        private TimeSpan _paymentInterval;
        private TimeSpan _saveInterval;
        private TimeSpan _alertInterval;
        private TimeSpan _aIInterval;

        public IRabbitRepo RabbitRepo { get => _rabbitRepo; }
        public ServiceState(INetLoggerFactory loggerFactory, IConfiguration config, CancellationTokenSource cancellationTokenSource, IRabbitRepo rabbitRepo,ISystemParamsHelper systemParamsHelper)
        {
            _config = config;
            _logger = loggerFactory.GetLogger("ServiceState");
            _rabbitRepo=rabbitRepo;
            _token = cancellationTokenSource.Token;
            _token.Register(() => OnStopping());
            _systemParams = systemParamsHelper.GetSystemParams();
            _alertServiceStateChanges.Add(DateTime.UtcNow);
            _paymentServiceStateChanges.Add(DateTime.UtcNow);
            _monitorServiceStateChanges.Add(DateTime.UtcNow);
            _monitorCheckServiceStateChanges.Add(DateTime.UtcNow);
            List<ProcessorObj> processorList = new List<ProcessorObj>();

            _config.GetSection("ProcessorList").Bind(processorList);
            foreach (var processorObj in processorList)
            {
                ProcessorInstance procInst = new ProcessorInstance();
                procInst.ID = processorObj.AppID;
                procInst.IsReportSent = false;
                _processorInstances.Add(procInst);
                _processorStateChanges.Add(procInst.ID, new List<DateTime>());
                _logger.Info(" Success : added Processor AppID " + processorObj.AppID);
            }
            foreach (KeyValuePair<string, List<DateTime>> entry in _processorStateChanges)
            {
                entry.Value.Add(DateTime.UtcNow);
            }
           

            try {
     _pingScheduleInterval= GetScheduleInterval(_config["PingSchedule"]);
         _monitorCheckInterval=GetScheduleInterval(_config["MonitorCheckSchedule"]);
         _paymentInterval=GetScheduleInterval(_config["PaymentSchedule"]);
         _saveInterval=GetScheduleInterval(_config["SaveSchedule"]);
         _alertInterval=GetScheduleInterval(_config["AlertSchedule"]);
         _aIInterval=GetScheduleInterval(_config["AISchedule"]);
            }
            catch (Exception e){
 _logger.Error(" Could setup health check parameteres : " + e.ToString() + " . ");
            }


        }

        private TimeSpan GetScheduleInterval(string cronTabString)
        {
            var schedule = CrontabSchedule.Parse(cronTabString);

            var now = DateTime.Now;

            var nextOccurrence = schedule.GetNextOccurrence(now);
            var nextNextOccurrence = schedule.GetNextOccurrence(nextOccurrence);

            var interval = nextNextOccurrence - nextOccurrence;

            return interval;
        }
        private void OnStopping()
        {
            ResultObj result = new ResultObj();
            result.Message = " SERVICE SHUTDOWN : Starting shutdown of SchedulerService : ";
            try
            {
                result.Message += " Nothing to do. ";
                result.Success = true;
                _logger.Info(result.Message);
                _logger.Warn("SERVICE SHUTDOWN : Complete : ");
            }
            catch (Exception e)
            {
                _logger.Fatal("Error : Failed to run OnStopping during shutdown : Error Was : " + e.Message);
            }
        }
        public Task Init()
        {
            return Task.Run(() =>
              {
              });
        }
        public ResultObj ResetReportSent()
        {
            _isAlertServiceReportSent = false;
            _isMonitorServiceReportSent = false;
            _isMonitorCheckServiceReportSent = false;
            _isPaymentServiceReady = false;
            _processorInstances.ForEach(f =>
            {
                f.IsReportSent = false;
            });
            var result = new ResultObj();
            result.Success = true;
            result.Message = "Success : Reset Report Sent flags.";
            return result;
        }
        public bool IsPaymentServiceReady
        {
            get => _isPaymentServiceReady; set
            {
                _isPaymentServiceReady = value;
                _paymentServiceStateChanges.Add(DateTime.UtcNow);
            }
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
        public bool IsMonitorCheckServiceReady
        {
            get => _isMonitorCheckServiceReady; set
            {
                _isMonitorCheckServiceReady = value;
                _monitorCheckServiceStateChanges.Add(DateTime.UtcNow);
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
            if (_monitorServiceStateChanges.LastOrDefault() < DateTime.UtcNow.AddHours(-_saveInterval.Hours) && !_isMonitorServiceReportSent)
            {
                //alert MonitorService not changing state
                result.Success = false;
                var timeSpan = DateTime.UtcNow - _monitorServiceStateChanges.LastOrDefault();
                result.Message += "Failed : MonitorSerivce has not changed state for " + timeSpan.TotalHours + " h ";
                _isMonitorServiceReportSent = true;
            }
            if (_alertServiceStateChanges.LastOrDefault() < DateTime.UtcNow.AddMinutes(-_alertInterval.Minutes*2) && !_isAlertServiceReportSent)
            {
                //alert MonitorService not changing state
                result.Success = false;
                var timeSpan = DateTime.UtcNow - _alertServiceStateChanges.LastOrDefault();
                result.Message += "Failed : AlertSerivce has not changed state for " + timeSpan.TotalMinutes + " m ";
                _isAlertServiceReportSent = true;
            }
            if (_monitorCheckServiceStateChanges.LastOrDefault() < DateTime.UtcNow.AddMinutes(-_monitorCheckInterval.Minutes*2) && !_isMonitorCheckServiceReportSent)
            {
                //alert MonitorService not changing state
                result.Success = false;
                var timeSpan = DateTime.UtcNow - _monitorCheckServiceStateChanges.LastOrDefault();
                result.Message += "Failed : MonitorCheck has not changed state for " + timeSpan.TotalMinutes + " m ";
                _isMonitorCheckServiceReportSent = true;
            }
            if (_paymentServiceStateChanges.LastOrDefault() < DateTime.UtcNow.AddMinutes(-_paymentInterval.Minutes*2) && !_isPaymentServiceReportSent)
            {
                //payment MonitorService not changing state
                result.Success = false;
                var timeSpan = DateTime.UtcNow - _paymentServiceStateChanges.LastOrDefault();
                result.Message += "Failed : PaymentSerivce has not changed state for " + timeSpan.TotalMinutes + " m ";
                _isPaymentServiceReportSent = true;
            }
            foreach (var procInst in _processorInstances)
            {
                if (_processorStateChanges[procInst.ID].LastOrDefault() < DateTime.UtcNow.AddMinutes(-_pingScheduleInterval.Minutes*2) && !procInst.IsReportSent)
                {
                    //alert MonitorService not changing state
                    result.Success = false;
                    var timeSpan = DateTime.UtcNow - _processorStateChanges[procInst.ID].LastOrDefault();
                    result.Message += "Failed : Processor with AppID " + procInst.ID + " has not changed state for " + timeSpan.TotalMinutes + " m ";
                    procInst.IsReportSent = true;
                }
            }
            return result;
        }
    }
}
