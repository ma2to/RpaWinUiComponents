// PublicApi.cs - KOMPLETNÉ RIEŠENIE všetkých CS chýb s novým namespace
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

// Import internal typov s explicitnými aliasmi - zabráni konfliktom
using InternalColumnDefinition = RpaWinUiComponents.AdvancedWinUiDataGrid.Models.ColumnDefinition;
using InternalValidationRule = RpaWinUiComponents.AdvancedWinUiDataGrid.Models.ValidationRule;
using InternalThrottlingConfig = RpaWinUiComponents.AdvancedWinUiDataGrid.Models.ThrottlingConfig;
using RpaWinUiComponents.AdvancedWinUiDataGrid.Models;

namespace RpaWinUiComponents.PublicApi
{
    /// <summary>
    /// OPRAVENÝ Public API pre ColumnDefinition - jasne oddelený namespace
    /// </summary>
    public sealed class ColumnDefinition
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

        public ColumnDefinition() { }

        public ColumnDefinition(string name, Type dataType)
        {
            Name = name;
            DataType = dataType;
            Header = name;
        }

        /// <summary>
        /// EXPLICITNÁ konverzia na internal typ - odstráni ambiguity
        /// </summary>
        internal InternalColumnDefinition ToInternal()
        {
            return new InternalColumnDefinition(Name, DataType)
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
    /// OPRAVENÝ Public API pre ValidationRule - jasne oddelený namespace
    /// </summary>
    public sealed class ValidationRule
    {
        public string ColumnName { get; set; } = string.Empty;
        public Func<object?, DataGridRow, bool> ValidationFunction { get; set; } = (_, _) => true;
        public string ErrorMessage { get; set; } = string.Empty;
        public Func<DataGridRow, bool> ApplyCondition { get; set; } = _ => true;
        public int Priority { get; set; } = 0;
        public string RuleName { get; set; } = string.Empty;
        public bool IsAsync { get; set; } = false;
        public Func<object?, DataGridRow, CancellationToken, Task<bool>>? AsyncValidationFunction { get; set; }
        public TimeSpan ValidationTimeout { get; set; } = TimeSpan.FromSeconds(5);

        public ValidationRule()
        {
            RuleName = Guid.NewGuid().ToString("N")[..8];
        }

        public ValidationRule(string columnName, Func<object?, DataGridRow, bool> validationFunction, string errorMessage)
        {
            ColumnName = columnName;
            ValidationFunction = validationFunction;
            ErrorMessage = errorMessage;
            RuleName = $"{columnName}_{Guid.NewGuid().ToString("N")[..8]}";
        }

        public ValidationRule(string columnName, Func<object?, bool> simpleValidationFunction, string errorMessage)
        {
            ColumnName = columnName;
            ValidationFunction = (value, row) => simpleValidationFunction(value);
            ErrorMessage = errorMessage;
            RuleName = $"{columnName}_{Guid.NewGuid().ToString("N")[..8]}";
        }

        /// <summary>
        /// EXPLICITNÁ konverzia na internal typ
        /// </summary>
        internal InternalValidationRule ToInternal()
        {
            return new InternalValidationRule(ColumnName, ValidationFunction, ErrorMessage)
            {
                Priority = Priority,
                RuleName = RuleName,
                IsAsync = IsAsync,
                AsyncValidationFunction = AsyncValidationFunction,
                ValidationTimeout = ValidationTimeout,
                ApplyCondition = ApplyCondition
            };
        }

        #region Static Helper Methods
        public static ValidationRule Required(string columnName, string? errorMessage = null)
        {
            return new ValidationRule(
                columnName,
                (value, row) => !string.IsNullOrWhiteSpace(value?.ToString()),
                errorMessage ?? $"{columnName} je povinné pole"
            )
            {
                RuleName = $"{columnName}_Required"
            };
        }

        public static ValidationRule Length(string columnName, int minLength, int maxLength = int.MaxValue, string? errorMessage = null)
        {
            return new ValidationRule(
                columnName,
                (value, row) =>
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

        public static ValidationRule Range(string columnName, double min, double max, string? errorMessage = null)
        {
            return new ValidationRule(
                columnName,
                (value, row) =>
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

        public static ValidationRule Email(string columnName, string? errorMessage = null)
        {
            return new ValidationRule(
                columnName,
                (value, row) =>
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

        public static ValidationRule Numeric(string columnName, string? errorMessage = null)
        {
            return new ValidationRule(
                columnName,
                (value, row) =>
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
    /// OPRAVENÝ Public API pre ThrottlingConfig - jasne oddelený namespace
    /// </summary>
    public sealed class ThrottlingConfig
    {
        public int TypingDelayMs { get; set; } = 300;
        public int PasteDelayMs { get; set; } = 100;
        public int BatchValidationDelayMs { get; set; } = 200;
        public int MaxConcurrentValidations { get; set; } = 5;
        public bool IsEnabled { get; set; } = true;
        public TimeSpan ValidationTimeout { get; set; } = TimeSpan.FromSeconds(30);
        public int MinValidationIntervalMs { get; set; } = 50;

        /// <summary>
        /// EXPLICITNÁ konverzia na internal typ
        /// </summary>
        internal InternalThrottlingConfig ToInternal()
        {
            return new InternalThrottlingConfig
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

        #region Static Factory Methods
        public static ThrottlingConfig Default => new();

        public static ThrottlingConfig Fast => new()
        {
            TypingDelayMs = 150,
            PasteDelayMs = 50,
            BatchValidationDelayMs = 100,
            MaxConcurrentValidations = 10,
            MinValidationIntervalMs = 25
        };

        public static ThrottlingConfig Slow => new()
        {
            TypingDelayMs = 500,
            PasteDelayMs = 200,
            BatchValidationDelayMs = 400,
            MaxConcurrentValidations = 3,
            MinValidationIntervalMs = 100
        };

        public static ThrottlingConfig Disabled => new()
        {
            IsEnabled = false,
            TypingDelayMs = 0,
            PasteDelayMs = 0,
            BatchValidationDelayMs = 0,
            MinValidationIntervalMs = 0
        };
        #endregion
    }
}

/// <summary>
/// Extension metódy pre konverziu typov - RIEŠENIE CS1503
/// </summary>
public static class PublicApiExtensions
{
    public static List<InternalColumnDefinition> ToInternal(this List<RpaWinUiComponents.PublicApi.ColumnDefinition> publicColumns)
    {
        return publicColumns?.Select(c => c.ToInternal()).ToList() ?? new List<InternalColumnDefinition>();
    }

    public static List<InternalValidationRule> ToInternal(this List<RpaWinUiComponents.PublicApi.ValidationRule> publicRules)
    {
        return publicRules?.Select(r => r.ToInternal()).ToList() ?? new List<InternalValidationRule>();
    }
}