// PublicApiAliases.cs - JEDNODUCHÉ TYPE ALIASY na existujúce triedy
// Vytvára type aliasy aby users mohli používať krátke názvy

namespace RpaWinUiComponents.AdvancedWinUiDataGrid
{
    /// <summary>
    /// Public API alias pre ColumnDefinition - RIEŠENIE CS0426
    /// Umožňuje používať: RpaWinUiComponents.AdvancedWinUiDataGrid.ColumnDefinition
    /// </summary>
    public class ColumnDefinition : Models.PublicColumnDefinition
    {
        public ColumnDefinition() : base() { }

        public ColumnDefinition(string name, System.Type dataType) : base(name, dataType) { }
    }

    /// <summary>
    /// Public API alias pre ValidationRule - RIEŠENIE CS0426
    /// Umožňuje používať: RpaWinUiComponents.AdvancedWinUiDataGrid.ValidationRule
    /// </summary>
    public class ValidationRule : Models.PublicValidationRule
    {
        public ValidationRule() : base() { }

        public ValidationRule(string columnName, System.Func<object?, bool> validationFunction, string errorMessage)
            : base(columnName, validationFunction, errorMessage) { }
    }

    /// <summary>
    /// Public API alias pre ThrottlingConfig - RIEŠENIE CS0426
    /// Umožňuje používať: RpaWinUiComponents.AdvancedWinUiDataGrid.ThrottlingConfig
    /// </summary>
    public class ThrottlingConfig : Models.PublicThrottlingConfig
    {
        public new static ThrottlingConfig Default => new();
        public new static ThrottlingConfig Fast => new()
        {
            TypingDelayMs = 150,
            PasteDelayMs = 50,
            BatchValidationDelayMs = 100,
            MaxConcurrentValidations = 10,
            MinValidationIntervalMs = 25
        };
        public new static ThrottlingConfig Slow => new()
        {
            TypingDelayMs = 500,
            PasteDelayMs = 200,
            BatchValidationDelayMs = 400,
            MaxConcurrentValidations = 3,
            MinValidationIntervalMs = 100
        };
        public new static ThrottlingConfig Disabled => new()
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
                PasteDelayMs = System.Math.Max(10, typingDelayMs / 3),
                BatchValidationDelayMs = typingDelayMs * 2,
                MaxConcurrentValidations = maxConcurrentValidations,
                MinValidationIntervalMs = System.Math.Max(10, typingDelayMs / 6)
            };
        }
    }
}