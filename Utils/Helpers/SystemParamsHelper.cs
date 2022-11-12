using NetworkMonitor.Objects;
using Microsoft.Extensions.Configuration;
using System;
using System.Linq;
using System.Collections.Generic;
namespace NetworkMonitor.Utils.Helpers
{
    public class SystemParamsHelper
    {

        static public SystemParams getSystemParams(IConfiguration  config, string publicIPAddress){
            SystemParams systemParams = new SystemParams();
            systemParams.SystemUrls = config.GetSection("SystemUrls").Get<List<SystemUrl>>();
            systemParams.SystemPassword = config.GetValue<string>("SystemPassword");
            systemParams.EmailEncryptKey = config.GetValue<string>("EmailEncryptKey");
            systemParams.SystemEmail = config.GetValue<string>("SystemEmail");
            systemParams.SystemUser = config.GetValue<string>("SystemUser");
            systemParams.MailServer = config.GetValue<string>("MailServer");
            systemParams.MailServerPort = config.GetValue<int>("MailServerPort");
            systemParams.MailServerUseSSL = config.GetValue<bool>("MailServerUseSSL");
            systemParams.TrustPilotReviewEmail = config.GetValue<string>("TrustPilotReviewEmail");
            systemParams.SendTrustPilot = config.GetValue<bool>("SendTrustPilot");
            systemParams.PublicIPAddress=publicIPAddress;
            systemParams.ManagementToken=config.GetValue<string>("Auth0:ManagementToken");
            systemParams.Domain=config.GetValue<string>("Auth0:Domain");

            if (systemParams.SystemUrls != null)
            {
                systemParams.ThisSystemUrl = systemParams.SystemUrls.FirstOrDefault(x => x.IPAddress == publicIPAddress);
                if (systemParams.SystemUrls.Count > 1)
                {
                    systemParams.IsSingleSystem = false;
                }
                else
                {
                    systemParams.IsSingleSystem = true;
                }
            }
            if (systemParams.ThisSystemUrl == null)
            {
                systemParams.ThisSystemUrl = config.GetSection("LocalSystemUrl").Get<SystemUrl>();
                systemParams.IsSingleSystem = true;
            }

            return systemParams;
        }
        
    }
}