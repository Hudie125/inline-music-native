using System.Windows;
using System.Windows.Controls;
using InlineMusic.ViewModels;

namespace InlineMusic.Views;

public partial class EqualizerView : UserControl
{
    public EqualizerView()
    {
        InitializeComponent();
    }

    private void Slider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (sender is Slider slider && slider.Tag is string tagStr && int.TryParse(tagStr, out int bandIndex))
        {
            if (DataContext is EqualizerViewModel vm)
            {
                vm.SetBandGain(bandIndex, (float)e.NewValue);
            }
        }
    }
}
