using System.Windows;
using System.Windows.Controls;
using InlineMusic.ViewModels;
using Microsoft.Win32;

namespace InlineMusic.Views;

public partial class HomeView : UserControl
{
    public HomeView()
    {
        InitializeComponent();
    }

    private async void ScanFolder_Click(object sender, RoutedEventArgs e)
    {
        if (DataContext is LibraryViewModel vm)
        {
            var dialog = new OpenFolderDialog
            {
                Title = "Select Music Directory to Scan"
            };

            if (dialog.ShowDialog() == true && !string.IsNullOrEmpty(dialog.FolderName))
            {
                await vm.ScanFolderAsync(dialog.FolderName);
            }
        }
    }
}
