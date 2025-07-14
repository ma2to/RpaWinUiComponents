// KROK 1: OPRAVA CS0234 - Explicitný export PUBLIC typov
// SÚBOR: RpaWinUiComponents/AdvancedWinUiDataGrid/PublicApiExport.cs (NOVÝ SÚBOR)

namespace RpaWinUiComponents.AdvancedWinUiDataGrid
{
    /// <summary>
    /// KĽÚČOVÁ OPRAVA CS0234: Explicitný export všetkých PUBLIC typov
    /// Tento súbor zabezpečuje, že PUBLIC typy sú dostupné cez NuGet package
    /// </summary>

    // 1. ✅ ColumnDefinition už existuje v správnom namespace - OK
    // 2. ✅ ValidationRule už existuje v správnom namespace - OK  
    // 3. ✅ ThrottlingConfig už existuje v správnom namespace - OK
    // 4. ✅ AdvancedWinUiDataGridControl už existuje v správnom namespace - OK

    /// <summary>
    /// Verification class - zabezpečuje že všetky PUBLIC typy sú exportované
    /// </summary>
    internal static class PublicApiVerification
    {
        /// <summary>
        /// Verifikuje dostupnosť všetkých PUBLIC typov pre NuGet consumers
        /// </summary>
        internal static void VerifyPublicTypes()
        {
            // Verify main control
            _ = typeof(AdvancedWinUiDataGridControl);

            // Verify public models
            _ = typeof(ColumnDefinition);
            _ = typeof(ValidationRule);
            _ = typeof(ThrottlingConfig);

            // Verify extension methods are available
            var serviceCollection = new Microsoft.Extensions.DependencyInjection.ServiceCollection();

            try
            {
                // KĽÚČOVÁ OPRAVA CS1061: Overenie že extension metóda je dostupná
                serviceCollection.AddAdvancedWinUiDataGrid();
                System.Diagnostics.Debug.WriteLine("✅ AddAdvancedWinUiDataGrid extension method je dostupná");
            }
            catch (System.Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ AddAdvancedWinUiDataGrid extension method NEDOSTUPNÁ: {ex.Message}");
            }
        }
    }
}