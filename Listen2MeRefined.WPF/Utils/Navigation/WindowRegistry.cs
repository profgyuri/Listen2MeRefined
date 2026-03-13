using System.Collections.Concurrent;
using System.Collections.Generic;
using Listen2MeRefined.Application.Navigation.Windows;
using Listen2MeRefined.Application.ViewModels.Shells;

namespace Listen2MeRefined.WPF.Utils.Navigation;

public class WindowRegistry : IWindowRegistry
{
    private readonly Dictionary<Type, Type> _map = new();
    
    public void Register<TShellViewModel, TWindow>() where TShellViewModel : ShellViewModelBase where TWindow : class
    {
        if (!_map.TryAdd(typeof(TShellViewModel), typeof(TWindow)))
        {
            throw new InvalidOperationException(
                $"{typeof(TShellViewModel).Name} is already mapped to {_map[typeof(TShellViewModel)].Name}.");
        }
    }

    public Type Resolve<TShellViewModel>() where TShellViewModel : ShellViewModelBase
    {
        if (_map.TryGetValue(typeof(TShellViewModel), out var windowType))
            return windowType;

        throw new InvalidOperationException(
            $"No window registered for shell ViewModel {typeof(TShellViewModel).Name}.");
    }
}