using System;
using System.Collections.Generic;
using Microsoft.Extensions.Configuration;

namespace NetworkMonitorScheduler.Services
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

        List<ProcessorInstance> ProcessorInstances { get; set; }
    }

    public class ServiceState : IServiceState
    {
        private List<ProcessorInstance> _processorInstances = new List<ProcessorInstance>();
        private bool _isAlertServiceReady = true;
        private bool _isMonitorServiceReady = true;
        private IConfiguration _config;
        public bool IsAlertServiceReady { get => _isAlertServiceReady; set => _isAlertServiceReady = value; }
        public bool IsMonitorServiceReady { get => _isMonitorServiceReady; set => _isMonitorServiceReady = value; }
        public List<ProcessorInstance> ProcessorInstances { get => _processorInstances; set => _processorInstances = value; }

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

            }
        }

    }
}
