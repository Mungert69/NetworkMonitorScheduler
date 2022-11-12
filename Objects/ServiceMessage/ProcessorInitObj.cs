using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace NetworkMonitor.Objects.ServiceMessage
{
    public class ProcessorInitObj
    {
        private List<MonitorIP> _monitorIPs;

        private List<MonitorPingInfo> _savedMonitorPingInfos;

        private PingParams _pingParams;
        private bool _isProcessorStarted = false;
        private bool _reset=false;
        private bool _totalReset=false;

        public List<MonitorIP> MonitorIPs { get => _monitorIPs; set => _monitorIPs = value; }
        public List<MonitorPingInfo> SavedMonitorPingInfos { get => _savedMonitorPingInfos; set => _savedMonitorPingInfos = value; }
        public PingParams PingParams { get => _pingParams; set => _pingParams = value; }
        public bool IsProcessorStarted { get => _isProcessorStarted; set => _isProcessorStarted = value; }
        public bool Reset { get => _reset; set => _reset = value; }
        public bool TotalReset { get => _totalReset; set => _totalReset = value; }
    }
}