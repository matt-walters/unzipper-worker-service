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
                    services
                        .Configure<AppConfig>(hostContext.Configuration.GetSection("AppConfig"))
                        .AddHostedService<UnzipperWorkerService>();
                });
    }
}
