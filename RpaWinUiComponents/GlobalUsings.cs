// GlobalUsings.cs - KOMPLETNÁ OPRAVA pre CS1537 chyby
global using System;
global using System.Collections.Generic;
global using System.Linq;
global using System.Threading.Tasks;
global using System.ComponentModel;
global using System.Runtime.CompilerServices;

// WinUI 3 basic namespaces
global using Microsoft.UI.Xaml;
global using Microsoft.UI.Xaml.Controls;
global using Microsoft.UI.Xaml.Data;
global using Microsoft.UI.Xaml.Media;
global using Microsoft.UI.Xaml.Input;

// .NET Extensions
global using Microsoft.Extensions.DependencyInjection;
global using Microsoft.Extensions.Logging;

// KRITICKÁ OPRAVA: Explicit aliasy pre predchádzanie konfliktov
// Tieto aliasy riešia CS1537 chyby s ColumnDefinition
global using WinUIGrid = Microsoft.UI.Xaml.Controls.Grid;
global using WinUIRowDefinition = Microsoft.UI.Xaml.Controls.RowDefinition;
global using WinUIColumnDefinition = Microsoft.UI.Xaml.Controls.ColumnDefinition;
global using WinUIBorder = Microsoft.UI.Xaml.Controls.Border;
global using WinUIStackPanel = Microsoft.UI.Xaml.Controls.StackPanel;
global using WinUIScrollViewer = Microsoft.UI.Xaml.Controls.ScrollViewer;
global using WinUITextBlock = Microsoft.UI.Xaml.Controls.TextBlock;
global using WinUIButton = Microsoft.UI.Xaml.Controls.Button;
global using WinUITextBox = Microsoft.UI.Xaml.Controls.TextBox;

// HLAVNÝ ALIAS - tento rieši všetky CS1537 chyby v celom projekte
global using DataGridColumnDefinition = RpaWinUiComponents.AdvancedWinUiDataGrid.Models.ColumnDefinition;