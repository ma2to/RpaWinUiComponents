// App.xaml.cs - KOMPLETNÁ OPRAVA DI konfigurácie
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Xaml;
using RpaWinUiComponents.AdvancedWinUiDataGrid.Configuration;
using System;
using System.Threading.Tasks;

namespace RpaWinUiComponents.Demo
{
    /// <summary>
    /// Demo aplikácia pre testovanie RpaWinUiComponents balíka - OPRAVENÁ VERZIA
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
        /// Inicializuje služby a DI kontajner pre demo aplikáciu - KOMPLETNÁ OPRAVA
        /// </summary>
        private void InitializeServices()
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("🔧 Inicializuje sa DI kontajner...");

                // Vytvorenie host builderu s robustnou konfiguráciou
                var hostBuilder = Host.CreateDefaultBuilder()
                    .ConfigureLogging(logging =>
                    {
                        logging.ClearProviders();
                        logging.AddConsole();
                        logging.AddDebug();
                        logging.SetMinimumLevel(LogLevel.Information);
                    })
                    .ConfigureServices((context, services) =>
                    {
                        try
                        {
                            System.Diagnostics.Debug.WriteLine("📦 Registrujú sa služby...");

                            // KĽÚČOVÁ OPRAVA: Registrácia služieb pre AdvancedWinUiDataGrid
                            services.AddAdvancedWinUiDataGrid();

                            // Registrácia demo aplikácie služieb
                            services.AddSingleton<MainWindow>();

                            // Dodatočné služby pre demo (voliteľné)
                            services.AddTransient<IDemoDataService, DemoDataService>();

                            System.Diagnostics.Debug.WriteLine("✅ Služby úspešne zaregistrované");
                        }
                        catch (Exception ex)
                        {
                            System.Diagnostics.Debug.WriteLine($"❌ Chyba pri registrácii služieb: {ex.Message}");
                            throw;
                        }
                    });

                // Build host
                _host = hostBuilder.Build();

                // KĽÚČOVÁ OPRAVA: Konfigurácia RpaWinUiComponents s DI kontajnerom
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
                System.Diagnostics.Debug.WriteLine($"❌ Stack trace: {ex.StackTrace}");

                // Fallback - vytvor základnú konfiguráciu
                CreateFallbackConfiguration();
            }
        }

        /// <summary>
        /// Vytvorí zjednodušenú konfiguráciu ak zlyhá hlavná inicializácia - VYLEPŠENÝ FALLBACK
        /// </summary>
        private void CreateFallbackConfiguration()
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("🔄 Vytváram fallback konfiguráciu...");

                var services = new ServiceCollection();

                // Základné logging
                services.AddLogging(builder =>
                {
                    builder.AddDebug();
                    builder.AddConsole();
                    builder.SetMinimumLevel(LogLevel.Information);
                });

                // KĽÚČOVÁ OPRAVA: Registrácia AdvancedWinUiDataGrid služieb
                services.AddAdvancedWinUiDataGrid();

                // Demo služby
                services.AddSingleton<MainWindow>();
                services.AddTransient<IDemoDataService, DemoDataService>();

                var serviceProvider = services.BuildServiceProvider();

                // Konfigurácia komponentu
                RpaWinUiComponents.AdvancedWinUiDataGrid.AdvancedWinUiDataGridControl
                    .Configuration.ConfigureServices(serviceProvider);

                // Vytvorenie pseudo-host pre fallback
                _host = new FallbackHost(serviceProvider);

                System.Diagnostics.Debug.WriteLine("✅ Demo App: Fallback configuration created");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Demo App: Even fallback configuration failed: {ex.Message}");

                // Úplný fallback - len basic service provider
                var basicServices = new ServiceCollection();
                basicServices.AddSingleton<MainWindow>();
                var basicProvider = basicServices.BuildServiceProvider();
                _host = new FallbackHost(basicProvider);
            }
        }

        protected override void OnLaunched(Microsoft.UI.Xaml.LaunchActivatedEventArgs args)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("🚀 Spúšťa sa aplikácia...");

                // Štart host služieb (ak nie je fallback)
                if (_host != null && _host is not FallbackHost)
                {
                    _host.StartAsync();
                }

                // Vytvorenie hlavného okna
                if (_host != null)
                {
                    try
                    {
                        // Získaj MainWindow z DI kontajnera
                        m_window = _host.Services.GetService<MainWindow>();
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"⚠️ Chyba pri získavaní MainWindow z DI: {ex.Message}");
                        m_window = null;
                    }
                }

                // Fallback ak DI zlyhá
                if (m_window == null)
                {
                    System.Diagnostics.Debug.WriteLine("🔄 Vytváram MainWindow bez DI...");
                    m_window = new MainWindow();
                }

                m_window.Activate();

                System.Diagnostics.Debug.WriteLine("✅ Demo App: Application launched successfully");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Demo App: Error during launch: {ex.Message}");

                // Emergency fallback
                try
                {
                    m_window = new MainWindow();
                    m_window.Activate();
                    System.Diagnostics.Debug.WriteLine("✅ Emergency fallback window created");
                }
                catch (Exception emergencyEx)
                {
                    System.Diagnostics.Debug.WriteLine($"❌ Emergency fallback failed: {emergencyEx.Message}");
                }
            }
        }

        /// <summary>
        /// Vyčistenie zdrojov pri ukončení aplikácie
        /// </summary>
        public async void OnApplicationExit()
        {
            try
            {
                if (_host != null && _host is not FallbackHost)
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
    /// Demo služba pre testovanie DI - OPRAVENÁ
    /// </summary>
    public interface IDemoDataService
    {
        string GetDemoInfo();
    }

    public class DemoDataService : IDemoDataService
    {
        private readonly ILogger<DemoDataService>? _logger;

        public DemoDataService(ILogger<DemoDataService>? logger = null)
        {
            _logger = logger;
        }

        public string GetDemoInfo()
        {
            _logger?.LogInformation("Demo data service called");
            return "Demo service is working!";
        }
    }

    /// <summary>
    /// Jednoduchý fallback host pre prípad zlyhania hlavného host builderu
    /// </summary>
    public class FallbackHost : IHost
    {
        public IServiceProvider Services { get; }

        public FallbackHost(IServiceProvider serviceProvider)
        {
            Services = serviceProvider;
        }

        public void Dispose() { }
        public Task StartAsync(System.Threading.CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task StopAsync(System.Threading.CancellationToken cancellationToken = default) => Task.CompletedTask;
    }
}