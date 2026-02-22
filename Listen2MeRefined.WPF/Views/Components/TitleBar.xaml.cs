namespace Listen2MeRefined.WPF.Views.Components;

using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

public partial class TitleBar : UserControl
{
    public static readonly DependencyProperty CenterTextProperty =
        DependencyProperty.Register(
            nameof(CenterText),
            typeof(string),
            typeof(TitleBar),
            new PropertyMetadata(string.Empty));

    public static readonly DependencyProperty ShowSettingsButtonProperty =
        DependencyProperty.Register(
            nameof(ShowSettingsButton),
            typeof(bool),
            typeof(TitleBar),
            new PropertyMetadata(false));

    public static readonly DependencyProperty ShowMinimizeButtonProperty =
        DependencyProperty.Register(
            nameof(ShowMinimizeButton),
            typeof(bool),
            typeof(TitleBar),
            new PropertyMetadata(true));

    public static readonly DependencyProperty ShowMaximizeButtonProperty =
        DependencyProperty.Register(
            nameof(ShowMaximizeButton),
            typeof(bool),
            typeof(TitleBar),
            new PropertyMetadata(true));

    public static readonly DependencyProperty ShowCloseButtonProperty =
        DependencyProperty.Register(
            nameof(ShowCloseButton),
            typeof(bool),
            typeof(TitleBar),
            new PropertyMetadata(true));

    public static readonly DependencyProperty ShowTaskStatusProperty =
        DependencyProperty.Register(
            nameof(ShowTaskStatus),
            typeof(bool),
            typeof(TitleBar),
            new PropertyMetadata(false));

    public static readonly DependencyProperty TaskStatusTextProperty =
        DependencyProperty.Register(
            nameof(TaskStatusText),
            typeof(string),
            typeof(TitleBar),
            new PropertyMetadata(string.Empty));

    public static readonly DependencyProperty SettingsCommandProperty =
        DependencyProperty.Register(
            nameof(SettingsCommand),
            typeof(ICommand),
            typeof(TitleBar),
            new PropertyMetadata(null));

    public static readonly DependencyProperty MinimizeCommandProperty =
        DependencyProperty.Register(
            nameof(MinimizeCommand),
            typeof(ICommand),
            typeof(TitleBar),
            new PropertyMetadata(null, OnCommandChanged));

    public static readonly DependencyProperty ToggleMaximizeCommandProperty =
        DependencyProperty.Register(
            nameof(ToggleMaximizeCommand),
            typeof(ICommand),
            typeof(TitleBar),
            new PropertyMetadata(null, OnCommandChanged));

    public static readonly DependencyProperty CloseCommandProperty =
        DependencyProperty.Register(
            nameof(CloseCommand),
            typeof(ICommand),
            typeof(TitleBar),
            new PropertyMetadata(null, OnCommandChanged));

    private static readonly DependencyPropertyKey EffectiveMinimizeCommandPropertyKey =
        DependencyProperty.RegisterReadOnly(
            nameof(EffectiveMinimizeCommand),
            typeof(ICommand),
            typeof(TitleBar),
            new PropertyMetadata(null));

    public static readonly DependencyProperty EffectiveMinimizeCommandProperty =
        EffectiveMinimizeCommandPropertyKey.DependencyProperty;

    private static readonly DependencyPropertyKey EffectiveToggleMaximizeCommandPropertyKey =
        DependencyProperty.RegisterReadOnly(
            nameof(EffectiveToggleMaximizeCommand),
            typeof(ICommand),
            typeof(TitleBar),
            new PropertyMetadata(null));

    public static readonly DependencyProperty EffectiveToggleMaximizeCommandProperty =
        EffectiveToggleMaximizeCommandPropertyKey.DependencyProperty;

    private static readonly DependencyPropertyKey EffectiveCloseCommandPropertyKey =
        DependencyProperty.RegisterReadOnly(
            nameof(EffectiveCloseCommand),
            typeof(ICommand),
            typeof(TitleBar),
            new PropertyMetadata(null));

    public static readonly DependencyProperty EffectiveCloseCommandProperty =
        EffectiveCloseCommandPropertyKey.DependencyProperty;

    private static readonly DependencyPropertyKey IsMaximizeAvailablePropertyKey =
        DependencyProperty.RegisterReadOnly(
            nameof(IsMaximizeAvailable),
            typeof(bool),
            typeof(TitleBar),
            new PropertyMetadata(false));

