using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Threading;
using Microsoft.Extensions.Configuration;
using NetworkMonitor.Objects;
using MailKit.Net.Smtp;
using Microsoft.Extensions.Logging;
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
        bool IsPredictServiceReady { get; set; }
        bool IsPaymentServiceReady { get; set; }
        bool IsMonitorCheckServiceReady { get; set; }
        bool IsMonitorDataSaveReady { get; set; }
        bool IsMonitorCheckDataReady { get; set; }
        bool IsMonitorDataPurgeReady { get; set; }
        List<ProcessorObj> EnabledProcessorInstances { get; }
        ResultObj SetProcessorReady(ProcessorObj procInst);
        ResultObj CheckHealth();
        ResultObj SendHealthReport(string reportMessage);
        ResultObj ResetReportSent();
        public IRabbitRepo RabbitRepo { get; }
    }
    public class ServiceState : IServiceState
    {
       // private List<ProcessorInstance> _processorInstances = new List<ProcessorInstance>();
        private Dictionary<string, List<DateTime>> _processorStateChanges = new Dictionary<string, List<DateTime>>();
        private List<DateTime> _monitorDataSaveStateChanges = new List<DateTime>();
        private List<DateTime> _monitorCheckServiceStateChanges = new List<DateTime>();
        private List<DateTime> _monitorDataPurgeStateChanges = new List<DateTime>();
        private List<DateTime> _monitorCheckDataStateChanges = new List<DateTime>();
        private List<DateTime> _alertServiceStateChanges = new List<DateTime>();
        private List<DateTime> _predictServiceStateChanges = new List<DateTime>();
        private List<DateTime> _paymentServiceStateChanges = new List<DateTime>();
        private bool _isAlertServiceReady = true;
        private bool _isPredictServiceReady = true;
        private bool _isPaymentServiceReady = true;
        private bool _isMonitorDataSaveReady = true;
        private bool _isMonitorCheckServiceReady = true;
        private bool _isMonitorDataPurgeReady = true;
        private bool _isMonitorCheckDataReady = true;
        private bool _isMonitorDataSaveReportSent = false;
        private bool _isMonitorCheckServiceReportSent = false;
        private bool _isMonitorDataPurgeReportSent = false;
        private bool _isMonitorCheckDataReportSent = false;
        private bool _isAlertServiceReportSent = false;
        private bool _isPredictServiceReportSent = false;
        private bool _isPaymentServiceReportSent = false;
        private IConfiguration _config;
        private ILogger _logger;
        private SystemParams _systemParams;
        private IRabbitRepo _rabbitRepo;
        private CancellationToken _token;
        private TimeSpan _pingScheduleInterval;
        private TimeSpan _monitorCheckInterval;
        private TimeSpan _paymentInterval;
        private TimeSpan _dataSaveInterval;
        private TimeSpan _alertInterval;
        private TimeSpan _predictInterval;
        private TimeSpan _aIInterval;
        private TimeSpan _dataCheckInterval;
        private TimeSpan _dataPurgeInterval;
        private IProcessorState _processorState;
        private IFileRepo _fileRepo;

        public IRabbitRepo RabbitRepo { get => _rabbitRepo; }
        public ServiceState(ILogger<ServiceState> logger, IConfiguration config, CancellationTokenSource cancellationTokenSource, IRabbitRepo rabbitRepo, ISystemParamsHelper systemParamsHelper, IProcessorState processorState, IFileRepo fileRepo)
        {
            _config = config;
            _logger = logger;
            _rabbitRepo = rabbitRepo;
            _fileRepo=fileRepo;
            _token = cancellationTokenSource.Token;
            _token.Register(() => OnStopping());
            _processorState = processorState;
            _processorState.OnAppIDAdded += HandleAppIDAdded;
            _systemParams = systemParamsHelper.GetSystemParams();
            _alertServiceStateChanges.Add(DateTime.UtcNow);
            _predictServiceStateChanges.Add(DateTime.UtcNow);
            _paymentServiceStateChanges.Add(DateTime.UtcNow);
            _monitorDataSaveStateChanges.Add(DateTime.UtcNow);
            _monitorCheckServiceStateChanges.Add(DateTime.UtcNow);
            _monitorDataPurgeStateChanges.Add(DateTime.UtcNow);
            _monitorCheckDataStateChanges.Add(DateTime.UtcNow);
            var processorList = new List<ProcessorObj>();
             try
            {
                _fileRepo.CheckFileExists("ProcessorList", _logger);
                processorList = _fileRepo.GetStateJson<List<ProcessorObj>>("ProcessorList");

            }
            catch (Exception e)
            {
                _logger.LogInformation($" Error : Unable to get Processor List from State . Error was : {e.Message}");

            }
             if (processorList == null || processorList.Count==0)
            {

                _logger.LogError(" Error : No processors in processor list .");
                processorList = new List<ProcessorObj>();
            }
            else {
                _logger.LogInformation($" Success : Got {processorList.Count} processors from state . ");
            }
            _processorState.ProcessorList = processorList;
            //List<ProcessorObj> processorList = new List<ProcessorObj>();

            //_config.GetSection("ProcessorList").Bind(processorList);
            foreach (var processorObj in _processorState.EnabledProcessorList)
            {
                processorObj.IsReportSent=false;
                _processorStateChanges.Add(processorObj.AppID, new List<DateTime>());
                _logger.LogInformation(" Success : added Processor AppID " + processorObj.AppID);
            }
            foreach (KeyValuePair<string, List<DateTime>> entry in _processorStateChanges)
            {
                entry.Value.Add(DateTime.UtcNow);
            }


            try
            {
                _pingScheduleInterval = GetScheduleInterval(_config["PingSchedule"] ?? "* * * * *");
                _monitorCheckInterval = GetScheduleInterval(_config["MonitorCheckSchedule"] ?? "* * * * *");
                _paymentInterval = GetScheduleInterval(_config["PaymentSchedule"] ?? "* * * * *");
                _dataSaveInterval = GetScheduleInterval(_config["DataSaveSchedule"] ?? "0 */6 * * *");
                _alertInterval = GetScheduleInterval(_config["AlertSchedule"] ?? "* * * * *");
                _predictInterval = GetScheduleInterval(_config["PredictSchedule"] ?? "*/5 * * * *");
                _aIInterval = GetScheduleInterval(_config["AISchedule"] ?? "5 0 * * *");
                _dataCheckInterval = GetScheduleInterval(_config["DataCheckSchedule"] ?? "* * * * *");
                _dataPurgeInterval = GetScheduleInterval(_config["DataPurgeSchedule"] ?? "0 1 * * 0");
            }
            catch (Exception e)
            {
                _logger.LogError(" Could setup health check parameteres : " + e.ToString() + " . ");
            }


        }

         private ResultObj HandleAppIDAdded(string appID)
        {
            var result = new ResultObj();
            result.Message = " HandleAppIDAdded : ";
            result.Success = true;
            try
            {

                var isExists=_processorStateChanges[appID];
                if (isExists!=null){
                    _processorStateChanges[appID].Add(DateTime.UtcNow);      
                    result.Message += $" Success: Updated StateChange in processorStateChange for AppID {appID} . ";
          
                }
                else {
                     _processorStateChanges.Add(appID, new List<DateTime>());
                _processorStateChanges[appID].Add(DateTime.UtcNow);      
                result.Message += $" Success : Added new AppID to processorStateChanges for AppID {appID} . ";
          
                }
                     
            }
            catch (Exception e)
            {
                result.Message += " Error : Could not add new AppID to processorStateChanges . Error was : " + e.Message;
                result.Success = false;
            }
            return result;
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
                _logger.LogInformation(result.Message);
                _logger.LogWarning("SERVICE SHUTDOWN : Complete : ");
            }
            catch (Exception e)
            {
                _logger.LogCritical("Error : Failed to run OnStopping during shutdown : Error Was : " + e.Message);
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
            _isPredictServiceReportSent = false;
            _isMonitorDataSaveReportSent = false;
            _isMonitorCheckServiceReportSent = false;
            _isMonitorDataPurgeReportSent = false;
            _isMonitorCheckDataReportSent = false;
            _isPaymentServiceReady = false;
            _processorState.EnabledProcessorList.ForEach(f =>
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
                if (value != _isPaymentServiceReady)
                {
                    _isPaymentServiceReady = value;
                    _paymentServiceStateChanges.Add(DateTime.UtcNow);
                }

            }
        }
        public bool IsAlertServiceReady
        {
            get => _isAlertServiceReady; set
            {
                if (value != _isAlertServiceReady)
                {
                    _isAlertServiceReady = value;
                    _alertServiceStateChanges.Add(DateTime.UtcNow);
                }

            }
        }
        public bool IsPredictServiceReady
        {
            get => _isPredictServiceReady; set
            {
                if (value != _isPredictServiceReady)
                {
                    _isPredictServiceReady = value;
                    _predictServiceStateChanges.Add(DateTime.UtcNow);
                }

            }
        }
        public bool IsMonitorDataSaveReady
        {
            get => _isMonitorDataSaveReady; set
            {
                if (value != _isMonitorDataSaveReady)
                {
                    _isMonitorDataSaveReady = value;
                    _monitorDataSaveStateChanges.Add(DateTime.UtcNow);
                }

            }
        }
        public bool IsMonitorCheckServiceReady
        {
            get => _isMonitorCheckServiceReady; set
            {
                if (value != _isMonitorCheckServiceReady)
                {
                    _isMonitorCheckServiceReady = value;
                    _monitorCheckServiceStateChanges.Add(DateTime.UtcNow);
                }

            }
        }
        public bool IsMonitorDataPurgeReady
        {
            get => _isMonitorDataPurgeReady; set
            {
                if (value != _isMonitorDataPurgeReady)
                {
                    _isMonitorDataPurgeReady = value;
                    _monitorDataPurgeStateChanges.Add(DateTime.UtcNow);
                }

            }
        }
        public bool IsMonitorCheckDataReady
        {
            get => _isMonitorCheckDataReady; set
            {
                if (value != _isMonitorCheckDataReady)
                {
                    _isMonitorCheckDataReady = value;
                    _monitorCheckDataStateChanges.Add(DateTime.UtcNow);
                }

            }
        }
        public List<ProcessorObj> EnabledProcessorInstances
        {
            get => _processorState.EnabledProcessorList;
        }
        public ResultObj SetProcessorReady(ProcessorObj procInst)
        {
            var result = new ResultObj();
            try
            {
                var processorInstance = _processorState.ProcessorList.FirstOrDefault(f => f.AppID == procInst.AppID);
                if (processorInstance != null)
                {
                    processorInstance.IsReady = procInst.IsReady;
                    _processorStateChanges[procInst.AppID].Add(DateTime.UtcNow);
                    result.Success = true;
                    result.Message = " Success : Set Processor Ready for AppID " + procInst.AppID + " to " + procInst.IsReady;
                }
                else {
                     result.Success = false;
                    result.Message = " Error  : Failed to find Processor with AppID " + procInst.AppID;
            
                }

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
            var publicIP=_systemParams.PublicIPAddress;
            string publicIPStr="IP N/A";
            if (publicIP!=null) publicIP.ToString();
            alertMessage.Message += "\n\nThis message was sent by the messenger running at " + _systemParams.ThisSystemUrl.ExternalUrl + " (" + publicIPStr + " Health Report message : " + reportMessage;
            string? emailFrom = _systemParams.SystemEmail;
            string? systemPassword = _systemParams.SystemPassword;
            string? systemUser = _systemParams.SystemUser;
            int mailServerPort = _systemParams.MailServerPort;
            bool mailServerUseSSL = _systemParams.MailServerUseSSL;
            string? mailServer = _systemParams.MailServer;
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
                _logger.LogInformation(result.Message);
            }
            catch (Exception e)
            {
                result.Message = "Email with subject " + alertMessage.Subject + " failed to send . Error was :" + e.Message.ToString();
                result.Success = false;
                _logger.LogError(result.Message);
            }
            return result;
        }
        public ResultObj CheckHealth()
        {

            var result = new ResultObj();
            result.Success = true;
            if (_monitorDataSaveStateChanges.LastOrDefault() < DateTime.UtcNow.AddHours(-_dataSaveInterval.TotalHours) && !_isMonitorDataSaveReportSent)
            {
                //alert MonitorService not changing state
                result.Success = false;
                var timeSpan = DateTime.UtcNow - _monitorDataSaveStateChanges.LastOrDefault();
                result.Message += "Failed : DataSave has not changed state for " + timeSpan.TotalHours + " h ";
                _isMonitorDataSaveReportSent = true;
            }
            if (_monitorDataPurgeStateChanges.LastOrDefault() < DateTime.UtcNow.AddHours(-_dataPurgeInterval.TotalHours) && !_isMonitorDataPurgeReportSent)
            {
                //alert MonitorData not changing state
                result.Success = false;
                var timeSpan = DateTime.UtcNow - _monitorDataPurgeStateChanges.LastOrDefault();
                result.Message += "Failed : MonitorPurge has not changed state for " + timeSpan.TotalHours + " h ";
                _isMonitorDataPurgeReportSent = true;
            }
            if (_alertServiceStateChanges.LastOrDefault() < DateTime.UtcNow.AddMinutes(-_alertInterval.TotalMinutes * 2) && !_isAlertServiceReportSent)
            {
                //alert MonitorService not changing state
                result.Success = false;
                var timeSpan = DateTime.UtcNow - _alertServiceStateChanges.LastOrDefault();
                result.Message += "Failed : AlertSerivce has not changed state for " + timeSpan.TotalMinutes + " m ";
                _isAlertServiceReportSent = true;
            }
            if (_predictServiceStateChanges.LastOrDefault() < DateTime.UtcNow.AddMinutes(-_predictInterval.TotalMinutes * 2) && !_isPredictServiceReportSent)
            {
                //predict MonitorMLService not changing state
                result.Success = false;
                var timeSpan = DateTime.UtcNow - _predictServiceStateChanges.LastOrDefault();
                result.Message += "Failed : MonitorMLSerivce has not changed state for " + timeSpan.TotalMinutes + " m ";
                _isAlertServiceReportSent = true;
            }
            if (_monitorCheckServiceStateChanges.LastOrDefault() < DateTime.UtcNow.AddMinutes(-_monitorCheckInterval.TotalMinutes * 2) && !_isMonitorCheckServiceReportSent)
            {
                //alert MonitorService not changing state
                result.Success = false;
                var timeSpan = DateTime.UtcNow - _monitorCheckServiceStateChanges.LastOrDefault();
                result.Message += "Failed : MonitorCheck has not changed state for " + timeSpan.TotalMinutes + " m ";
                _isMonitorCheckServiceReportSent = true;
            }
            if (_monitorCheckDataStateChanges.LastOrDefault() < DateTime.UtcNow.AddMinutes(-_dataCheckInterval.TotalMinutes * 2) && !_isMonitorCheckDataReportSent)
            {
                //alert MonitorData not changing state
                result.Success = false;
                var timeSpan = DateTime.UtcNow - _monitorCheckDataStateChanges.LastOrDefault();
                result.Message += "Failed : DataCheck has not changed state for " + timeSpan.TotalMinutes + " m ";
                _isMonitorCheckDataReportSent = true;
            }
            if (_paymentServiceStateChanges.LastOrDefault() < DateTime.UtcNow.AddMinutes(-_paymentInterval.TotalMinutes * 2) && !_isPaymentServiceReportSent)
            {
                //payment MonitorService not changing state
                result.Success = false;
                var timeSpan = DateTime.UtcNow - _paymentServiceStateChanges.LastOrDefault();
                result.Message += "Failed : PaymentSerivce has not changed state for " + timeSpan.TotalMinutes + " m ";
                _isPaymentServiceReportSent = true;
            }
            foreach (var procInst in _processorState.EnabledSystemProcessorList())
            {
                if (_processorStateChanges[procInst.AppID].LastOrDefault() < DateTime.UtcNow.AddMinutes(-_pingScheduleInterval.TotalMinutes * 2) && !procInst.IsReportSent)
                {
                    //alert MonitorService not changing state
                    result.Success = false;
                    var timeSpan = DateTime.UtcNow - _processorStateChanges[procInst.AppID].LastOrDefault();
                    result.Message += "Failed : Processor with AppID " + procInst.AppID + " has not changed state for " + timeSpan.TotalMinutes + " m ";
                    procInst.IsReportSent = true;
                }
            }
            return result;
        }
    }
}
