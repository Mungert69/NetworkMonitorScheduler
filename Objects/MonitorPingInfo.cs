using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;

namespace NetworkMonitor.Objects
{
    public class MonitorPingInfo
    {
        // Empty constructor for EF
        public MonitorPingInfo()
        {
        }
        // Copy constructor
        public MonitorPingInfo(MonitorPingInfo copy)
        {
            ID = copy.ID;
            DataSetID = copy.DataSetID;
            Status = copy.Status;
            DestinationUnreachable = copy.DestinationUnreachable;
            MonitorIPID = copy.MonitorIPID;
            Timeout = copy.Timeout;
            TimeOuts = copy.TimeOuts;
            Address = copy.Address;
            UserID = copy.UserID;
            EndPointType = copy.EndPointType;
            PacketsRecieved = copy.PacketsRecieved;
            PacketsLost = copy.PacketsLost;
            PacketsLostPercentage = copy.PacketsLostPercentage;
            RoundTripTimeMaximum = copy.RoundTripTimeMaximum;
            RoundTripTimeAverage = copy.RoundTripTimeAverage;
            RoundTripTimeTotal = copy.RoundTripTimeTotal;
            RoundTripTimeMinimum = copy.RoundTripTimeMinimum;
            DateStarted = copy.DateStarted;
            MonitorStatus = copy.MonitorStatus;
            PacketsSent = copy.PacketsSent;
            Enabled = copy.Enabled;
            PingInfos = null;
        }
        [Key]
        [DatabaseGeneratedAttribute(DatabaseGeneratedOption.Identity)]
        public int ID { get; set; }

        private DateTime _dateStarted = DateTime.UtcNow;
        private int _roundTripTimeMinimum = 999;
        private StatusObj _monitorStatus = new StatusObj();
        private List<PingInfo> _pingInfos = new List<PingInfo>();

        public int DataSetID { get; set; }
        public string Status { get => _monitorStatus.Message; set => _monitorStatus.Message = value; }
        public int DestinationUnreachable { get; set; }

        public int MonitorIPID { get; set; }
        public int Timeout { get; set; }
        public int TimeOuts { get; set; }

        public string Address { get; set; }

        public string UserID { get; set; }

        public string EndPointType { get; set; }
        public int PacketsRecieved { get; set; }
        public int PacketsLost { get; set; }
        public float PacketsLostPercentage { get; set; }

        public int RoundTripTimeMaximum { get; set; }
        public float RoundTripTimeAverage { get; set; }

        public int RoundTripTimeTotal { get; set; }

        public int PacketsSent { get; internal set; }

        public bool Enabled { get; set; }

        public DateTime DateStarted
        {
            get
            {
                return DateTime.SpecifyKind(_dateStarted, DateTimeKind.Utc); ;
            }
            set { _dateStarted = value; }
        }
        public int RoundTripTimeMinimum { get => _roundTripTimeMinimum; set => _roundTripTimeMinimum = value; }
        public StatusObj MonitorStatus { get => _monitorStatus; set => _monitorStatus = value; }
        public List<PingInfo> PingInfos { get => _pingInfos; set => _pingInfos = value; }


    }
}


