// GlobalAssemblyInfo.cs - Konfigurácia viditeľnosti NuGet package
using System.Runtime.CompilerServices;

// KĽÚČOVÉ: Skrytie internal typov pred IntelliSense a NuGet package konzumentami
[assembly: System.Runtime.CompilerServices.InternalsVisibleTo("RpaWinUiComponents.Tests")]

// OPRAVA: Označenie assembly ako Windows-only pre vyriesenie CA1416 warnings
[assembly: System.Runtime.Versioning.SupportedOSPlatform("windows10.0.17763.0")]

// OPRAVA: Metadata pre NuGet package - skrytie internal namespace
[assembly: System.Reflection.AssemblyMetadata("IsTrimmable", "true")]
[assembly: System.Reflection.AssemblyMetadata("SuppressIldasmAttribute", "true")]