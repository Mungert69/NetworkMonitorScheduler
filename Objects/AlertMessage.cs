using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace NetworkMonitor.Objects
{
    public class AlertMessage
    {
        private UserInfo _userInfo;
        private  bool _verifyLink;
        public UserInfo UserInfo
        {
            get { return _userInfo; }
            set { _userInfo = value; }
        }
        private List<int> _monitorPingInfoIDs=new List<int>();
        public string Message { get; set; }
        public string EmailTo { get { return _userInfo.Email; } }
        public string UserID { get { return _userInfo.UserID; } }

        public string Name { get { return _userInfo.Name; } }

        public string Subject { get; set; }

        public bool SendTrustPilot { get; set; }

        public bool dontSend { get; set; }
        public List<int> MonitorPingInfoIDs { get => _monitorPingInfoIDs; }
        public bool VerifyLink { get => _verifyLink; set => _verifyLink = value; }
    }
}