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
        ResultObj CheckHealth();
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

        public ResultObj CheckHealth(){

            var result=new ResultObj();
            result.Success=true;

            if (_monitorServiceStateChanges.LastOrDefault()<DateTime.UtcNow.AddHours(-6)){
                //alert MonitorService not changing state
                result.Success=false;
                var timeSpan=DateTime.UtcNow-_monitorServiceStateChanges.LastOrDefault();

                result.Message+="Failed : MonitorSerivce has not changed state for "+timeSpan.TotalMinutes;
            }
             if (_alertServiceStateChanges.LastOrDefault()<DateTime.UtcNow.AddMinutes(-2)){
                //alert MonitorService not changing state
                result.Success=false;
                var timeSpan=DateTime.UtcNow-_alertServiceStateChanges.LastOrDefault();

                result.Message+="Failed : AlertSerivce has not changed state for "+timeSpan.TotalMinutes;
            }

            foreach (var procInst in _processorInstances){
                  if (_processorStateChanges[procInst.ID].LastOrDefault()<DateTime.UtcNow.AddMinutes(-2)){
                //alert MonitorService not changing state
                result.Success=false;
                var timeSpan=DateTime.UtcNow-_processorStateChanges[procInst.ID].LastOrDefault();

                result.Message+="Failed : Processor with AppID "+procInst.ID+" has not changed state for "+timeSpan.TotalMinutes;
            }
            }

            return result;
        }

    }
}
