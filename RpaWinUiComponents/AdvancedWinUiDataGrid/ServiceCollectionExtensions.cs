// ServiceCollectionExtensions.cs - MODULÁRNE EXTENSION METÓDY
// SÚBOR: RpaWinUiComponents/AdvancedWinUiDataGrid/ServiceCollectionExtensions.cs (NOVÝ SÚBOR)

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;

namespace RpaWinUiComponents.AdvancedWinUiDataGrid
{
    /// <summary>
    /// MODULÁRNE EXTENSION METÓDY pre AdvancedWinUiDataGrid komponent
    /// Umožňuje použitie: services.AddAdvancedWinUiDataGrid()
    /// </summary>
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        /// HLAVNÁ EXTENSION METÓDA: Registruje všetky služby potrebné pre AdvancedWinUiDataGrid
        /// </summary>
        [System.Runtime.Versioning.SupportedOSPlatform("windows10.0.17763.0")]
        public static IServiceCollection AddAdvancedWinUiDataGrid(this IServiceCollection services, ILoggerFactory? loggerFactory = null)
        {
            if (services == null)
                throw new ArgumentNullException(nameof(services));

            try
            {
                System.Diagnostics.Debug.WriteLine("📦 AddAdvancedWinUiDataGrid() - Registrácia služieb...");

                // Register logger provider
                if (loggerFactory != null)
                {
                    services.AddSingleton<Configuration.IDataGridLoggerProvider>(new Configuration.DataGridLoggerProvider(loggerFactory));
                }
                else
                {
                    services.AddSingleton<Configuration.IDataGridLoggerProvider>(provider =>
                    {
                        var factory = provider.GetService<ILoggerFactory>();
                        return new Configuration.DataGridLoggerProvider(factory);
                    });
                }

                // Register core services - používame interné typy pre implementáciu
                services.AddScoped<Services.Interfaces.IDataService, Services.Implementation.DataService>();
                services.AddScoped<Services.Interfaces.IValidationService, Services.Implementation.ValidationService>();
                services.AddScoped<Services.Interfaces.IClipboardService, Services.Implementation.ClipboardService>();
                services.AddScoped<Services.Interfaces.IColumnService, Services.Implementation.ColumnService>();
                services.AddScoped<Services.Interfaces.IExportService, Services.Implementation.ExportService>();
                services.AddScoped<Services.Interfaces.INavigationService, Services.Implementation.NavigationService>();

                // Register ViewModels
                services.AddTransient<ViewModels.AdvancedDataGridViewModel>();

                System.Diagnostics.Debug.WriteLine("✅ AddAdvancedWinUiDataGrid() - Služby úspešne zaregistrované");
                return services;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ AddAdvancedWinUiDataGrid() - Chyba: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Registruje služby pre testovanie (s null logger provider)
        /// </summary>
        [System.Runtime.Versioning.SupportedOSPlatform("windows10.0.17763.0")]
        public static IServiceCollection AddAdvancedWinUiDataGridForTesting(this IServiceCollection services)
        {
            if (services == null)
                throw new ArgumentNullException(nameof(services));

            services.AddSingleton<Configuration.IDataGridLoggerProvider, Configuration.NullDataGridLoggerProvider>();

            // Register services
            services.AddScoped<Services.Interfaces.IDataService, Services.Implementation.DataService>();
            services.AddScoped<Services.Interfaces.IValidationService, Services.Implementation.ValidationService>();
            services.AddScoped<Services.Interfaces.IClipboardService, Services.Implementation.ClipboardService>();
            services.AddScoped<Services.Interfaces.IColumnService, Services.Implementation.ColumnService>();
            services.AddScoped<Services.Interfaces.IExportService, Services.Implementation.ExportService>();
            services.AddScoped<Services.Interfaces.INavigationService, Services.Implementation.NavigationService>();

            services.AddTransient<ViewModels.AdvancedDataGridViewModel>();

            return services;
        }

        /// <summary>
        /// Minimálna registrácia pre základné fungovanie (fallback)
        /// </summary>
        [System.Runtime.Versioning.SupportedOSPlatform("windows10.0.17763.0")]
        public static IServiceCollection AddAdvancedWinUiDataGridMinimal(this IServiceCollection services)
        {
            if (services == null)
                throw new ArgumentNullException(nameof(services));

            // Minimal logging
            services.AddSingleton<Configuration.IDataGridLoggerProvider, Configuration.NullDataGridLoggerProvider>();

            // Core services with minimal dependencies
            services.AddSingleton<Services.Interfaces.IDataService, Services.Implementation.DataService>();
            services.AddSingleton<Services.Interfaces.IValidationService, Services.Implementation.ValidationService>();
            services.AddSingleton<Services.Interfaces.IClipboardService, Services.Implementation.ClipboardService>();
            services.AddSingleton<Services.Interfaces.IColumnService, Services.Implementation.ColumnService>();
            services.AddSingleton<Services.Interfaces.IExportService, Services.Implementation.ExportService>();
            services.AddSingleton<Services.Interfaces.INavigationService, Services.Implementation.NavigationService>();

            // ViewModel
            services.AddTransient<ViewModels.AdvancedDataGridViewModel>();

            return services;
        }
    }
}