    public static readonly DependencyProperty IsMaximizeAvailableProperty =
        IsMaximizeAvailablePropertyKey.DependencyProperty;

    private static readonly DependencyPropertyKey IsMaximizedPropertyKey =
        DependencyProperty.RegisterReadOnly(
            nameof(IsMaximized),
            typeof(bool),
            typeof(TitleBar),
            new PropertyMetadata(false));

    public static readonly DependencyProperty IsMaximizedProperty =
        IsMaximizedPropertyKey.DependencyProperty;

    private Window? _hostWindow;
    private DependencyPropertyDescriptor? _resizeModeDescriptor;
    private ICommand? _defaultMinimizeCommand;
    private ICommand? _defaultToggleMaximizeCommand;
    private ICommand? _defaultCloseCommand;

    public string CenterText
    {
        get => (string)GetValue(CenterTextProperty);
        set => SetValue(CenterTextProperty, value);
    }

    public bool ShowSettingsButton
    {
        get => (bool)GetValue(ShowSettingsButtonProperty);
        set => SetValue(ShowSettingsButtonProperty, value);
    }

    public bool ShowMinimizeButton
    {
        get => (bool)GetValue(ShowMinimizeButtonProperty);
        set => SetValue(ShowMinimizeButtonProperty, value);
    }

    public bool ShowMaximizeButton
    {
        get => (bool)GetValue(ShowMaximizeButtonProperty);
        set => SetValue(ShowMaximizeButtonProperty, value);
    }

    public bool ShowCloseButton
    {
        get => (bool)GetValue(ShowCloseButtonProperty);
        set => SetValue(ShowCloseButtonProperty, value);
    }

    public bool ShowTaskStatus
    {
        get => (bool)GetValue(ShowTaskStatusProperty);
        set => SetValue(ShowTaskStatusProperty, value);
    }

    public string TaskStatusText
    {
        get => (string)GetValue(TaskStatusTextProperty);
        set => SetValue(TaskStatusTextProperty, value);
    }

    public ICommand? SettingsCommand
    {
        get => (ICommand?)GetValue(SettingsCommandProperty);
        set => SetValue(SettingsCommandProperty, value);
    }

    public ICommand? MinimizeCommand
    {
        get => (ICommand?)GetValue(MinimizeCommandProperty);
        set => SetValue(MinimizeCommandProperty, value);
    }

    public ICommand? ToggleMaximizeCommand
    {
        get => (ICommand?)GetValue(ToggleMaximizeCommandProperty);
        set => SetValue(ToggleMaximizeCommandProperty, value);
    }

    public ICommand? CloseCommand
    {
        get => (ICommand?)GetValue(CloseCommandProperty);
        set => SetValue(CloseCommandProperty, value);
    }

    public ICommand? EffectiveMinimizeCommand =>
        (ICommand?)GetValue(EffectiveMinimizeCommandProperty);

    public ICommand? EffectiveToggleMaximizeCommand =>
        (ICommand?)GetValue(EffectiveToggleMaximizeCommandProperty);

    public ICommand? EffectiveCloseCommand =>
        (ICommand?)GetValue(EffectiveCloseCommandProperty);

    public bool IsMaximizeAvailable =>
        (bool)GetValue(IsMaximizeAvailableProperty);

    public bool IsMaximized =>
        (bool)GetValue(IsMaximizedProperty);

    public TitleBar()
    {
        InitializeComponent();
        Loaded += TitleBar_Loaded;
        Unloaded += TitleBar_Unloaded;
    }

    private static void OnCommandChanged(
        DependencyObject d,
        DependencyPropertyChangedEventArgs e)
    {
        if (d is TitleBar titleBar)
        {
            titleBar.UpdateEffectiveCommands();
        }
    }

    private void TitleBar_Loaded(object sender, RoutedEventArgs e)
    {
        AttachToWindow();
    }

    private void TitleBar_Unloaded(object sender, RoutedEventArgs e)
    {
        DetachFromWindow();
    }

