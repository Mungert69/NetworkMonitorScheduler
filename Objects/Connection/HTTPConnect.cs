using NetworkMonitor.Objects;
using System;
using System.Net;
using System.Threading.Tasks;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Diagnostics;

namespace NetworkMonitor.Connection
{
    public class HTTPConnect : NetConnect
    {
        HttpClient _client;
        public HTTPConnect(MonitorPingInfo pingInfo, PingParams pingParams)
        {
            MonitorPingInfo = pingInfo;
            PingParams = pingParams;
            try
            {
                _client = new HttpClient();
                string who = MonitorPingInfo.Address;
                _client.Timeout = TimeSpan.FromMilliseconds(MonitorPingInfo.Timeout);
                _client.BaseAddress = new Uri(who);
            }
            catch 
            {
                MonitorPingInfo.PacketsSent++;
                // Ignore expections on setup
            }
        }
        private async Task GetPage(HttpClient client)
        {
            
            Stopwatch timer = new Stopwatch();
              MonitorPingInfo.PacketsSent++;
            timer.Start();
            HttpResponseMessage response = await client.GetAsync("");
            timer.Stop();
            TimeSpan timeTaken = timer.Elapsed;

            ProcessStatus(response, timeTaken);
        }
        public override async Task connect()
        {
             try
            {
                string who = MonitorPingInfo.Address;
                //  only update if these parameters are different
                TimeSpan timeOut = TimeSpan.FromMilliseconds(MonitorPingInfo.Timeout);
                if (timeOut != _client.Timeout)
                {
                    _client.Timeout = timeOut;
                }
                Uri baseAddress = new Uri(who);
                if (baseAddress != _client.BaseAddress)
                {
                    _client.BaseAddress = baseAddress;
                }
                await GetPage(_client);
                
            }
            catch (Exception e)
            {
                ProcessException(MonitorPingInfo, MonitorPingInfo.EndPointType, e.Message.ToString());
            }
            finally
            {
              
            }
        }


        private void ProcessStatus(HttpResponseMessage reply, TimeSpan timeTaken)
        {
            
            PingInfo pingInfo = new PingInfo();
            RoundTrip = -20;
            pingInfo.DateSent = DateTime.UtcNow;
            pingInfo.Status = reply.StatusCode.ToString();
            MonitorPingInfo.MonitorStatus.Message = reply.StatusCode.ToString();
            if (reply.StatusCode == HttpStatusCode.OK)
            {
                MonitorPingInfo.PacketsRecieved++;
                RoundTrip = Convert.ToInt32(timeTaken.TotalMilliseconds);
                pingInfo.RoundTripTime = RoundTrip;
                MonitorPingInfo.MonitorStatus.IsUp = true;
                MonitorPingInfo.MonitorStatus.DownCount = 0;

                if (MonitorPingInfo.RoundTripTimeMaximum < RoundTrip)
                {
                    MonitorPingInfo.RoundTripTimeMaximum = RoundTrip;
                }
                if (MonitorPingInfo.RoundTripTimeMinimum > RoundTrip)
                {
                    MonitorPingInfo.RoundTripTimeMinimum = RoundTrip;
                }
                MonitorPingInfo.RoundTripTimeTotal += RoundTrip;
                MonitorPingInfo.RoundTripTimeAverage = MonitorPingInfo.RoundTripTimeTotal / (float)MonitorPingInfo.PacketsRecieved;
            }
            else
            {
                MonitorPingInfo.PacketsLost++;
                MonitorPingInfo.MonitorStatus.IsUp = false;
                MonitorPingInfo.MonitorStatus.DownCount++;
                MonitorPingInfo.MonitorStatus.EventTime = pingInfo.DateSent;
                pingInfo.RoundTripTime = -1;
            }
            //Note we add one to packets sent because it is not updated till the finaly clause.
            MonitorPingInfo.PacketsLostPercentage = MonitorPingInfo.PacketsLost * (float)100 / (MonitorPingInfo.PacketsSent);

            MonitorPingInfo.PingInfos.Add(pingInfo);
            
        }
    }
}
