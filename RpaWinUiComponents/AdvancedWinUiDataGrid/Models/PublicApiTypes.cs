// OPRAVA pre Models/PublicApiTypes.cs - sekcia PublicValidationRule
// Nahraďte existujúcu PublicValidationRule triedu touto opravenou verziou

/// <summary>
/// Validačné pravidlo pre DataGrid - PUBLIC API TRIEDA pre aliasy - OPRAVENÉ
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

    // OPRAVENÉ: Vytvorenie wrapper validation rule s kópiou validation function
    public static ValidationRule CreateWrapper(PublicValidationRule baseRule)
    {
        var wrapper = new ValidationRule()
        {
            ColumnName = baseRule.ColumnName,
            ErrorMessage = baseRule.ErrorMessage,
            RuleName = baseRule.RuleName,
            Priority = baseRule.Priority,
            IsAsync = baseRule.IsAsync,
            ValidationTimeout = baseRule.ValidationTimeout
        };

        // Nastavenie validation function cez private reflection alebo internal access
        wrapper._base = baseRule;
        return wrapper;
    }

    #region Static Helper Methods - OPRAVENÉ

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