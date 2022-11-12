using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace NetworkMonitor.Objects
{
    public class StatusObj
    {
        [Key]
        [DatabaseGeneratedAttribute(DatabaseGeneratedOption.Identity)]
        private int iD;
        private DateTime eventTime;
        private string message;
        private bool isUp;
        private int downCount;
        private bool alertFlag;
        private bool alertSent;

        public bool IsUp { get => isUp; set => isUp = value; }
        public int DownCount { get => downCount; set => downCount = value; }
        public bool AlertFlag { get => alertFlag; set => alertFlag = value; }
        public bool AlertSent { get => alertSent; set => alertSent = value; }
        public string Message { get => message; set => message = value; }
        [Key]
        public int ID { get => iD; set => iD = value; }

         public DateTime EventTime
        {
            get
            {
                return DateTime.SpecifyKind(eventTime, DateTimeKind.Utc);;
            }
            set { eventTime = value; }
        }
       
        public int MonitorPingInfoID{get;set;}
        public virtual MonitorPingInfo MonitorPingInfo{get;set;}
    }
}
