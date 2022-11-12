namespace NetworkMonitor.Objects
{
    public class PingParams

    {

        private int _bufferLength;
        private int _timeOut;
        private string _schedule;
        private string _saveSchedule;
        private string _alertSchedule;
        private int _pingBurstNumber;
        private int _pingBurstDelay;
        private int _alertThreshold;
        private bool _isLogStats;
        private int _logStatsThreshold;
        private int _hostLimit;

        public int BufferLength { get => _bufferLength; set => _bufferLength = value; }
        public int Timeout { get => _timeOut; set => _timeOut = value; }
        public string Schedule { get => _schedule; set => _schedule = value; }

        public int PingBurstNumber { get => _pingBurstNumber; set => _pingBurstNumber = value; }
        public int PingBurstDelay { get => _pingBurstDelay; set => _pingBurstDelay = value; }
        public string SaveSchedule { get => _saveSchedule; set => _saveSchedule = value; }
        public string AlertSchedule { get => _alertSchedule; set => _alertSchedule = value; }
        public int AlertThreshold { get => _alertThreshold; set => _alertThreshold = value; }
        public int LogStatsThreshold { get => _logStatsThreshold; set => _logStatsThreshold = value; }
        public int HostLimit { get => _hostLimit; set => _hostLimit = value; }
    }
}
