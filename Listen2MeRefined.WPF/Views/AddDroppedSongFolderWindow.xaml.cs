using System.Windows;

namespace Listen2MeRefined.WPF.Views;

public partial class AddDroppedSongFolderWindow : Window
{
    public bool DontAskAgainChecked => DontAskAgainCheckBox.IsChecked == true;

    public AddDroppedSongFolderWindow()
    {
        InitializeComponent();
    }

    public void SetFolderPath(string path)
    {
        FolderPathText.Text = path;
    }

    private void FooterBar_OnPrimaryClick(object sender, RoutedEventArgs e)
    {
        DialogResult = true;
    }

    private void FooterBar_OnSecondaryClick(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
    }
}
