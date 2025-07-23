// GlobalUsings.cs - OPRAVENÝ pre CS0518 fix - v1.0.30
// ZJEDNODUŠENÉ global usings bez konfliktov

// ✅ ZÁKLADNÉ .NET typy - BEZ EXPLICIT ALIASOV
global using System;
global using System.Collections.Generic;
global using System.Linq;
global using System.Threading.Tasks;
global using System.ComponentModel;
global using System.Runtime.CompilerServices;

// .NET Extensions
global using Microsoft.Extensions.DependencyInjection;
global using Microsoft.Extensions.Logging;

// ✅ OPRAVENÉ: WinUI 3 - LEN ZÁKLADNÉ TYPY BEZ ALIASOV
global using Microsoft.UI.Xaml;
global using Microsoft.UI.Xaml.Controls;
global using Microsoft.UI.Xaml.Data;
global using Microsoft.UI.Xaml.Media;

// ✅ ODSTRÁNENÉ: Problematické aliasy ktoré spôsobovali CS0518
// Tieto riadky ODSTRÁNENÉ pretože spôsobovali konflikty:
// global using WinUIGrid = Microsoft.UI.Xaml.Controls.Grid;
// global using WinUIColumnDefinition = Microsoft.UI.Xaml.Controls.ColumnDefinition;
// atď.

// ✅ SYSTÉMOVÉ TYPY - explicitne zahrnuté pre CS0518 fix
global using System.Text;
global using System.Collections;
global using System.Runtime.Serialization;