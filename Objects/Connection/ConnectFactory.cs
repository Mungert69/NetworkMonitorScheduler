
using NetworkMonitor.Objects;
using System.Threading.Tasks;
using System.Collections.Generic;


namespace NetworkMonitor.Connection
{
    public class ConnectFactory
    {


        public static Task GetNetConnect(MonitorPingInfo pingInfo, PingParams pingParams)
        {
            
            return GetNetConnectObj(pingInfo,pingParams).connect();


        }

        //Method to get a list of NetConnect objects passing in a List of MonitorPingInfo objects and pingParams
        public static List<NetConnect> GetNetConnectList(List<MonitorPingInfo> pingInfos, PingParams pingParams)
        {
            List<NetConnect> netConnects = new List<NetConnect>();
            foreach (MonitorPingInfo pingInfo in pingInfos)
            {
                netConnects.Add(GetNetConnectObj(pingInfo, pingParams));
            }
            return netConnects;
        }

        //Method to get the NetConnect object based on what who starts with http or icmp
        public static NetConnect GetNetConnectObj(MonitorPingInfo pingInfo, PingParams pingParams)
        {
           string type = pingInfo.EndPointType;
            NetConnect netConnect;
            if (type.StartsWith("http"))
            {

                netConnect= new HTTPConnect(pingInfo, pingParams);
            }
            else{
                netConnect=new ICMPConnect(pingInfo, pingParams);
            }
            return netConnect;
        }   


    }
}
