using System;

namespace NetworkMonitorScheduler.Services{
    public interface IServiceState
    {
        bool IsProcessorReady { get; set; }
        bool IsAlertServiceReady { get; set; }
        bool IsMonitorServiceReady { get; set; }
    }

    public class ServiceState : IServiceState
    {
        private bool _isProcessorReady=true;
        private bool _isAlertServiceReady=true;
        private bool _isMonitorServiceReady=true;

        public bool IsProcessorReady { get => _isProcessorReady; set => _isProcessorReady = value; }
        public bool IsAlertServiceReady { get => _isAlertServiceReady; set => _isAlertServiceReady = value; }
        public bool IsMonitorServiceReady { get => _isMonitorServiceReady; set => _isMonitorServiceReady = value; }
    }
}
