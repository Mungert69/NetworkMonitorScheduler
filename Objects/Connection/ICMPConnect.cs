using NetworkMonitor.Objects;
using System;
using System.Net.NetworkInformation;
using System.Threading;
using System.Threading.Tasks;

namespace NetworkMonitor.Connection
{
    public class ICMPConnect : NetConnect
    {
        public ICMPConnect(MonitorPingInfo pingInfo, PingParams pingParams)
        {
            MonitorPingInfo = pingInfo;
            PingParams = pingParams;
        }


        public override async Task connect()
        {
             AutoResetEvent waiter = new AutoResetEvent(false);

            try
            {
                   MonitorPingInfo.PacketsSent++;

                string who = MonitorPingInfo.Address;
               
                Ping pingSender = new Ping();

                // When the PingCompleted event is raised,
                // the PingCompletedCallback method is called.
                pingSender.PingCompleted += new PingCompletedEventHandler(PingCompletedCallback);

                // Create a buffer of x bytes of data to be transmitted.

                byte[] buffer = new byte[PingParams.BufferLength];
                new Random().NextBytes(buffer);


                // Wait 5 seconds for a reply.
                Timeout = MonitorPingInfo.Timeout;

                // Set options for transmission:
                // The data can go through 64 gateways or routers
                // before it is destroyed, and the data packet
                // cannot be fragmented.
                PingOptions options = new PingOptions(64, true);

                // Send the ping asynchronously.
                // Use the waiter as the user token.
                // When the callback completes, it can wake up this thread.

             pingSender.SendAsync(who, Timeout, buffer, options, waiter);
    
            }
            catch (Exception ex)
            {
                 ProcessException(MonitorPingInfo, MonitorPingInfo.EndPointType, ex.Message.ToString());
                
            }
            finally
            {
              
            }

        }

        private void PingCompletedCallback(object sender, PingCompletedEventArgs e)
        {
            // If the operation was canceled, display a message to the user.
            if (e.Cancelled)
            {

                ProcessException(MonitorPingInfo, MonitorPingInfo.EndPointType, "Ping Canceled");           
                ((AutoResetEvent)e.UserState).Set();
            }

            // If an error occurred, display the exception to the user.
            if (e.Error != null)
            {
                ProcessException(MonitorPingInfo, MonitorPingInfo.EndPointType, e.Error.ToString());
                 // Let the main thread resume.
                ((AutoResetEvent)e.UserState).Set();
            }

            PingReply reply = e.Reply;
            ProcessStatus(reply);
            // Let the main thread resume.
            ((AutoResetEvent)e.UserState).Set();
        }

        private void ProcessStatus(PingReply reply)
        {

            PingInfo pingInfo = new PingInfo();
            RoundTrip = -1;
            pingInfo.DateSent = DateTime.UtcNow;
            if (reply == null)
            {
                pingInfo.Status = "Malformed Ping Request";
                MonitorPingInfo.MonitorStatus.Message = pingInfo.Status.ToString();
                return;
            }
            else
            {
                pingInfo.Status = reply.Status.ToString();
            }


            MonitorPingInfo.MonitorStatus.Message = reply.Status.ToString();
            if (reply.Status == IPStatus.Success)
            {
                MonitorPingInfo.PacketsRecieved++;
                RoundTrip = (int)reply.RoundtripTime;
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
            if (reply.Status == IPStatus.TimedOut)
            {
                MonitorPingInfo.TimeOuts++;
            }

            if (reply.Status == IPStatus.DestinationHostUnreachable)
            {
                MonitorPingInfo.DestinationUnreachable++;
            }

            MonitorPingInfo.PingInfos.Add(pingInfo);

        }
    }
}
