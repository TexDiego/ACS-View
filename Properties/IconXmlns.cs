using Microsoft.Maui.Controls;

// MauiIcons 6.0.0's transitive namespace is not resolved by XamlC in Release.
// Keep the existing XAML/icon package and explicitly map its attached property type.
[assembly: XmlnsDefinition("http://www.aathifmahir.com/dotnet/2022/maui/icons", "MauiIcons.Core", AssemblyName = "MauiIcons.Core")]
