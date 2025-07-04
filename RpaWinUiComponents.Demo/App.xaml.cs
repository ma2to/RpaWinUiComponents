// App.xaml.cs - DEMO APLIKÁCIA S KOMPLETNOU DI KONFIGURÁCIOU
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Xaml;
using RpaWinUiComponents.AdvancedWinUiDataGrid.Configuration;
using System;

namespace RpaWinUiComponents.Demo
{
    /// <summary>
    /// Demo aplikácia pre testovanie RpaWinUiComponents balíka
    /// </summary>
    public partial class App : Application
    {
        private Window? m_window;
        private IHost? _host;

        public App()
        {
            this.InitializeComponent();

            // Inicializácia DI kontajnera a služieb
            InitializeServices();
        }

        /// <summary>
        /// Inicializuje služby a DI kontajner pre demo aplikáciu
        /// </summary>
        private void InitializeServices()
        {
            try
            {
                // Vytvorenie host builderu
                var hostBuilder = Host.CreateDefaultBuilder()
                    .ConfigureLogging(logging =>
                    {
                        logging.ClearProviders();
                        logging.AddConsole();
                        logging.AddDebug();
                        logging.SetMinimumLevel(LogLevel.Debug);
                    })
                    .ConfigureServices((context, services) =>
                    {
                        // Registrácia služieb pre AdvancedWinUiDataGrid
                        services.AddAdvancedWinUiDataGrid();

                        // Registrácia demo aplikácie služieb
                        services.AddSingleton<MainWindow>();

                        // Dodatočné služby pre demo
                        services.AddTransient<IDemoDataService, DemoDataService>();
                    });

                // Build host
                _host = hostBuilder.Build();

                // Konfigurácia RpaWinUiComponents s DI kontajnerom
                RpaWinUiComponents.AdvancedWinUiDataGrid.AdvancedWinUiDataGridControl
                    .Configuration.ConfigureServices(_host.Services);

                // Konfigurácia loggingu
                var loggerFactory = _host.Services.GetRequiredService<ILoggerFactory>();
                RpaWinUiComponents.AdvancedWinUiDataGrid.AdvancedWinUiDataGridControl
                    .Configuration.ConfigureLogging(loggerFactory);

                // Zapnutie debug loggu pre vývoj
                RpaWinUiComponents.AdvancedWinUiDataGrid.AdvancedWinUiDataGridControl
                    .Configuration.SetDebugLogging(true);

                System.Diagnostics.Debug.WriteLine("✅ Demo App: Services initialized successfully");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Demo App: Error initializing services: {ex.Message}");
                // Fallback - vytvor základnú konfiguráciu
                CreateFallbackConfiguration();
            }
        }

        /// <summary>
        /// Vytvorí zjednodušenú konfiguráciu ak zlyhá hlavná inicializácia
        /// </summary>
        private void CreateFallbackConfiguration()
        {
            try
            {
                var services = new ServiceCollection();

                // Základné logging
                services.AddLogging(builder =>
                {
                    builder.AddDebug();
                    builder.AddConsole();
                    builder.SetMinimumLevel(LogLevel.Information);
                });

                // Registrácia AdvancedWinUiDataGrid služieb
                services.AddAdvancedWinUiDataGrid();

                var serviceProvider = services.BuildServiceProvider();

                // Konfigurácia komponentu
                RpaWinUiComponents.AdvancedWinUiDataGrid.AdvancedWinUiDataGridControl
                    .Configuration.ConfigureServices(serviceProvider);

                System.Diagnostics.Debug.WriteLine("✅ Demo App: Fallback configuration created");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Demo App: Even fallback configuration failed: {ex.Message}");
            }
        }

        protected override void OnLaunched(Microsoft.UI.Xaml.LaunchActivatedEventArgs args)
        {
            try
            {
                // Štart host služieb
                _host?.StartAsync();

                // Vytvorenie hlavného okna
                if (_host != null)
                {
                    // Získaj MainWindow z DI kontajnera
                    m_window = _host.Services.GetService<MainWindow>() ?? new MainWindow();
                }
                else
                {
                    // Fallback
                    m_window = new MainWindow();
                }

                m_window.Activate();

                System.Diagnostics.Debug.WriteLine("✅ Demo App: Application launched successfully");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Demo App: Error during launch: {ex.Message}");

                // Emergency fallback
                m_window = new MainWindow();
                m_window.Activate();
            }
        }

        /// <summary>
        /// Vyčistenie zdrojov pri ukončení aplikácie
        /// </summary>
        public async void OnApplicationExit()
        {
            try
            {
                if (_host != null)
                {
                    await _host.StopAsync();
                    _host.Dispose();
                }

                System.Diagnostics.Debug.WriteLine("✅ Demo App: Application shutdown completed");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Demo App: Error during shutdown: {ex.Message}");
            }
        }
    }

    /// <summary>
    /// Demo služba pre testovanie DI
    /// </summary>
    public interface IDemoDataService
    {
        string GetDemoInfo();
    }

    public class DemoDataService : IDemoDataService
    {
        private readonly ILogger<DemoDataService> _logger;

        public DemoDataService(ILogger<DemoDataService> logger)
        {
            _logger = logger;
        }

        public string GetDemoInfo()
        {
            _logger.LogInformation("Demo data service called");
            return "Demo service is working!";
        }
    }
}