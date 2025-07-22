// SÚBOR: ValidationRule.cs - OPRAVENÉ ACCESSIBILITY  
using System;
using System.Threading;
using System.Threading.Tasks;
using RpaWinUiComponents.AdvancedWinUiDataGrid.Models;

namespace RpaWinUiComponents.AdvancedWinUiDataGrid
{
    /// <summary>
    /// ✅ OPRAVA CS0051, CS0053: PUBLIC validation rule používa internal DataGridRow len v internal implementácii
    /// </summary>
    public class ValidationRule
    {
        /// <summary>
        /// Názov stĺpca na ktorý sa pravidlo aplikuje
        /// </summary>
        public string ColumnName { get; set; } = string.Empty;

        /// <summary>
        /// Chybová správa pri neúspešnej validácii
        /// </summary>
        public string ErrorMessage { get; set; } = string.Empty;

        /// <summary>
        /// Priorita pravidla (vyššie číslo = vyššia priorita)
        /// </summary>
        public int Priority { get; set; } = 0;

        /// <summary>
        /// Unique identifikátor pravidla
        /// </summary>
        public string RuleName { get; set; } = Guid.NewGuid().ToString();

        /// <summary>
        /// Timeout pre async validácie (default: 5 sekúnd)
        /// </summary>
        public TimeSpan ValidationTimeout { get; set; } = TimeSpan.FromSeconds(5);

        /// <summary>
        /// Či je validácia async
        /// </summary>
        public bool IsAsync { get; set; } = false;

        // ✅ OPRAVA CS0053: Internal function types - nie sú public accessible
        internal Func<object?, DataGridRow, bool> ValidationFunction { get; set; } = (_, _) => true;
        internal Func<DataGridRow, bool> ApplyCondition { get; set; } = _ => true;
        internal Func<object?, DataGridRow, CancellationToken, Task<bool>>? AsyncValidationFunction { get; set; }

        public ValidationRule()
        {
        }

        // ✅ OPRAVA CS0051: Public constructor používa len simple functions bez internal types
        public ValidationRule(string columnName, Func<object?, bool> simpleValidationFunction, string errorMessage)
        {
            ColumnName = columnName;
            ValidationFunction = (value, row) => simpleValidationFunction(value);
            ErrorMessage = errorMessage;
            RuleName = $"{columnName}_{Guid.NewGuid().ToString("N")[..8]}";
        }

        // ✅ INTERNAL constructors s internal types
        internal ValidationRule(string columnName, Func<object?, DataGridRow, bool> validationFunction, string errorMessage)
        {
            ColumnName = columnName;
            ValidationFunction = validationFunction;
            ErrorMessage = errorMessage;
            RuleName = $"{columnName}_{Guid.NewGuid().ToString("N")[..8]}";
        }

        /// <summary>
        /// ✅ OPRAVA CS0051: Internal methods používajú internal types
        /// </summary>
        internal bool ShouldApply(DataGridRow row)
        {
            try
            {
                return ApplyCondition?.Invoke(row) ?? true;
            }
            catch
            {
                return true; // V prípade chyby aplikuj validáciu
            }
        }

        /// <summary>
        /// ✅ OPRAVA CS0051: Internal validation method
        /// </summary>
        internal bool Validate(object? value, DataGridRow row)
        {
            try
            {
                if (!ShouldApply(row))
                    return true;

                return ValidationFunction?.Invoke(value, row) ?? true;
            }
            catch
            {
                return false; // V prípade chyby považuj za nevalidné
            }
        }

        /// <summary>
        /// ✅ OPRAVA CS0051: Internal async validation method  
        /// </summary>
        internal async Task<bool> ValidateAsync(object? value, DataGridRow row, CancellationToken cancellationToken = default)
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

        #region Static Helper Methods - CLEAN PUBLIC API
        public static ValidationRule Required(string columnName, string? errorMessage = null)
        {
            return new ValidationRule(
                columnName,
                value => !string.IsNullOrWhiteSpace(value?.ToString()),
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

        public static ValidationRule Range(string columnName, double min, double max, string? errorMessage = null)
        {
            return new ValidationRule(
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

        public static ValidationRule Email(string columnName, string? errorMessage = null)
        {
            return new ValidationRule(
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

        public static ValidationRule Numeric(string columnName, string? errorMessage = null)
        {
            return new ValidationRule(
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

        public override string ToString()
        {
            return $"{RuleName}: {ColumnName} - {ErrorMessage}";
        }
    }
}