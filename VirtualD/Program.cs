using JsonFlatFileDataStore;
using Microsoft.Extensions.DependencyInjection;
using VirtualD.Entities;
using VirtualD.Services;

namespace VirtualD
{
    internal static class Program
    {
        /// <summary>
        ///  The main entry point for the application.
        /// </summary>
        [STAThread]
        private static void Main()
        {
            // To customize application configuration such as set high DPI settings or default font,
            // see https://aka.ms/applicationconfiguration.
            ApplicationConfiguration.Initialize();

            var services = new ServiceCollection();

            ConfigureServices(services);

            var serviceProvider = services.BuildServiceProvider();
            var context = serviceProvider.GetRequiredService<AppContext>();
            
            Application.ApplicationExit += (_, _) => OnApplicationExit(serviceProvider);

            Application.Run(context);
        }

        private static void OnApplicationExit(ServiceProvider serviceProvider)
        {
            var workspaceService = serviceProvider.GetRequiredService<WorkspaceService>();
            workspaceService.Reset();
            
            serviceProvider.Dispose();
        }

        private static void ConfigureServices(IServiceCollection services)
        {
            services
                // .AddSingleton<IDataStore, DataStore>(_ => new DataStore("data.json", keyProperty: "Id"))
                .AddSingleton<WorkspaceService>()
                .AddSingleton<HotkeyService>()
                .AddSingleton<Form1>()
                .AddSingleton<AppContext>();
        }
    }
}