using SmartHouse.Entities;
using System.Collections.Generic;

namespace SmartHouse.Hub.Model
{
    public class UrlDataConfiguration
    {
        public int PortToListen { get; set; }

        public string HubUrl { get; set; }

        public string GetUrlForInit { get; set; }

        public string GetUrlForListen { get; set; }

        public Dictionary<SlaveType, string> MethodUrls { get; set; }
    }
}
