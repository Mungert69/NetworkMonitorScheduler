using System;
using System.ComponentModel.DataAnnotations;
using System.Collections.Generic;

namespace NetworkMonitor.Objects
{
    public class UserInfo
    {
        [Key]
        public string UserID { get; set; }

        private DateTime _dateCreated;
        public DateTime DateCreated
        {
            get
            {
                return DateTime.SpecifyKind(_dateCreated, DateTimeKind.Utc); ;
            }
            set { _dateCreated = value; }
        }

        public int HostLimit { get; set; }

        public bool DisableEmail { get; set; }

        public string Status { get; set; }

        public string Name { get; set; }
        public string Given_name { get; set; }
        public string Family_name { get; set; }
        public string Nickname { get; set; }
        public string Sub { get; set; }

        public bool Enabled { get; set; } = true;

        public string AccountType { get; set; } = "Free";

        public string Email { get; set; }

        public bool Email_verified { get; set; }

        public string Picture { get; set; }
        private DateTime _updated_at;
        public DateTime Updated_at
        {
            get
            {
                return DateTime.SpecifyKind(_updated_at, DateTimeKind.Utc); ;
            }
            set { _updated_at = value; }
        }
        private DateTime _lastLoginDate;
        public DateTime LastLoginDate
        {
            get
            {
                return DateTime.SpecifyKind(_lastLoginDate, DateTimeKind.Utc); ;
            }
            set { _lastLoginDate = value; }
        }

        private List<MonitorIP> _monitorIPs = new List<MonitorIP>();

        public List<MonitorIP> MonitorIPs { get => _monitorIPs; set => _monitorIPs = value; }


    }
}
