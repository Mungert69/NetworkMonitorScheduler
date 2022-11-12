using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace NetworkMonitor.Objects.ServiceMessage
{
    public class ProcessorConnectObj
    {
        private int _nextRunInterval;
        private int _maxBuffer=2000;

        public int NextRunInterval { get => _nextRunInterval; set => _nextRunInterval = value; }
        public int MaxBuffer { get => _maxBuffer; set => _maxBuffer = value; }
    }
}