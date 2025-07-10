// Models/PublicApiTypes.cs - FINÁLNA OPRAVA s explicitnými typmi
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace RpaWinUiComponents.AdvancedWinUiDataGrid.Models
{
    /// <summary>
    /// Public API trieda pre ColumnDefinition - SKUTOČNÁ implementácia
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

        /// <summary>
        /// Konvertuje na internú verziu - EXPLICITNÝ TYP
        /// </summary>
        public RpaWinUiComponents.AdvancedWinUiDataGrid.Models.ColumnDefinition ToInternal()
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
    /// Public API trieda pre ValidationRule - SKUTOČNÁ implementácia s ValidateAsync
    /// </summary>
    public class PublicValidationRule
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

        public PublicValidationRule()
        {
            RuleName = Guid.NewGuid().ToString("N")[..8];
        }

        public PublicValidationRule(string columnName, Func<object?, DataGridRow, bool> validationFunction, string errorMessage)
        {
            ColumnName = columnName;
            ValidationFunction = validationFunction;
            ErrorMessage = errorMessage;
            RuleName = $"{columnName}_{Guid.NewGuid().ToString("N")[..8]}";
        }

        // Convenience constructor s jednoduchým Func<object?, bool>
        public PublicValidationRule(string columnName, Func<object?, bool> simpleValidationFunction, string errorMessage)
        {
            ColumnName = columnName;
            ValidationFunction = (value, row) => simpleValidationFunction(value);
            ErrorMessage = errorMessage;
            RuleName = $"{columnName}_{Guid.NewGuid().ToString("N")[..8]}";
        }

        /// <summary>
        /// KĽÚČOVÁ OPRAVA: Pridanie ValidateAsync metódy do public API
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
        /// Konvertuje na internú verziu - EXPLICITNÝ TYP
        /// </summary>
        public RpaWinUiComponents.AdvancedWinUiDataGrid.Models.ValidationRule ToInternal()
        {
            return new RpaWinUiComponents.AdvancedWinUiDataGrid.Models.ValidationRule(ColumnName, ValidationFunction, ErrorMessage)
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

        public static PublicValidationRule Required(string columnName, string? errorMessage = null)
        {
            return new PublicValidationRule(
                columnName,
                (value, row) => !string.IsNullOrWhiteSpace(value?.ToString()),
                errorMessage ?? $"{columnName} je povinné pole"
            )
            {
                RuleName = $"{columnName}_Required"
            };
        }

        public static PublicValidationRule Length(string columnName, int minLength, int maxLength = int.MaxValue, string? errorMessage = null)
        {
            return new PublicValidationRule(
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

        public static PublicValidationRule Range(string columnName, double min, double max, string? errorMessage = null)
        {
            return new PublicValidationRule(
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

        public static PublicValidationRule Email(string columnName, string? errorMessage = null)
        {
            return new PublicValidationRule(
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

        public static PublicValidationRule Numeric(string columnName, string? errorMessage = null)
        {
            return new PublicValidationRule(
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
    /// Public API trieda pre ThrottlingConfig - SKUTOČNÁ implementácia
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

        /// <summary>
        /// Konvertuje na internú verziu - EXPLICITNÝ TYP
        /// </summary>
        public RpaWinUiComponents.AdvancedWinUiDataGrid.Models.ThrottlingConfig ToInternal()
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

        #endregion
    }
}