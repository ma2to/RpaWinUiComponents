//Models/PublicApiTypes.cs - Top-level triedy pre jednoduché aliasy
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace RpaWinUiComponents.AdvancedWinUiDataGrid.Models
{
    /// <summary>
    /// Definícia stĺpca pre DataGrid - PUBLIC API TRIEDA pre aliasy
    /// </summary>
    public class PublicColumnDefinition
    {
        public string Name { get; set; } = string.Empty;
        public Type DataType { get; set; } = typeof(string);
        public double MinWidth { get; set; } = 80;
        public double MaxWidth { get; set; } = 300;
        public double Width { get; set; } = 150;
        public bool AllowResize { get; set; } = true;
        public bool AllowSort { get; set; } = true;
        public bool IsReadOnly { get; set; } = false;
        public string? Header { get; set; }
        public string? ToolTip { get; set; }

        public PublicColumnDefinition() { }

        public PublicColumnDefinition(string name, Type dataType)
        {
            Name = name;
            DataType = dataType;
            Header = name;
        }

        // Konverzia na internú verziu
        internal RpaWinUiComponents.AdvancedWinUiDataGrid.Models.ColumnDefinition ToInternal()
        {
            return new RpaWinUiComponents.AdvancedWinUiDataGrid.Models.ColumnDefinition(Name, DataType)
            {
                MinWidth = MinWidth,
                MaxWidth = MaxWidth,
                Width = Width,
                AllowResize = AllowResize,
                AllowSort = AllowSort,
                IsReadOnly = IsReadOnly,
                Header = Header,
                ToolTip = ToolTip
            };
        }
    }

    /// <summary>
    /// Validačné pravidlo pre DataGrid - PUBLIC API TRIEDA pre aliasy
    /// </summary>
    public class PublicValidationRule
    {
        public string ColumnName { get; set; } = string.Empty;
        public Func<object?, bool> ValidationFunction { get; set; } = _ => true;
        public string ErrorMessage { get; set; } = string.Empty;
        public Func<bool> ApplyCondition { get; set; } = () => true;
        public int Priority { get; set; } = 0;
        public string RuleName { get; set; } = string.Empty;
        public bool IsAsync { get; set; } = false;
        public Func<object?, CancellationToken, Task<bool>>? AsyncValidationFunction { get; set; }
        public TimeSpan ValidationTimeout { get; set; } = TimeSpan.FromSeconds(5);

        public PublicValidationRule()
        {
            RuleName = Guid.NewGuid().ToString("N")[..8];
        }

        public PublicValidationRule(string columnName, Func<object?, bool> validationFunction, string errorMessage)
        {
            ColumnName = columnName;
            ValidationFunction = validationFunction;
            ErrorMessage = errorMessage;
            RuleName = $"{columnName}_{Guid.NewGuid().ToString("N")[..8]}";
        }

        // Konverzia na internú verziu
        internal RpaWinUiComponents.AdvancedWinUiDataGrid.Models.ValidationRule ToInternal()
        {
            return new RpaWinUiComponents.AdvancedWinUiDataGrid.Models.ValidationRule(ColumnName,
                (value, row) => ValidationFunction(value),
                ErrorMessage)
            {
                Priority = Priority,
                RuleName = RuleName,
                IsAsync = IsAsync,
                AsyncValidationFunction = AsyncValidationFunction != null
                    ? (value, row, token) => AsyncValidationFunction(value, token)
                    : null,
                ValidationTimeout = ValidationTimeout,
                ApplyCondition = row => ApplyCondition()
            };
        }

        #region Static Helper Methods

        /// <summary>
        /// Vytvorí pravidlo pre povinné pole
        /// </summary>
        public static PublicValidationRule Required(string columnName, string? errorMessage = null)
        {
            return new PublicValidationRule(
                columnName,
                value => !string.IsNullOrWhiteSpace(value?.ToString()),
                errorMessage ?? $"{columnName} je povinné pole"
            )
            {
                RuleName = $"{columnName}_Required"
            };
        }

        /// <summary>
        /// Vytvorí pravidlo pre kontrolu dĺžky textu
        /// </summary>
        public static PublicValidationRule Length(string columnName, int minLength, int maxLength = int.MaxValue, string? errorMessage = null)
        {
            return new PublicValidationRule(
                columnName,
                value =>
                {
                    var text = value?.ToString() ?? "";
                    return text.Length >= minLength && text.Length <= maxLength;
                },
                errorMessage ?? $"{columnName} musí mať dĺžku medzi {minLength} a {maxLength} znakmi"
            )
            {
                RuleName = $"{columnName}_Length"
            };
        }

        /// <summary>
        /// Vytvorí pravidlo pre kontrolu číselného rozsahu
        /// </summary>
        public static PublicValidationRule Range(string columnName, double min, double max, string? errorMessage = null)
        {
            return new PublicValidationRule(
                columnName,
                value =>
                {
                    if (value == null || string.IsNullOrWhiteSpace(value.ToString()))
                        return true;

                    if (double.TryParse(value.ToString(), out double numValue))
                    {
                        return numValue >= min && numValue <= max;
                    }

                    return false;
                },
                errorMessage ?? $"{columnName} musí byť medzi {min} a {max}"
            )
            {
                RuleName = $"{columnName}_Range"
            };
        }

        /// <summary>
        /// Vytvorí pravidlo pre validáciu emailu
        /// </summary>
        public static PublicValidationRule Email(string columnName, string? errorMessage = null)
        {
            return new PublicValidationRule(
                columnName,
                value =>
                {
                    if (value == null || string.IsNullOrWhiteSpace(value.ToString()))
                        return true;

                    var email = value.ToString();
                    return email?.Contains("@") == true && email.Contains(".") && email.Length > 5;
                },
                errorMessage ?? $"{columnName} musí mať platný formát emailu"
            )
            {
                RuleName = $"{columnName}_Email"
            };
        }

        /// <summary>
        /// Vytvorí pravidlo pre validáciu číselných hodnôt
        /// </summary>
        public static PublicValidationRule Numeric(string columnName, string? errorMessage = null)
        {
            return new PublicValidationRule(
                columnName,
                value =>
                {
                    if (value == null || string.IsNullOrWhiteSpace(value.ToString()))
                        return true;

                    return double.TryParse(value.ToString(), out _);
                },
                errorMessage ?? $"{columnName} musí byť číslo"
            )
            {
                RuleName = $"{columnName}_Numeric"
            };
        }

        #endregion
    }

    /// <summary>
    /// Konfigurácia throttling pre DataGrid - PUBLIC API TRIEDA pre aliasy
    /// </summary>
    public class PublicThrottlingConfig
    {
        public int TypingDelayMs { get; set; } = 300;
        public int PasteDelayMs { get; set; } = 100;
        public int BatchValidationDelayMs { get; set; } = 200;
        public int MaxConcurrentValidations { get; set; } = 5;
        public bool IsEnabled { get; set; } = true;
        public TimeSpan ValidationTimeout { get; set; } = TimeSpan.FromSeconds(30);
        public int MinValidationIntervalMs { get; set; } = 50;

        public static PublicThrottlingConfig Default => new();
        public static PublicThrottlingConfig Fast => new()
        {
            TypingDelayMs = 150,
            PasteDelayMs = 50,
            BatchValidationDelayMs = 100,
            MaxConcurrentValidations = 10,
            MinValidationIntervalMs = 25
        };
        public static PublicThrottlingConfig Slow => new()
        {
            TypingDelayMs = 500,
            PasteDelayMs = 200,
            BatchValidationDelayMs = 400,
            MaxConcurrentValidations = 3,
            MinValidationIntervalMs = 100
        };
        public static PublicThrottlingConfig Disabled => new()
        {
            IsEnabled = false,
            TypingDelayMs = 0,
            PasteDelayMs = 0,
            BatchValidationDelayMs = 0,
            MinValidationIntervalMs = 0
        };

        public static PublicThrottlingConfig Custom(int typingDelayMs, int maxConcurrentValidations = 5)
        {
            return new PublicThrottlingConfig
            {
                TypingDelayMs = typingDelayMs,
                PasteDelayMs = Math.Max(10, typingDelayMs / 3),
                BatchValidationDelayMs = typingDelayMs * 2,
                MaxConcurrentValidations = maxConcurrentValidations,
                MinValidationIntervalMs = Math.Max(10, typingDelayMs / 6)
            };
        }

        // Konverzia na internú verziu
        internal RpaWinUiComponents.AdvancedWinUiDataGrid.Models.ThrottlingConfig ToInternal()
        {
            return new RpaWinUiComponents.AdvancedWinUiDataGrid.Models.ThrottlingConfig
            {
                TypingDelayMs = TypingDelayMs,
                PasteDelayMs = PasteDelayMs,
                BatchValidationDelayMs = BatchValidationDelayMs,
                MaxConcurrentValidations = MaxConcurrentValidations,
                IsEnabled = IsEnabled,
                ValidationTimeout = ValidationTimeout,
                MinValidationIntervalMs = MinValidationIntervalMs
            };
        }

        public override string ToString()
        {
            return $"Throttling: {TypingDelayMs}ms typing, {MaxConcurrentValidations} concurrent, Enabled: {IsEnabled}";
        }
    }
}