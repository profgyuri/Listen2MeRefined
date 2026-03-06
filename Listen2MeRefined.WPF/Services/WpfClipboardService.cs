using Listen2MeRefined.Infrastructure.FolderBrowser;
using System.Windows;

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
