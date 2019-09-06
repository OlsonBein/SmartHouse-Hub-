using Microsoft.Extensions.Options;
using SmartHouse.Hub.Model;
using SmartHouse.Udp.Hub;

namespace SmartHouse.Hub
{
    public class UdpListener
    {
        private readonly UrlDataConfiguration _config;

        private readonly HubUdp _hubUdp;
        public UdpListener(IOptions<UrlDataConfiguration> config)
        {
            _config = config.Value;
            _hubUdp = new HubUdp(_config.HubUrl);
        }

        public void Run()
        {
            _hubUdp.SendContract(_config.PortToListen);
        }
    }
}