    private void AttachToWindow()
    {
        var window = Window.GetWindow(this);
        if (window is null || ReferenceEquals(_hostWindow, window))
        {
            if (window is not null)
            {
                RefreshState();
                UpdateEffectiveCommands();
            }

            return;
        }

        DetachFromWindow();

        _hostWindow = window;
        _hostWindow.StateChanged += HostWindow_StateChanged;
        _hostWindow.Closed += HostWindow_Closed;

        _resizeModeDescriptor = DependencyPropertyDescriptor.FromProperty(
            Window.ResizeModeProperty,
            typeof(Window));
        _resizeModeDescriptor?.AddValueChanged(_hostWindow, HostWindow_ResizeModeChanged);

        _defaultMinimizeCommand = new WindowRelayCommand(
            _hostWindow,
            w => w.WindowState = WindowState.Minimized);

        _defaultToggleMaximizeCommand = new WindowRelayCommand(
            _hostWindow,
            ToggleWindowState,
            IsWindowResizable);

        _defaultCloseCommand = new WindowRelayCommand(
            _hostWindow,
            w => w.Close());

        RefreshState();
        UpdateEffectiveCommands();
    }

    private void DetachFromWindow()
    {
        if (_hostWindow is null)
        {
            return;
        }

        _hostWindow.StateChanged -= HostWindow_StateChanged;
        _hostWindow.Closed -= HostWindow_Closed;
        _resizeModeDescriptor?.RemoveValueChanged(_hostWindow, HostWindow_ResizeModeChanged);

        _hostWindow = null;
        _resizeModeDescriptor = null;
        _defaultMinimizeCommand = null;
        _defaultToggleMaximizeCommand = null;
        _defaultCloseCommand = null;

        SetValue(EffectiveMinimizeCommandPropertyKey, null);
        SetValue(EffectiveToggleMaximizeCommandPropertyKey, null);
        SetValue(EffectiveCloseCommandPropertyKey, null);
        SetValue(IsMaximizeAvailablePropertyKey, false);
        SetValue(IsMaximizedPropertyKey, false);
    }

    private void HostWindow_StateChanged(object? sender, EventArgs e)
    {
        RefreshState();
    }

    private void HostWindow_ResizeModeChanged(object? sender, EventArgs e)
    {
        RefreshState();
    }

    private void HostWindow_Closed(object? sender, EventArgs e)
    {
        DetachFromWindow();
    }

    private void RefreshState()
    {
        if (_hostWindow is null)
        {
            return;
        }

        SetValue(IsMaximizeAvailablePropertyKey, IsWindowResizable(_hostWindow));
        SetValue(IsMaximizedPropertyKey, _hostWindow.WindowState == WindowState.Maximized);
        CommandManager.InvalidateRequerySuggested();
    }

    private void UpdateEffectiveCommands()
    {
        SetValue(EffectiveMinimizeCommandPropertyKey, MinimizeCommand ?? _defaultMinimizeCommand);
        SetValue(EffectiveToggleMaximizeCommandPropertyKey, ToggleMaximizeCommand ?? _defaultToggleMaximizeCommand);
        SetValue(EffectiveCloseCommandPropertyKey, CloseCommand ?? _defaultCloseCommand);
    }

    private static bool IsWindowResizable(Window window) =>
        window.ResizeMode is ResizeMode.CanResize or ResizeMode.CanResizeWithGrip;

    private static void ToggleWindowState(Window window)
    {
        window.WindowState = window.WindowState == WindowState.Maximized
            ? WindowState.Normal
            : WindowState.Maximized;
    }

    private sealed class WindowRelayCommand : ICommand
    {
        private readonly WeakReference<Window> _windowReference;
        private readonly Action<Window> _execute;
        private readonly Func<Window, bool>? _canExecute;

        public WindowRelayCommand(
            Window window,
            Action<Window> execute,
            Func<Window, bool>? canExecute = null)
        {
            _windowReference = new WeakReference<Window>(window);
            _execute = execute;
            _canExecute = canExecute;
        }

        public bool CanExecute(object? parameter) =>
            _windowReference.TryGetTarget(out var window)
            && (_canExecute?.Invoke(window) ?? true);

        public void Execute(object? parameter)
        {
            if (_windowReference.TryGetTarget(out var window) && CanExecute(parameter))
            {
                _execute(window);
            }
        }

        public event EventHandler? CanExecuteChanged
        {
            add => CommandManager.RequerySuggested += value;
            remove => CommandManager.RequerySuggested -= value;
        }
    }
}
