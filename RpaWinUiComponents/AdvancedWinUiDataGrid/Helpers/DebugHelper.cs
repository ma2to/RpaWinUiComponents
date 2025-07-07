//Helpers/DebugHelper.cs - Jednoduchá implementácia pre RpaWinUiComponents
namespace RpaWinUiComponents.AdvancedWinUiDataGrid.Helpers
{
    /// <summary>
    /// Jednoduchý helper pre debug logging
    /// </summary>
    public static class DebugHelper
    {
        /// <summary>
        /// Či je debug logging povolený
        /// </summary>
        public static bool IsDebugEnabled { get; set; } = false;

        /// <summary>
        /// Debug log výstup
        /// </summary>
        public static void WriteDebug(string message)
        {
            if (IsDebugEnabled)
            {
                System.Diagnostics.Debug.WriteLine($"[RpaWinUiComponents] {message}");
            }
        }

        /// <summary>
        /// Debug log s formátovaním
        /// </summary>
        public static void WriteDebug(string format, params object[] args)
        {
            if (IsDebugEnabled)
            {
                System.Diagnostics.Debug.WriteLine($"[RpaWinUiComponents] {string.Format(format, args)}");
            }
        }
    }
}