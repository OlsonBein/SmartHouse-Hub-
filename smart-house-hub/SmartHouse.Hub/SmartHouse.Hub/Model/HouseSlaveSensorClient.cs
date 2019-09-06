using SmartHouse.Entities;

namespace SmartHouse.Hub.Model
{
    public class HouseSlaveSensorClient
    {
        public string ConnectionId { get; set; }

        public Sensor Slave { get; set; }
    }
}
