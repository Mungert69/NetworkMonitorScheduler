using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace NetworkMonitor.Objects.ServiceMessage
{
    public class ProcessorQueueDicObj
    {
        private List<MonitorIP> _monitorIPs;
        private string _userId;

        public List<MonitorIP> MonitorIPs { get => _monitorIPs; set => _monitorIPs = value; }
        public string UserId { get => _userId; set => _userId = value; }
    }
}