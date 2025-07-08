// PublicApiAliases.cs - FINÁLNA OPRAVA - nahrádzané sú OBA duplicitné súbory
// Umiestnite do: RpaWinUiComponents/PublicApiAliases.cs (root level)

using RpaWinUiComponents.AdvancedWinUiDataGrid.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace RpaWinUiComponents.AdvancedWinUiDataGrid
{
    /// <summary>
    /// Public API alias pre ColumnDefinition - FINÁLNE RIEŠENIE
    /// Dedí z skutočnej PublicColumnDefinition triedy
    /// </summary>
    public class ColumnDefinition : Models.PublicColumnDefinition
    {
        public ColumnDefinition() : base() { }

        public ColumnDefinition(string name, Type dataType) : base(name, dataType) { }
    }

    /// <summary>
    /// Public API alias pre ValidationRule - FINÁLNE RIEŠENIE
    /// Dedí z skutočnej PublicValidationRule triedy
    /// </summary>
    public class ValidationRule : Models.PublicValidationRule
    {
        public ValidationRule() : base() { }

        public ValidationRule(string columnName, Func<object?, DataGridRow, bool> validationFunction, string errorMessage)
            : base(columnName, validationFunction, errorMessage) { }

        // Convenience constructor s jednoduchým Func<object?, bool>
        public ValidationRule(string columnName, Func<object?, bool> simpleValidationFunction, string errorMessage)
            : base(columnName, simpleValidationFunction, errorMessage) { }

        #region Static Helper Methods - delegované na base triedu

        public new static ValidationRule Required(string columnName, string? errorMessage = null)
        {
            var baseRule = Models.PublicValidationRule.Required(columnName, errorMessage);
            return new ValidationRule(baseRule.ColumnName, baseRule.ValidationFunction, baseRule.ErrorMessage)
            {
                RuleName = baseRule.RuleName,
                Priority = baseRule.Priority
            };
        }

        public new static ValidationRule Length(string columnName, int minLength, int maxLength = int.MaxValue, string? errorMessage = null)
        {
            var baseRule = Models.PublicValidationRule.Length(columnName, minLength, maxLength, errorMessage);
            return new ValidationRule(baseRule.ColumnName, baseRule.ValidationFunction, baseRule.ErrorMessage)
            {
                RuleName = baseRule.RuleName,
                Priority = baseRule.Priority
            };
        }

        public new static ValidationRule Range(string columnName, double min, double max, string? errorMessage = null)
        {
            var baseRule = Models.PublicValidationRule.Range(columnName, min, max, errorMessage);
            return new ValidationRule(baseRule.ColumnName, baseRule.ValidationFunction, baseRule.ErrorMessage)
            {
                RuleName = baseRule.RuleName,
                Priority = baseRule.Priority
            };
        }

        public new static ValidationRule Email(string columnName, string? errorMessage = null)
        {
            var baseRule = Models.PublicValidationRule.Email(columnName, errorMessage);
            return new ValidationRule(baseRule.ColumnName, baseRule.ValidationFunction, baseRule.ErrorMessage)
            {
                RuleName = baseRule.RuleName,
                Priority = baseRule.Priority
            };
        }

        public new static ValidationRule Numeric(string columnName, string? errorMessage = null)
        {
            var baseRule = Models.PublicValidationRule.Numeric(columnName, errorMessage);
            return new ValidationRule(baseRule.ColumnName, baseRule.ValidationFunction, baseRule.ErrorMessage)
            {
                RuleName = baseRule.RuleName,
                Priority = baseRule.Priority
            };
        }

        #endregion
    }

    /// <summary>
    /// Public API alias pre ThrottlingConfig - FINÁLNE RIEŠENIE
    /// Dedí z skutočnej PublicThrottlingConfig triedy
    /// </summary>
    public class ThrottlingConfig : Models.PublicThrottlingConfig
    {
        #region Static Factory Methods - delegované na base triedu

        public new static ThrottlingConfig Default =>
            new()
            {
                TypingDelayMs = 300,
                PasteDelayMs = 100,
                BatchValidationDelayMs = 200,
                MaxConcurrentValidations = 5,
                IsEnabled = true,
                ValidationTimeout = TimeSpan.FromSeconds(30),
                MinValidationIntervalMs = 50
            };

        public new static ThrottlingConfig Fast =>
            new()
            {
                TypingDelayMs = 150,
                PasteDelayMs = 50,
                BatchValidationDelayMs = 100,
                MaxConcurrentValidations = 10,
                MinValidationIntervalMs = 25
            };

        public new static ThrottlingConfig Slow =>
            new()
            {
                TypingDelayMs = 500,
                PasteDelayMs = 200,
                BatchValidationDelayMs = 400,
                MaxConcurrentValidations = 3,
                MinValidationIntervalMs = 100
            };

        public new static ThrottlingConfig Disabled =>
            new()
            {
                IsEnabled = false,
                TypingDelayMs = 0,
                PasteDelayMs = 0,
                BatchValidationDelayMs = 0,
                MinValidationIntervalMs = 0
            };

        public new static ThrottlingConfig Custom(int typingDelayMs, int maxConcurrentValidations = 5)
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