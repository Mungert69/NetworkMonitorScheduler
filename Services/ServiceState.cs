using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Configuration;
using NetworkMonitor.Objects;

namespace NetworkMonitor.Scheduler.Services
{
    public class ProcessorInstance
    {
        public string ID;
        public bool IsReady = true;
    }
    public interface IServiceState
    {
        bool IsAlertServiceReady { get; set; }
        bool IsMonitorServiceReady { get; set; }

        List<ProcessorInstance> ProcessorInstances { get; }

        ResultObj SetProcessorReady(ProcessorInstance procInst);
    }

    public class ServiceState : IServiceState
    {
        private List<ProcessorInstance> _processorInstances = new List<ProcessorInstance>();

        private Dictionary<string, List<DateTime>> _processorStateChanges = new Dictionary<string, List<DateTime>>();
        private List<DateTime> _monitorServiceStateChanges = new List<DateTime>();
        private List<DateTime> _alertServiceStateChanges = new List<DateTime>();
        private bool _isAlertServiceReady = true;
        private bool _isMonitorServiceReady = true;
        private IConfiguration _config;
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
                result.Message = "Success : Set Processor Ready for AppID "+procInst.ID+" to " + procInst.IsReady;
            }
            catch (Exception e)
            {
                result.Success = false;
                result.Message = "Error : to set Processor Ready. Error was : " + e.Message.ToString();
            }
            return result;

        }

        public ServiceState(IConfiguration config)
        {
            _config = config;
            List<string> processorList = new List<string>();
            _config.GetSection("ProcessorList").Bind(processorList);
            foreach (string appID in processorList)
            {
                ProcessorInstance procInst = new ProcessorInstance();
                procInst.ID = appID;
                _processorInstances.Add(procInst);
                _processorStateChanges.Add(procInst.ID, new List<DateTime>());

            }
        }

    }
}
