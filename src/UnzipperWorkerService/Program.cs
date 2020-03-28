using System.Runtime.InteropServices;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace UnzipperWorkerService
{
    public class Program
    {
        public static void Main(string[] args)
        {
            CreateHostBuilder(args).Build().Run();
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureServices((hostContext, services) =>
                {
                    var isWindows = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
                    var appConfigSectionName = isWindows ? "WindowsAppConfig" : "AppConfig";

                    services
                        .Configure<AppConfig>(hostContext.Configuration.GetSection(appConfigSectionName))
                        .AddHostedService<UnzipperWorkerService>();
                });
    }
}
