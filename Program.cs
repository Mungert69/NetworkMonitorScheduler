using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NetworkMonitor.Data;
using System;
using System.Net;

namespace NetworkMonitor
{
    public class Program
    {
        public static void Main(string[] args)
        {
            
            IConfigurationRoot config = new ConfigurationBuilder()
        .AddJsonFile("appsettings.json", optional: false)
        .Build();
            IWebHost host = CreateWebHostBuilder(args).Build();
            using (IServiceScope scope = host.Services.CreateScope())
            {
                
            }
            host.Run();

        }

        public static IWebHostBuilder CreateWebHostBuilder(string[] args) =>


        WebHost.CreateDefaultBuilder(args).UseKestrel(options =>
    {
        //options.Listen(IPAddress.Loopback, 5000);  // http:localhost:5000
        options.Listen(IPAddress.Any, 2056);         // http:*:65123
        options.Listen(IPAddress.Any, 2057, listenOptions =>
        {
            listenOptions.UseHttps("https.pfx", "Ac£0462110");
        });
    }).UseStartup<Startup>();
    }
}
