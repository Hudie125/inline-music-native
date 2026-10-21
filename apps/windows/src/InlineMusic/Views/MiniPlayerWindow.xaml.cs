using System.Windows;
using System.Windows.Input;

namespace InlineMusic.Views;

public partial class MiniPlayerWindow : Window
{
    public MiniPlayerWindow()
    {
        InitializeComponent();
        Left = SystemParameters.WorkArea.Right - Width - 30;
        Top = SystemParameters.WorkArea.Bottom - Height - 30;
    }

    private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ButtonState == MouseButtonState.Pressed)
        {
            DragMove();
        }
    }

    private void Close_Click(object sender, RoutedEventArgs e)
    {
        Hide();
    }
}
