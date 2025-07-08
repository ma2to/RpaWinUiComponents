// PublicApiAliases.cs - OPRAVENÝ - bez konfliktov namespace
// Umiestnite tento súbor do: RpaWinUiComponents/PublicApiAliases.cs (root level)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace RpaWinUiComponents.AdvancedWinUiDataGrid
{
    /// <summary>
    /// Public API alias pre ColumnDefinition - FINÁLNE RIEŠENIE bez konfliktov
    /// Umožňuje používať: RpaWinUiComponents.AdvancedWinUiDataGrid.ColumnDefinition
    /// </summary>
    public class ColumnDefinition
    {
        // Wrappujeme existujúcu Models.PublicColumnDefinition
        private readonly Models.PublicColumnDefinition _base;

        public ColumnDefinition()
        {
            _base = new Models.PublicColumnDefinition();
        }

        public ColumnDefinition(string name, Type dataType)
        {
            _base = new Models.PublicColumnDefinition(name, dataType);
        }

        /// <summary>
        /// Interný konštruktor pre wrapper s base implementation
        /// </summary>
        private ColumnDefinition(Models.PublicColumnDefinition baseColumn)
        {
            _base = baseColumn;
        }

        // Všetky properties delegujú na base implementation
        public string Name
        {
            get => _base.Name;
            set => _base.Name = value;
        }

        public Type DataType
        {
            get => _base.DataType;
            set => _base.DataType = value;
        }

        public double MinWidth
        {
            get => _base.MinWidth;
            set => _base.MinWidth = value;
        }

        public double MaxWidth
        {
            get => _base.MaxWidth;
            set => _base.MaxWidth = value;
        }

        public double Width
        {
            get => _base.Width;
            set => _base.Width = value;
        }

        public bool AllowResize
        {
            get => _base.AllowResize;
            set => _base.AllowResize = value;
        }

        public bool AllowSort
        {
            get => _base.AllowSort;
            set => _base.AllowSort = value;
        }

        public bool IsReadOnly
        {
            get => _base.IsReadOnly;
            set => _base.IsReadOnly = value;
        }

        public string? Header
        {
            get => _base.Header;
            set => _base.Header = value;
        }

        public string? ToolTip
        {
            get => _base.ToolTip;
            set => _base.ToolTip = value;
        }

        /// <summary>
        /// KĽÚČOVÁ METÓDA: Konverzia na internú verziu
        /// </summary>
        public Models.ColumnDefinition ToInternal() => _base.ToInternal();

        /// <summary>
        /// Implicit konverzia na internú verziu
        /// </summary>
        public static implicit operator Models.ColumnDefinition(ColumnDefinition publicColumn)
        {
            return publicColumn.ToInternal();
        }

        public override string ToString() => $"{Name} ({DataType.Name}) [{MinWidth}-{MaxWidth}]";
    }

    /// <summary>
    /// Public API alias pre ValidationRule - FINÁLNE RIEŠENIE bez konfliktov
    /// </summary>
    public class ValidationRule
    {
        // Wrappujeme existujúcu Models.PublicValidationRule
        private readonly Models.PublicValidationRule _base;

        public ValidationRule()
        {
            _base = new Models.PublicValidationRule();
        }

        public ValidationRule(string columnName, Func<object?, bool> validationFunction, string errorMessage)
        {
            _base = new Models.PublicValidationRule(columnName, validationFunction, errorMessage);
        }

        // Všetky properties delegujú na base implementation
        public string ColumnName
        {
            get => _base.ColumnName;
            set => _base.ColumnName = value;
        }

        public string ErrorMessage
        {
            get => _base.ErrorMessage;
            set => _base.ErrorMessage = value;
        }

        public int Priority
        {
            get => _base.Priority;
            set => _base.Priority = value;
        }

        public string RuleName
        {
            get => _base.RuleName;
            set => _base.RuleName = value;
        }

        public bool IsAsync
        {
            get => _base.IsAsync;
            set => _base.IsAsync = value;
        }

        public TimeSpan ValidationTimeout
        {
            get => _base.ValidationTimeout;
            set => _base.ValidationTimeout = value;
        }

        /// <summary>
        /// KĽÚČOVÁ METÓDA: Konverzia na internú verziu
        /// </summary>
        public Models.ValidationRule ToInternal() => _base.ToInternal();

        /// <summary>
        /// Implicit konverzia na internú verziu
        /// </summary>
        public static implicit operator Models.ValidationRule(ValidationRule publicRule)
        {
            return publicRule.ToInternal();
        }

        /// <summary>
        /// Interný konštruktor pre wrapper s base implementation
        /// </summary>
        private ValidationRule(Models.PublicValidationRule baseRule)
        {
            _base = baseRule;
        }

        #region Static Helper Methods - používajú base implementation s wrapper

        public static ValidationRule Required(string columnName, string? errorMessage = null)
        {
            var baseRule = Models.PublicValidationRule.Required(columnName, errorMessage);
            return new ValidationRule(baseRule);
        }

        public static ValidationRule Length(string columnName, int minLength, int maxLength = int.MaxValue, string? errorMessage = null)
        {
            var baseRule = Models.PublicValidationRule.Length(columnName, minLength, maxLength, errorMessage);
            return new ValidationRule(baseRule);
        }

        public static ValidationRule Range(string columnName, double min, double max, string? errorMessage = null)
        {
            var baseRule = Models.PublicValidationRule.Range(columnName, min, max, errorMessage);
            return new ValidationRule(baseRule);
        }

        public static ValidationRule Email(string columnName, string? errorMessage = null)
        {
            var baseRule = Models.PublicValidationRule.Email(columnName, errorMessage);
            return new ValidationRule(baseRule);
        }

        public static ValidationRule Numeric(string columnName, string? errorMessage = null)
        {
            var baseRule = Models.PublicValidationRule.Numeric(columnName, errorMessage);
            return new ValidationRule(baseRule);
        }

        #endregion

        public override string ToString() => $"{RuleName}: {ColumnName} - {ErrorMessage}";
    }

    /// <summary>
    /// Public API alias pre ThrottlingConfig - FINÁLNE RIEŠENIE bez konfliktov
    /// </summary>
    public class ThrottlingConfig
    {
        // Wrappujeme existujúcu Models.PublicThrottlingConfig
        private readonly Models.PublicThrottlingConfig _base;

        public ThrottlingConfig()
        {
            _base = new Models.PublicThrottlingConfig();
        }

        // Všetky properties delegujú na base implementation
        public int TypingDelayMs
        {
            get => _base.TypingDelayMs;
            set => _base.TypingDelayMs = value;
        }

        public int PasteDelayMs
        {
            get => _base.PasteDelayMs;
            set => _base.PasteDelayMs = value;
        }

        public int BatchValidationDelayMs
        {
            get => _base.BatchValidationDelayMs;
            set => _base.BatchValidationDelayMs = value;
        }

        public int MaxConcurrentValidations
        {
            get => _base.MaxConcurrentValidations;
            set => _base.MaxConcurrentValidations = value;
        }

        public bool IsEnabled
        {
            get => _base.IsEnabled;
            set => _base.IsEnabled = value;
        }

        public TimeSpan ValidationTimeout
        {
            get => _base.ValidationTimeout;
            set => _base.ValidationTimeout = value;
        }

        public int MinValidationIntervalMs
        {
            get => _base.MinValidationIntervalMs;
            set => _base.MinValidationIntervalMs = value;
        }

        /// <summary>
        /// Interný konštruktor pre wrapper s base implementation
        /// </summary>
        private ThrottlingConfig(Models.PublicThrottlingConfig baseConfig)
        {
            _base = baseConfig;
        }

        // Static factory methods - používajú base implementation
        public static ThrottlingConfig Default
        {
            get
            {
                var baseDefault = Models.PublicThrottlingConfig.Default;
                return new ThrottlingConfig(baseDefault);
            }
        }

        public static ThrottlingConfig Fast
        {
            get
            {
                var baseFast = Models.PublicThrottlingConfig.Fast;
                return new ThrottlingConfig(baseFast);
            }
        }

        public static ThrottlingConfig Slow
        {
            get
            {
                var baseSlow = Models.PublicThrottlingConfig.Slow;
                return new ThrottlingConfig(baseSlow);
            }
        }

        public static ThrottlingConfig Disabled
        {
            get
            {
                var baseDisabled = Models.PublicThrottlingConfig.Disabled;
                return new ThrottlingConfig(baseDisabled);
            }
        }

        public static ThrottlingConfig Custom(int typingDelayMs, int maxConcurrentValidations = 5)
        {
            var baseCustom = Models.PublicThrottlingConfig.Custom(typingDelayMs, maxConcurrentValidations);
            return new ThrottlingConfig(baseCustom);
        }

        /// <summary>
        /// KĽÚČOVÁ METÓDA: Konverzia na internú verziu
        /// </summary>
        public Models.ThrottlingConfig ToInternal() => _base.ToInternal();

        /// <summary>
        /// Implicit konverzia na internú verziu
        /// </summary>
        public static implicit operator Models.ThrottlingConfig(ThrottlingConfig publicConfig)
        {
            return publicConfig.ToInternal();
        }

        public override string ToString() => _base.ToString();
    }
}