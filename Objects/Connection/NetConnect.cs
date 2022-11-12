using NetworkMonitor.Objects;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace NetworkMonitor.Connection
{
    public abstract class NetConnect : INetConnect
    {
        private MonitorPingInfo _monitorPingInfo;

        private PingParams _pingParams;
        // default value for timeout 1000ms;
        private int _timeout = 1000;
        private int _roundTrip;

        public int RoundTrip { get => _roundTrip; set => _roundTrip = value; }
        public MonitorPingInfo MonitorPingInfo { get => _monitorPingInfo; set => _monitorPingInfo = value; }
        public PingParams PingParams { get => _pingParams; set => _pingParams = value; }
        public int Timeout { get => _timeout; set => _timeout = value; }

        public abstract Task connect();
        protected void ProcessException(MonitorPingInfo monitorPingInfo, string endPointType, string message)
        {
            monitorPingInfo.MonitorStatus.Message = endPointType + " failed to connect: " + message;
            monitorPingInfo.MonitorStatus.IsUp = false;
            monitorPingInfo.MonitorStatus.DownCount++;
            monitorPingInfo.MonitorStatus.EventTime = DateTime.UtcNow;
            monitorPingInfo.PacketsLost++;
            monitorPingInfo.TimeOuts++;
            //Note we add one to packets sent because it is not updated till the finaly clause.
            monitorPingInfo.PacketsLostPercentage = (float)((double)monitorPingInfo.PacketsLost / (double)(monitorPingInfo.PacketsSent ) * 100);
            PingInfo pingInfo = new PingInfo();
            pingInfo.DateSent = DateTime.UtcNow;
            pingInfo.Status = message;
            pingInfo.RoundTripTime = -1;
            monitorPingInfo.PingInfos.Add(pingInfo);

        }

    }
}
