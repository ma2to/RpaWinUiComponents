﻿// GlobalUsings.cs - OPRAVENÝ bez konfliktov
global using System;
global using System.Collections.Generic;
global using System.Linq;
global using System.Threading.Tasks;
global using System.ComponentModel;
global using System.Runtime.CompilerServices;
global using System.Text;

// .NET Extensions
global using Microsoft.Extensions.DependencyInjection;
global using Microsoft.Extensions.Logging;

// WinUI 3 - ZÁKLADNÉ TYPY BEZ KONFLIKTOV
global using Microsoft.UI.Xaml;
global using Microsoft.UI.Xaml.Data;
global using Microsoft.UI.Xaml.Media;
global using Microsoft.UI.Xaml.Input;

// KRITICKÁ OPRAVA: Explicitné aliasy pre WinUI komponenty aby sa predišlo konfliktom
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