using System.ComponentModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;
using Listen2MeRefined.Application.ErrorHandling;
using Listen2MeRefined.Application.Messages;
using Listen2MeRefined.Application.Navigation;
using Listen2MeRefined.Application.Navigation.Windows;
using Listen2MeRefined.Application.ViewModels.SettingsTabs;
using Serilog;

namespace Listen2MeRefined.Application.ViewModels.Shells;

public partial class SettingsShellViewModel : ShellViewModelBase
{
    public IReadOnlyList<SettingsShellNavigationItem> NavigationItems { get; }
    
    [ObservableProperty] private string _fontFamilyName = string.Empty;

    public SettingsShellViewModel(
        IErrorHandler errorHandler, 
        ILogger logger, 
        IMessenger messenger, 
        IShellContextFactory context,
        ISettingsShellNavigationProvider navigationProvider) : base(errorHandler, logger, messenger, context.Create())
    {
        RegisterMessage<FontFamilyChangedMessage>(OnFontFamilyChangedMessage);
        
        NavigationItems = navigationProvider.CreateNavigationItems();

        PropertyChanged += OnPropertyChanged;
        UpdateActiveRoute(CurrentRoute);
    }

    public override async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        await NavigationService.NavigateAsync<SettingsGeneralTabViewModel>(cancellationToken).ConfigureAwait(true);
        
        await base.InitializeAsync(cancellationToken);
    }

    private void OnPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(CurrentRoute))
        {
            UpdateActiveRoute(CurrentRoute);
        }
    }

    private void UpdateActiveRoute(string route)
    {
        foreach (var navigationItem in NavigationItems)
        {
            navigationItem.IsActive = string.Equals(navigationItem.Route, route, StringComparison.OrdinalIgnoreCase);
        }
    }
    
    private void OnFontFamilyChangedMessage(FontFamilyChangedMessage message)
    {
        Logger.Debug("[SettingsShellViewModel] Received FontFamilyChangedMessage: {value}", message.Value);
        FontFamilyName = message.Value;
    }
}
