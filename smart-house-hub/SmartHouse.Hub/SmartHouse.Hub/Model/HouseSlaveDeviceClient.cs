using SmartHouse.Entities;

namespace SmartHouse.Hub.Model
{
    public class HouseSlaveDeviceClient
    {
        public string ConnectionId { get; set; }

        public Device Slave { get; set; }
    }
}
