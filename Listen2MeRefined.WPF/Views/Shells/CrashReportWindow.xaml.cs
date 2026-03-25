using System.Globalization;
using System.Windows;
using Listen2MeRefined.Application.ErrorHandling;
using Listen2MeRefined.WPF.ErrorHandling;

namespace Listen2MeRefined.WPF.Views.Shells;

public partial class CrashReportWindow : Window
{
    public CrashDialogAction Action { get; private set; } = CrashDialogAction.Exit;

    public CrashReportWindow(Exception exception, UnhandledErrorContext context, string logDirectoryPath)
    {
        InitializeComponent();

        var isTerminating = context.IsTerminating;
        PrimaryActionButton.Content = isTerminating ? "Open Logs and Exit" : "Open Logs";
        SecondaryActionButton.Content = isTerminating ? "Exit" : "Close";

        CrashReportContent.DataContext = new CrashReportViewData(
            SourceText: context.Source.ToString(),
            OccurredAtText: context.OccurredAtUtc.ToString("yyyy-MM-dd HH:mm:ss 'UTC'", CultureInfo.InvariantCulture),
            LogDirectoryPath: logDirectoryPath,
            SummaryText: isTerminating
                ? "The application will close after this report."
                : "The current window could not be initialized. You can continue using the rest of the app.",
            ExceptionDetails: exception.ToString());
    }

    private void PrimaryActionButton_OnClick(object sender, RoutedEventArgs e)
    {
        Action = CrashDialogAction.OpenLogsAndExit;
        DialogResult = true;
        Close();
    }

    private void SecondaryActionButton_OnClick(object sender, RoutedEventArgs e)
    {
        Action = CrashDialogAction.Exit;
        DialogResult = false;
        Close();
    }

    private sealed record CrashReportViewData(
        string SourceText,
        string OccurredAtText,
        string LogDirectoryPath,
        string SummaryText,
        string ExceptionDetails);
}
