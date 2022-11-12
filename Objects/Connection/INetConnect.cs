using System.Net.NetworkInformation;
using System.Threading.Tasks;

namespace NetworkMonitor.Connection
{
    public interface INetConnect
    {
        int RoundTrip { get; set; }
        Task connect();
    }
}