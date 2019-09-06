using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using SmartHouse.Hub.Model;
using System.Threading.Tasks;

namespace SmartHouse.Hub
{
    public class Program
    {
        public static void Main(string[] args)
        {
            IWebHost host = CreateWebHostBuilder(args).Build();
            Task.Run(() =>
            {
                using (IServiceScope scope = host.Services.CreateScope())
                {
                    UdpListener listener = new UdpListener((IOptions<UrlDataConfiguration>)scope.
                        ServiceProvider.GetService(typeof(IOptions<UrlDataConfiguration>)));
                    listener.Run();
                }

            });            
            host.Run();

        }

        public static IWebHostBuilder CreateWebHostBuilder(string[] args)
        {
            return WebHost.CreateDefaultBuilder(args)
                .UseStartup<Startup>();
        }
    }
}
