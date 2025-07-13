//Configuration/DependencyInjectionConfig.cs - KOMPLETNÁ OPRAVA EXTENSION METÓD
using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using RpaWinUiComponents.AdvancedWinUiDataGrid.Services.Implementation;
using RpaWinUiComponents.AdvancedWinUiDataGrid.Services.Interfaces;
using RpaWinUiComponents.AdvancedWinUiDataGrid.ViewModels;

namespace RpaWinUiComponents.AdvancedWinUiDataGrid.Configuration
{
    /// <summary>
    /// Konfigurácia Dependency Injection pre AdvancedWinUiDataGrid
    /// </summary>
    public static class DependencyInjectionConfig
    {
        private static IServiceProvider? _serviceProvider;

        /// <summary>
        /// Konfiguruje service provider pre komponent
        /// </summary>
        public static void ConfigureServices(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        /// <summary>
        /// Získa službu z DI kontajnera
        /// </summary>
        public static T? GetService<T>()
        {
            if (_serviceProvider == null)
                return default(T);

            try
            {
                return _serviceProvider.GetService<T>();
            }
            catch
            {
                return default(T);
            }
        }

        /// <summary>
        /// Získa požadovanú službu z DI kontajnera
        /// </summary>
        public static T GetRequiredService<T>() where T : notnull
        {
            if (_serviceProvider == null)
                throw new InvalidOperationException("Services not configured. Call ConfigureServices first.");

            return _serviceProvider.GetRequiredService<T>();
        }

        /// <summary>
        /// Vytvorí default services collection pre komponent
        /// </summary>
        public static IServiceCollection CreateDefaultServices(ILoggerFactory? loggerFactory = null)
        {
            var services = new ServiceCollection();
            services.AddAdvancedWinUiDataGrid(loggerFactory);
            return services;
        }

        /// <summary>
        /// Vytvorí ViewModel bez DI kontajnera (fallback metóda)
        /// </summary>
        public static AdvancedDataGridViewModel CreateViewModelWithoutDI(ILoggerFactory? loggerFactory = null)
        {
            var loggerProvider = new DataGridLoggerProvider(loggerFactory);

            var dataService = new DataService(loggerProvider.CreateLogger<DataService>());
            var validationService = new ValidationService(loggerProvider.CreateLogger<ValidationService>());
            var clipboardService = new ClipboardService(loggerProvider.CreateLogger<ClipboardService>());
            var columnService = new ColumnService(loggerProvider.CreateLogger<ColumnService>());
            var exportService = new ExportService(loggerProvider.CreateLogger<ExportService>());
            var navigationService = new NavigationService(loggerProvider.CreateLogger<NavigationService>());

            return new AdvancedDataGridViewModel(
                dataService,
                validationService,
                clipboardService,
                columnService,
                exportService,
                navigationService,
                loggerProvider.CreateLogger<AdvancedDataGridViewModel>());
        }
    }

    /// <summary>
    /// Extension metódy pre IServiceCollection - KĽÚČOVÉ PRE DEMO
    /// OPRAVA CA1416: Označené ako Windows-only
    /// </summary>
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        /// HLAVNÁ EXTENSION METÓDA: Registruje všetky služby potrebné pre AdvancedWinUiDataGrid
        /// OPRAVA CA1416: Windows-only extension
        /// </summary>
        [System.Runtime.Versioning.SupportedOSPlatform("windows10.0.17763.0")]
        public static IServiceCollection AddAdvancedWinUiDataGrid(this IServiceCollection services, ILoggerFactory? loggerFactory = null)
        {
            if (services == null)
                throw new ArgumentNullException(nameof(services));

            // Register logger provider
            if (loggerFactory != null)
            {
                services.AddSingleton<IDataGridLoggerProvider>(new DataGridLoggerProvider(loggerFactory));
            }
            else
            {
                services.AddSingleton<IDataGridLoggerProvider>(provider =>
                {
                    var factory = provider.GetService<ILoggerFactory>();
                    return new DataGridLoggerProvider(factory);
                });
            }

            // Register core services
            services.AddScoped<IDataService, DataService>();
            services.AddScoped<IValidationService, ValidationService>();
            services.AddScoped<IClipboardService, ClipboardService>();
            services.AddScoped<IColumnService, ColumnService>();
            services.AddScoped<IExportService, ExportService>();
            services.AddScoped<INavigationService, NavigationService>();

            // Register ViewModels
            services.AddTransient<AdvancedDataGridViewModel>();

            return services;
        }

