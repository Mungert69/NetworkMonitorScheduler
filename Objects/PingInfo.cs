using NetworkMonitor.Objects;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore.Infrastructure;
namespace NetworkMonitor.Objects
{
    public class PingInfo
    {
        public PingInfo()
        {
        }

        private PingInfo(ILazyLoader lazyLoader)
        {
            _lazyLoader = lazyLoader;
        }

        private ILazyLoader _lazyLoader { get; set; }
        private MonitorPingInfo _monitorPingInfo;

        private DateTime _dateSent;

        [Key]
        [DatabaseGeneratedAttribute(DatabaseGeneratedOption.Identity)]
        public int ID { get; set; }
        public DateTime DateSent
        {
            get
            {
                return DateTime.SpecifyKind(_dateSent, DateTimeKind.Utc);;
            }
            set { _dateSent = value; }
        }

        public string Status { get; set; }

        public int? RoundTripTime { get; set; }

        public int MonitorPingInfoID { get; set; }
        public virtual MonitorPingInfo MonitorPingInfo
        {
            get => _lazyLoader.Load(this, ref _monitorPingInfo);
            set => _monitorPingInfo = value;
        }
    }
}
