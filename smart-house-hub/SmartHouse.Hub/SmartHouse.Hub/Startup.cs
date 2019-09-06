using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SmartHouse.Entities;
using SmartHouse.Hub.Hubs;
using SmartHouse.Hub.Model;
using SmartHub.Dal.Context;
using SmartHub.Dal.Interfaces;
using SmartHub.Dal.Repositories;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace SmartHouse.Hub
{
    public class Startup
    {
        public IConfigurationRoot Configuration { get; set; }

        public Startup(IHostingEnvironment env)
        {
            Configuration = new ConfigurationBuilder()
            .SetBasePath(env.ContentRootPath)
            .AddJsonFile("Configs/urlData.json", optional: true, reloadOnChange: true)
            .AddJsonFile("Configs/dbData.json", optional: true, reloadOnChange: true)
            .Build();
        }

        public void ConfigureServices(IServiceCollection services)
        {
            services.Configure<UrlDataConfiguration>((settings) =>
               Configuration.GetSection("UrlConfiguration").Bind(settings));
            var databaseConnection = Configuration.GetSection("DbConfiguration:MongoConnection").Value;
            var databaseName = Configuration.GetSection("DbConfiguration:DbName").Value;
            services.AddSingleton<IRepository<Device>>(s => new Repository<Device>(new Database(databaseConnection, databaseName)));
            services.AddSingleton<IRepository<Sensor>>(s => new Repository<Sensor>(new Database(databaseConnection, databaseName)));
            services.AddSingleton<IRepository<TelemetryDynamic>>(s => new Repository<TelemetryDynamic>(new Database(databaseConnection, databaseName)));
            services.AddSingleton<IList<TelemetryDynamic>, List<TelemetryDynamic>>();
            services.AddSingleton<ConcurrentDictionary<string, HouseSlaveSensorClient>>();
            services.AddSingleton<ConcurrentDictionary<string, HouseSlaveDeviceClient>>();
            services.AddSignalR();
        }

        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            app.UseSignalR(routes =>
            {
                routes.MapHub<SmartHouseHub>("/smarthouse", options => { options.ApplicationMaxBufferSize = 3000 * 1024; });
            });

        }
    }
}