        /// <summary>
        /// Registruje služby pre testovanie (s null logger provider)
        /// OPRAVA CA1416: Windows-only extension
        /// </summary>
        [System.Runtime.Versioning.SupportedOSPlatform("windows10.0.17763.0")]
        public static IServiceCollection AddAdvancedWinUiDataGridForTesting(this IServiceCollection services)
        {
            if (services == null)
                throw new ArgumentNullException(nameof(services));

            services.AddSingleton<IDataGridLoggerProvider, NullDataGridLoggerProvider>();

            // Register services
            services.AddScoped<IDataService, DataService>();
            services.AddScoped<IValidationService, ValidationService>();
            services.AddScoped<IClipboardService, ClipboardService>();
            services.AddScoped<IColumnService, ColumnService>();
            services.AddScoped<IExportService, ExportService>();
            services.AddScoped<INavigationService, NavigationService>();

            services.AddTransient<AdvancedDataGridViewModel>();

            return services;
        }

        /// <summary>
        /// Minimálna registrácia pre základné fungovanie (fallback)
        /// OPRAVA CA1416: Windows-only extension
        /// </summary>
        [System.Runtime.Versioning.SupportedOSPlatform("windows10.0.17763.0")]
        public static IServiceCollection AddAdvancedWinUiDataGridMinimal(this IServiceCollection services)
        {
            if (services == null)
                throw new ArgumentNullException(nameof(services));

            // Minimal logging
            services.AddSingleton<IDataGridLoggerProvider, NullDataGridLoggerProvider>();

            // Core services with minimal dependencies
            services.AddSingleton<IDataService, DataService>();
            services.AddSingleton<IValidationService, ValidationService>();
            services.AddSingleton<IClipboardService, ClipboardService>();
            services.AddSingleton<IColumnService, ColumnService>();
            services.AddSingleton<IExportService, ExportService>();
            services.AddSingleton<INavigationService, NavigationService>();

            // ViewModel
            services.AddTransient<AdvancedDataGridViewModel>();

            return services;
        }
    }

    /// <summary>
    /// Interface pre poskytovanie loggerov v AdvancedWinUiDataGrid komponente
    /// </summary>
    public interface IDataGridLoggerProvider
    {
        /// <summary>
        /// Vytvorí logger pre špecifický typ
        /// </summary>
        ILogger<T> CreateLogger<T>();

        /// <summary>
        /// Vytvorí logger s názvom kategórie
        /// </summary>
        ILogger CreateLogger(string categoryName);
    }

    /// <summary>
    /// Implementácia IDataGridLoggerProvider pre produkčné použitie
    /// </summary>
    public class DataGridLoggerProvider : IDataGridLoggerProvider
    {
        private readonly ILoggerFactory? _loggerFactory;

        public DataGridLoggerProvider(ILoggerFactory? loggerFactory = null)
        {
            _loggerFactory = loggerFactory;
        }

        public ILogger<T> CreateLogger<T>()
        {
            return _loggerFactory?.CreateLogger<T>() ?? NullLogger<T>.Instance;
        }

        public ILogger CreateLogger(string categoryName)
        {
            return _loggerFactory?.CreateLogger(categoryName) ?? NullLogger.Instance;
        }
    }

    /// <summary>
    /// Null Object Pattern implementácia pre prípady kde logging nie je potrebný
    /// </summary>
    public class NullDataGridLoggerProvider : IDataGridLoggerProvider
    {
        public static readonly NullDataGridLoggerProvider Instance = new();

        private NullDataGridLoggerProvider() { }

        public ILogger<T> CreateLogger<T>()
        {
            return NullLogger<T>.Instance;
        }

        public ILogger CreateLogger(string categoryName)
        {
            return NullLogger.Instance;
        }
    }
}