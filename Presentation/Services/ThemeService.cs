using System;
using System.Windows;
using InventoryERP.Application.Common;

namespace InventoryERP.Presentation.Services;

public sealed class ThemeService : IThemeService
{
    public string Current { get; private set; } = "Light";

    public void Set(string theme)
    {
        Current = theme == "Dark" ? "Dark" : "Light";
        // Example resource dictionary swap for WPF themes
        //InventoryERP.Application.Current.Resources.MergedDictionaries.Clear();
        //InventoryERP.Application.Current.Resources.MergedDictionaries.Add(
        //     new ResourceDictionary { Source = new Uri($"Themes/{Current}.xaml", UriKind.Relative) });
    }

    public void Toggle() => Set(Current == "Dark" ? "Light" : "Dark");
}




