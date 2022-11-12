using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace NetworkMonitor.Objects
{
    public class SystemParams
    {
        private List<SystemUrl> _systemUrls;

        private SystemUrl _thisSystemUrl;
        private string _systemPassword;
        private bool _isSingleSystem;
        private string _emailEncryptKey;
        private string _systemUser;
        private string _mailServer;
        private int _mailServerPort;
        private bool _mailServerUseSSL;
        private string _trustPilotReviewEmail;
        private string _systemEmail; 
        private string _publicIPAddress;
        private string _managementToken;
        private string _domain;
        private bool sendTrustPilot;
        public List<SystemUrl> SystemUrls { get => _systemUrls; set => _systemUrls = value; }
        public string SystemPassword { get => _systemPassword; set => _systemPassword = value; }
        public SystemUrl ThisSystemUrl { get => _thisSystemUrl; set => _thisSystemUrl = value; }
        public bool IsSingleSystem { get => _isSingleSystem; set => _isSingleSystem = value; }
        public string EmailEncryptKey { get => _emailEncryptKey; set => _emailEncryptKey = value; }
        public string SystemUser { get => _systemUser; set => _systemUser = value; }
        public string MailServer { get => _mailServer; set => _mailServer = value; }
        public int MailServerPort { get => _mailServerPort; set => _mailServerPort = value; }
        public bool MailServerUseSSL { get => _mailServerUseSSL; set => _mailServerUseSSL = value; }
        public string TrustPilotReviewEmail { get => _trustPilotReviewEmail; set => _trustPilotReviewEmail = value; }
        public string SystemEmail { get => _systemEmail; set => _systemEmail = value; }
        public string PublicIPAddress { get => _publicIPAddress; set => _publicIPAddress = value; }
        public string ManagementToken { get => _managementToken; set => _managementToken = value; }
        public string Domain { get => _domain; set => _domain = value; }
        public bool SendTrustPilot { get => sendTrustPilot; set => sendTrustPilot = value; }
    }
}