// PublicApiAliases.cs - FINÁLNA OPRAVA všetkých typových chýb
// Umiestnite do: RpaWinUiComponents/PublicApiAliases.cs (nahradí existujúci súbor)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

// Import internal typov
using InternalColumnDefinition = RpaWinUiComponents.AdvancedWinUiDataGrid.Models.ColumnDefinition;
using InternalValidationRule = RpaWinUiComponents.AdvancedWinUiDataGrid.Models.ValidationRule;
using InternalThrottlingConfig = RpaWinUiComponents.AdvancedWinUiDataGrid.Models.ThrottlingConfig;
using RpaWinUiComponents.AdvancedWinUiDataGrid.Models;

namespace RpaWinUiComponents.AdvancedWinUiDataGrid
{
    /// <summary>
    /// OPRAVENÝ Public API alias pre ColumnDefinition - konvertuje na internal typ
    /// </summary>
    public class ColumnDefinition
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
        /// KĽÚČOVÁ OPRAVA: Explicitná konverzia na internal typ
        /// </summary>
        public InternalColumnDefinition ToInternal()
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

        /// <summary>
        /// Implicitná konverzia z internal typu
        /// </summary>
        public static implicit operator ColumnDefinition(InternalColumnDefinition internalColumn)
        {
            return new ColumnDefinition(internalColumn.Name, internalColumn.DataType)
            {
                MinWidth = internalColumn.MinWidth,
                MaxWidth = internalColumn.MaxWidth,
                Width = internalColumn.Width,
                AllowResize = internalColumn.AllowResize,
                AllowSort = internalColumn.AllowSort,
                IsReadOnly = internalColumn.IsReadOnly,
                Header = internalColumn.Header,
                ToolTip = internalColumn.ToolTip
            };
        }
    }

    /// <summary>
    /// OPRAVENÝ Public API alias pre ValidationRule - so všetkými metódami
    /// </summary>
    public class ValidationRule
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

        // Convenience constructor s jednoduchým Func<object?, bool>
        public ValidationRule(string columnName, Func<object?, bool> simpleValidationFunction, string errorMessage)
        {
            ColumnName = columnName;
            ValidationFunction = (value, row) => simpleValidationFunction(value);
            ErrorMessage = errorMessage;
            RuleName = $"{columnName}_{Guid.NewGuid().ToString("N")[..8]}";
        }

        /// <summary>
        /// KĽÚČOVÁ OPRAVA: ValidateAsync metóda pre public API
        /// </summary>
        public async Task<bool> ValidateAsync(object? value, DataGridRow row, CancellationToken cancellationToken = default)
        {
            try
            {
                if (!ShouldApply(row))
                    return true;

                if (IsAsync && AsyncValidationFunction != null)
                {
                    using var timeoutCts = new CancellationTokenSource(ValidationTimeout);
                    using var combinedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutCts.Token);

                    return await AsyncValidationFunction(value, row, combinedCts.Token);
                }

                return Validate(value, row);
            }
            catch (OperationCanceledException)
            {
                return false; // Timeout alebo zrušenie = nevalidné
            }
            catch
            {
                return false; // Akákoľvek chyba = nevalidné
            }
        }

        /// <summary>
        /// Kontroluje či sa má validácia aplikovať na daný riadok
        /// </summary>
        public bool ShouldApply(DataGridRow row)
        {
            try
            {
                return ApplyCondition?.Invoke(row) ?? true;
            }
            catch
            {
                return true;
            }
        }

        /// <summary>
        /// Synchronne validuje hodnotu
        /// </summary>
        public bool Validate(object? value, DataGridRow row)
        {
            try
            {
                if (!ShouldApply(row))
                    return true;

                return ValidationFunction?.Invoke(value, row) ?? true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// EXPLICITNÁ konverzia na internal typ
        /// </summary>
        public InternalValidationRule ToInternal()
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
    /// OPRAVENÝ Public API alias pre ThrottlingConfig
    /// </summary>
    public class ThrottlingConfig
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
        public InternalThrottlingConfig ToInternal()
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

        /// <summary>
        /// Validácia konfigurácie
        /// </summary>
        public bool IsValidConfig(out string? errorMessage)
        {
            errorMessage = null;

            if (TypingDelayMs < 0)
            {
                errorMessage = "TypingDelayMs musí byť >= 0";
                return false;
            }

            if (PasteDelayMs < 0)
            {
                errorMessage = "PasteDelayMs musí byť >= 0";
                return false;
            }

            if (MaxConcurrentValidations < 1)
            {
                errorMessage = "MaxConcurrentValidations musí byť >= 1";
                return false;
            }

            if (ValidationTimeout <= TimeSpan.Zero)
            {
                errorMessage = "ValidationTimeout musí byť kladný";
                return false;
            }

            return true;
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

        public static ThrottlingConfig Custom(int typingDelayMs, int maxConcurrentValidations = 5)
        {
            return new ThrottlingConfig
            {
                TypingDelayMs = typingDelayMs,
                PasteDelayMs = Math.Max(10, typingDelayMs / 3),
                BatchValidationDelayMs = typingDelayMs * 2,
                MaxConcurrentValidations = maxConcurrentValidations,
                MinValidationIntervalMs = Math.Max(10, typingDelayMs / 6)
            };
        }

        #endregion
    }
}