using System.Windows;
using Listen2MeRefined.Application.Utils;

namespace Listen2MeRefined.WPF.Services;

/// <summary>
///     WPF implementation of <see cref="IClipboardService" /> using <see cref="Clipboard" />.
/// </summary>
public sealed class WpfClipboardService : IClipboardService
{
    public string GetText()
    {
        try { return Clipboard.GetText(); }
        catch { return string.Empty; }
    }
}
