using System.Windows;
using System.Windows.Media;

namespace ExpenseManager.Services;

public static class ThemeService
{
    public static bool IsDark { get; private set; }

    public static void Toggle()
    {
        IsDark = !IsDark;
        SetTheme(IsDark);
    }

    private static void SetTheme(bool dark)
    {
        Set("PrimaryBrush", dark ? "#5B5CE2" : "#315B96");
        Set("PrimaryDarkBrush", dark ? "#4546B8" : "#244777");
        Set("SecondaryBrush", dark ? "#8B8CF4" : "#6E93C4");
        Set("BackgroundBrush", dark ? "#0B1220" : "#DCEAF8");
        Set("CardBrush", dark ? "#141D2F" : "#FFFFFF");
        Set("InputBrush", dark ? "#19243A" : "#EAF2FB");
        Set("TextBrush", dark ? "#F3F4F6" : "#172033");
        Set("MutedBrush", dark ? "#AAB4C8" : "#6B7280");
        Set("SidebarBrush", dark ? "#070D18" : "#162A46");
        Set("SidebarTextBrush", dark ? "#C7D0E2" : "#D5DBEA");
        Set("SidebarActiveBrush", dark ? "#5B5CE2" : "#315B96");
        Set("BorderBrush", dark ? "#263653" : "#C7D8EA");
        Set("InputBorderBrush", dark ? "#657895" : "#172033");
        Set("CardBorderBrush", dark ? "#526582" : "#172033");
        Set("GridLineBrush", dark ? "#202E47" : "#EEF1F6");
        Set("InfoLightBrush", dark ? "#242A55" : "#C9DCF1");
        Set("SuccessLightBrush", dark ? "#173A2A" : "#EAF8EF");
        Set("DangerLightBrush", dark ? "#421E28" : "#FEF0F0");
        Set("WarningLightBrush", dark ? "#42321A" : "#FFF7E8");
        SetSystem(SystemColors.HighlightBrushKey, dark ? "#7072F2" : "#5B5CE2");
        SetSystem(SystemColors.HighlightTextBrushKey, "#FFFFFF");
        SetSystem(SystemColors.ControlBrushKey, dark ? "#202A40" : "#FFFFFF");
        SetSystem(SystemColors.ControlTextBrushKey, dark ? "#F3F4F6" : "#172033");
    }

    private static void Set(string key, string color)
    {
        Application.Current.Resources[key] = new SolidColorBrush((Color)ColorConverter.ConvertFromString(color));
    }

    private static void SetSystem(object key, string color)
    {
        Application.Current.Resources[key] = new SolidColorBrush((Color)ColorConverter.ConvertFromString(color));
    }
}
