// OPRAVA RpaWinUiComponents.Demo/App.xaml.cs - CS1061 fix
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Xaml;
using System;
using System.Threading.Tasks;

// ✅ KĽÚČOVÁ OPRAVA CS1061: Import PUBLIC API z PROJECT REFERENCE
using RpaWinUiComponents.AdvancedWinUiDataGrid;

namespace RpaWinUiComponents.Demo
{
    /// <summary>
    /// Demo aplikácia pre testovanie RpaWinUiComponents balíka - OPRAVENÁ VERZIA pre PROJECT REFERENCE
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
        /// OPRAVA CS1061: Inicializuje služby s PROJECT REFERENCE
        /// </summary>
        private void InitializeServices()
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("🔧 Inicializuje sa DI kontajner s PROJECT REFERENCE...");

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
                            System.Diagnostics.Debug.WriteLine("📦 Registrujú sa služby s PROJECT REFERENCE...");

                            // ✅ KĽÚČOVÁ OPRAVA CS1061: Extension metóda z PROJECT REFERENCE
                            try
                            {
                                services.AddAdvancedWinUiDataGrid();
                                System.Diagnostics.Debug.WriteLine("✅ AddAdvancedWinUiDataGrid() úspešne zavolaná cez PROJECT REFERENCE");
                            }
                            catch (Exception ex)
                            {
                                System.Diagnostics.Debug.WriteLine($"❌ Chyba pri AddAdvancedWinUiDataGrid(): {ex.Message}");
                                // Fallback - manuálna registrácia základných služieb
                                services.AddLogging();
                                System.Diagnostics.Debug.WriteLine("⚠️ Fallback registrácia služieb");
                            }

                            // Registrácia demo aplikácie služieb
                            services.AddSingleton<MainWindow>();

                            // Dodatočné služby pre demo (voliteľné)
                            services.AddTransient<IDemoDataService, DemoDataService>();

                            System.Diagnostics.Debug.WriteLine("✅ Služby úspešne zaregistrované cez PROJECT REFERENCE");
                        }
                        catch (Exception ex)
                        {
                            System.Diagnostics.Debug.WriteLine($"❌ Chyba pri registrácii služieb: {ex.Message}");
                            throw;
                        }
                    });

                // Build host
                _host = hostBuilder.Build();

                // ✅ KĽÚČOVÁ OPRAVA: Konfigurácia RpaWinUiComponents s DI kontajnerom z PROJECT REFERENCE
                try
                {
                    AdvancedWinUiDataGridControl.Configuration.ConfigureServices(_host.Services);
                    System.Diagnostics.Debug.WriteLine("✅ AdvancedWinUiDataGridControl.Configuration.ConfigureServices() úspešne");
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"⚠️ Configuration.ConfigureServices() chyba: {ex.Message}");
                    // Pokračujeme bez konfigurácie - komponent bude fungovať v základnom režime
                }

                // Konfigurácia loggingu
                try
                {
                    var loggerFactory = _host.Services.GetRequiredService<ILoggerFactory>();
                    AdvancedWinUiDataGridControl.Configuration.ConfigureLogging(loggerFactory);
                    System.Diagnostics.Debug.WriteLine("✅ Configuration.ConfigureLogging() úspešne");
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"⚠️ Configuration.ConfigureLogging() chyba: {ex.Message}");
                }

                // Zapnutie debug loggu pre vývoj
                try
                {
                    AdvancedWinUiDataGridControl.Configuration.SetDebugLogging(true);
                    System.Diagnostics.Debug.WriteLine("✅ Debug logging zapnuté");
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"⚠️ SetDebugLogging() chyba: {ex.Message}");
                }

                System.Diagnostics.Debug.WriteLine("✅ Demo App: Services initialized successfully s PROJECT REFERENCE");
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
        /// Vytvorí zjednodušenú konfiguráciu ak zlyhá hlavná inicializácia - FALLBACK
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

                // ✅ OPRAVA: Pokus o registráciu AdvancedWinUiDataGrid služieb
                try
                {
                    services.AddAdvancedWinUiDataGrid();
                    System.Diagnostics.Debug.WriteLine("✅ Fallback: AddAdvancedWinUiDataGrid() úspešné");
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"⚠️ Fallback: AddAdvancedWinUiDataGrid() zlyhalo: {ex.Message}");
                    // Pokračujeme bez registrácie - komponent vytvorí vlastné služby
                }

                // Demo služby
                services.AddSingleton<MainWindow>();
                services.AddTransient<IDemoDataService, DemoDataService>();

                var serviceProvider = services.BuildServiceProvider();

                // Pokus o konfiguráciu komponentu
                try
                {
                    AdvancedWinUiDataGridControl.Configuration.ConfigureServices(serviceProvider);
                    System.Diagnostics.Debug.WriteLine("✅ Fallback: Configuration.ConfigureServices() úspešné");
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"⚠️ Fallback: Configuration.ConfigureServices() zlyhalo: {ex.Message}");
                }

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
                System.Diagnostics.Debug.WriteLine("🚀 Spúšťa sa aplikácia s PROJECT REFERENCE...");

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

                System.Diagnostics.Debug.WriteLine("✅ Demo App: Application launched successfully s PROJECT REFERENCE");
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
    /// Demo služba pre testovanie DI
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