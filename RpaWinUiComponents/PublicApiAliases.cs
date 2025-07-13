// PublicApiAliases.cs - OPRAVENÉ - Extension metódy pre nový namespace
// Tento súbor obsahuje iba extension metódy pre konverziu typov

using System.Collections.Generic;
using System.Linq;

// Import internal typov s explicitnými aliasmi - zabráni konfliktom
using InternalColumnDefinition = RpaWinUiComponents.AdvancedWinUiDataGrid.Models.ColumnDefinition;
using InternalValidationRule = RpaWinUiComponents.AdvancedWinUiDataGrid.Models.ValidationRule;

namespace RpaWinUiComponents.AdvancedWinUiDataGrid
{
    /// <summary>
    /// Extension metódy pre konverziu typov - RIEŠENIE CS1503
    /// Iba extension metódy, bez duplicitných tried
    /// </summary>
    public static class PublicApiExtensions
    {
        /// <summary>
        /// Konvertuje zoznam public ColumnDefinition na internal typy
        /// </summary>
        public static List<InternalColumnDefinition> ToInternal(this List<ColumnDefinition> publicColumns)
        {
            return publicColumns?.Select(c => c.ToInternal()).ToList() ?? new List<InternalColumnDefinition>();
        }

        /// <summary>
        /// Konvertuje zoznam public ValidationRule na internal typy
        /// </summary>
        public static List<InternalValidationRule> ToInternal(this List<ValidationRule> publicRules)
        {
            return publicRules?.Select(r => r.ToInternal()).ToList() ?? new List<InternalValidationRule>();
        }
    }
}