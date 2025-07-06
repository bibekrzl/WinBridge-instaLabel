using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using PrintServer.Core.Interfaces;
using PrintServer.Core.Printers;
using PrintServer.Core.Services;
using PrintServer.Tray.Forms;

namespace PrintServer.Tray
{
    internal static class Program
    {
        [STAThread]
        static void Main()
        {
            Application.SetHighDpiMode(HighDpiMode.SystemAware);
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            var host = CreateHostBuilder().Build();
            var mainForm = host.Services.GetRequiredService<MainForm>();
            
            Application.Run(mainForm);
        }

        static IHostBuilder CreateHostBuilder() =>
            Host.CreateDefaultBuilder()
                .ConfigureServices((context, services) =>
                {
                    // Register printer drivers
                    services.AddScoped<MunbynPrinterDriver>();
                    services.AddScoped<EpsonPrinterDriver>();

                    // Register printer drivers as IPrinterDriver
                    services.AddScoped<IPrinterDriver, MunbynPrinterDriver>();
                    services.AddScoped<IPrinterDriver, EpsonPrinterDriver>();

                    // Register print service
                    services.AddScoped<IPrintService, PrintService>();

                    // Register main form
                    services.AddTransient<MainForm>();

                    // Configure logging
                    services.AddLogging(builder =>
                    {
                        builder.AddConsole();
                        builder.AddDebug();
                    });
                });
    }
} 