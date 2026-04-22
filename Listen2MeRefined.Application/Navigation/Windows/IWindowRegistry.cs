using Listen2MeRefined.Application.ViewModels.Shells;

namespace Listen2MeRefined.Application.Navigation.Windows;

public interface IWindowRegistry
{
    void Register<TShellViewModel, TWindow>() where TShellViewModel : ShellViewModelBase
                                              where TWindow : class;
    Type Resolve<TShellViewModel>() where TShellViewModel : ShellViewModelBase;
}