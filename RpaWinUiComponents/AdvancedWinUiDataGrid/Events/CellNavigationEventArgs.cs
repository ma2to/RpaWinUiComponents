//Events/CellNavigationEventArgs.cs - OPRAVENÉ bez duplikátov
using System;
using RpaWinUiComponents.AdvancedWinUiDataGrid.Models;

namespace RpaWinUiComponents.AdvancedWinUiDataGrid.Events
{
    /// <summary>
    /// INTERNAL - Cell navigation event args
    /// </summary>
    internal class CellNavigationEventArgs : EventArgs
    {
        public int OldRowIndex { get; init; }
        public int OldColumnIndex { get; init; }
        public int NewRowIndex { get; init; }
        public int NewColumnIndex { get; init; }
        public DataGridCell? OldCell { get; init; }
        public DataGridCell? NewCell { get; init; }
        public NavigationDirection Direction { get; init; }
    }

    /// <summary>
    /// INTERNAL - Navigation direction enum (JEDINÁ DEFINÍCIA)
    /// </summary>
    internal enum NavigationDirection
    {
        None,
        Next,
        Previous,
        Up,
        Down,
        Home,
        End
    }
}