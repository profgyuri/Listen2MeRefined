using System.Collections.Generic;
using System.Windows;
using System.Windows.Media;
using CommunityToolkit.Mvvm.Messaging;
using Listen2MeRefined.Application.Messages;
using Listen2MeRefined.Application.Settings;
using Listen2MeRefined.Infrastructure.Media.SoundWave;
using SkiaSharp;

namespace Listen2MeRefined.WPF.Utils.Theming;

public sealed class AppThemeService : IAppThemeService
{
    private readonly IMessenger _messenger;
    private readonly IEnumerable<IWaveformPaletteAware> _waveformPaletteAwareTargets;

    public AppThemeService(
        IEnumerable<IWaveformPaletteAware> waveformPaletteAwareTargets,
        IMessenger messenger)
    {
        _waveformPaletteAwareTargets = waveformPaletteAwareTargets;
        _messenger = messenger;
    }

    private static readonly IReadOnlyList<string> SupportedThemeModes = ["Dark", "Light"];
    private static readonly IReadOnlyList<string> SupportedAccentColors = ["Orange", "Blue", "Green", "Purple", "Red"];

    private static readonly IReadOnlyDictionary<string, string> ThemePaletteSources = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
    {
        ["Dark"] = "Styles/Themes/Base.Dark.xaml",
        ["Light"] = "Styles/Themes/Base.Light.xaml"
    };

    private static readonly IReadOnlyDictionary<string, string> AccentPaletteSources = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
    {
        ["Orange"] = "Styles/Themes/Accent.Orange.xaml",
        ["Blue"] = "Styles/Themes/Accent.Blue.xaml",
        ["Green"] = "Styles/Themes/Accent.Green.xaml",
        ["Purple"] = "Styles/Themes/Accent.Purple.xaml",
        ["Red"] = "Styles/Themes/Accent.Red.xaml"
    };

    private static readonly string[] BaseColorKeys = [
        "PrimaryColor",
        "PrimaryLightColor",
        "SecondaryColor",
        "SecondaryDarkColor",
        "BorderColor",
        "SurfaceColor",
        "SurfaceElevatedColor",
        "SurfaceRaisedColor",
        "SeparatorColor"
    ];

    private static readonly string[] AccentColorKeys = [
        "TertiaryColor",
        "TertiaryLightColor",
        "TertiaryLightMidColor",
        "TertiaryMidColor",
        "TertiaryDarkColor"
    ];

    private static readonly IReadOnlyDictionary<string, string> ColorToBrushMap = new Dictionary<string, string>
    {
        ["PrimaryColor"] = "PrimaryBrush",
        ["PrimaryLightColor"] = "PrimaryLightBrush",
        ["SecondaryColor"] = "SecondaryBrush",
        ["SecondaryDarkColor"] = "SecondaryDarkBrush",
        ["BorderColor"] = "BorderBrush",
        ["SurfaceColor"] = "SurfaceBrush",
        ["SurfaceElevatedColor"] = "SurfaceElevatedBrush",
        ["SurfaceRaisedColor"] = "SurfaceRaisedBrush",
        ["SeparatorColor"] = "SeparatorBrush",
        ["TertiaryColor"] = "TertiaryBrush",
        ["TertiaryLightColor"] = "TertiaryLightBrush",
        ["TertiaryLightMidColor"] = "TertiaryLightMidBrush",
        ["TertiaryMidColor"] = "TertiaryMidBrush",
        ["TertiaryDarkColor"] = "TertiaryDarkBrush"
    };

    public IReadOnlyList<string> GetThemeModes() => SupportedThemeModes;

    public IReadOnlyList<string> GetAccentColors() => SupportedAccentColors;

    public void ApplyTheme(string themeMode, string accentColor)
    {
        var normalizedTheme = ThemePaletteSources.ContainsKey(themeMode) ? themeMode : "Dark";
        var normalizedAccent = AccentPaletteSources.ContainsKey(accentColor) ? accentColor : "Orange";

        var basePalette = new ResourceDictionary
        {
            Source = new Uri(ThemePaletteSources[normalizedTheme], UriKind.Relative)
        };
        var accentPalette = new ResourceDictionary
        {
            Source = new Uri(AccentPaletteSources[normalizedAccent], UriKind.Relative)
        };

        ApplyPalette(basePalette, BaseColorKeys);
        ApplyPalette(accentPalette, AccentColorKeys);

        if (System.Windows.Application.Current.Resources["HoloBackground"] is SolidColorBrush holoBackground
            && System.Windows.Application.Current.Resources["PrimaryColor"] is Color primaryColor)
        {
            holoBackground.Color = primaryColor;
        }

        if (System.Windows.Application.Current.Resources["MaterialDesignPaper"] is SolidColorBrush paperBrush
            && System.Windows.Application.Current.Resources["PrimaryColor"] is Color materialPaperColor)
        {
            paperBrush.Color = materialPaperColor;
        }

        if (System.Windows.Application.Current.Resources["ContainerGradientBackgroundBrush"] is SolidColorBrush containerBrush
            && System.Windows.Application.Current.Resources["PrimaryLightColor"] is Color primaryLightColor)
        {
            containerBrush.Color = primaryLightColor;
        }

        var waveformLineColor = System.Windows.Application.Current.Resources["TertiaryColor"] is Color accent
            ? ToSkColor(accent)
            : new SKColor(255, 138, 61);
        var waveformBackgroundColor = System.Windows.Application.Current.Resources["SurfaceElevatedColor"] is Color background
            ? ToSkColor(background)
            : new SKColor(35, 35, 35);

        foreach (var paletteAwareTarget in _waveformPaletteAwareTargets)
        {
            paletteAwareTarget.UpdatePalette(waveformLineColor, waveformBackgroundColor);
        }

        _messenger.Send(new AppThemeChangedMessage());
    }

    private static void ApplyPalette(ResourceDictionary palette, IEnumerable<string> colorKeys)
    {
        foreach (var colorKey in colorKeys)
        {
            if (palette[colorKey] is not Color color)
            {
                continue;
            }

            System.Windows.Application.Current.Resources[colorKey] = color;

            if (ColorToBrushMap.TryGetValue(colorKey, out var brushKey)
                && System.Windows.Application.Current.Resources[brushKey] is SolidColorBrush)
            {
                System.Windows.Application.Current.Resources[brushKey] = new SolidColorBrush(color);
            }
        }
    }

    private static SKColor ToSkColor(Color color) => new(color.R, color.G, color.B, color.A);
}
