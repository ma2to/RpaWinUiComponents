// GlobalUsings.cs - FINÁLNA OPRAVA CS0104 konfliktov
global using System;
global using System.Collections.Generic;
global using System.Linq;
global using System.Threading.Tasks;
global using System.ComponentModel;
global using System.Runtime.CompilerServices;

// WinUI 3 basic namespaces s aliasmi pre zamedzenie konfliktov
global using Microsoft.UI.Xaml;
global using Microsoft.UI.Xaml.Controls;
global using Microsoft.UI.Xaml.Data;
global using Microsoft.UI.Xaml.Media;
global using Microsoft.UI.Xaml.Input;

// .NET Extensions
global using Microsoft.Extensions.DependencyInjection;
global using Microsoft.Extensions.Logging;

// KRITICKÁ OPRAVA: Explicitné aliasy pre WinUI komponenty
global using WinUIGrid = Microsoft.UI.Xaml.Controls.Grid;
global using WinUIRowDefinition = Microsoft.UI.Xaml.Controls.RowDefinition;
global using WinUIColumnDefinition = Microsoft.UI.Xaml.Controls.ColumnDefinition;
global using WinUIBorder = Microsoft.UI.Xaml.Controls.Border;
global using WinUIStackPanel = Microsoft.UI.Xaml.Controls.StackPanel;
global using WinUIScrollViewer = Microsoft.UI.Xaml.Controls.ScrollViewer;
global using WinUITextBlock = Microsoft.UI.Xaml.Controls.TextBlock;
global using WinUIButton = Microsoft.UI.Xaml.Controls.Button;
global using WinUITextBox = Microsoft.UI.Xaml.Controls.TextBox;
global using WinUIUserControl = Microsoft.UI.Xaml.Controls.UserControl;
global using WinUIItemsRepeater = Microsoft.UI.Xaml.Controls.ItemsRepeater;
global using WinUIProgressBar = Microsoft.UI.Xaml.Controls.ProgressBar;

// HLAVNÉ ALIASY pre naše modely - používať VŽDY tieto
global using DataGridColumnDefinition = RpaWinUiComponents.AdvancedWinUiDataGrid.Models.ColumnDefinition;
global using DataGridValidationRule = RpaWinUiComponents.AdvancedWinUiDataGrid.Models.ValidationRule;
global using DataGridThrottlingConfig = RpaWinUiComponents.AdvancedWinUiDataGrid.Models.ThrottlingConfig;
global using DataGridValidationResult = RpaWinUiComponents.AdvancedWinUiDataGrid.Models.ValidationResult;