using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using NetworkMonitor.Objects;
using System.Collections.Generic;

namespace NetworkMonitor.Utils
{
    public class ListUtils
    {

        public static T DeepCopy<T>(T item)
        {
            BinaryFormatter formatter = new BinaryFormatter();
            MemoryStream stream = new MemoryStream();
            formatter.Serialize(stream, item);
            stream.Seek(0, SeekOrigin.Begin);
            T result = (T)formatter.Deserialize(stream);
            stream.Close();
            return result;
        }

        public static void RemoveNestedMonitorPingInfos(List<MonitorPingInfo> mons){
             foreach (var mon in mons)
                {
                    foreach (var info in mon.PingInfos)
                    {
                        info.MonitorPingInfo = null;
                    }
                    mon.MonitorStatus.MonitorPingInfo=null;
                }
        }

        public static void RemoveNestedMonitorIPs(List<MonitorIP> mons){
            foreach (var mon in mons)
                {
                    mon.UserInfo.MonitorIPs = null;
                }
        }
    }
}
