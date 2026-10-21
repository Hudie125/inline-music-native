using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media.Animation;
using InlineMusic.Services;
using InlineMusic.ViewModels;
using Wpf.Ui.Controls;

namespace InlineMusic.Views;

public partial class MainWindow : FluentWindow
{
    private readonly MainViewModel _viewModel;
    private readonly ISystemMediaTransportService _smtcService;
    private readonly IGlobalHotkeyService _hotkeyService;
    private MiniPlayerWindow? _miniPlayerWindow;

    public MainWindow(
        MainViewModel viewModel,
        ISystemMediaTransportService smtcService,
        IGlobalHotkeyService hotkeyService)
    {
        InitializeComponent();
        _viewModel = viewModel;
        _smtcService = smtcService;
        _hotkeyService = hotkeyService;

        DataContext = _viewModel;
        Loaded += MainWindow_Loaded;
    }

    private async void MainWindow_Loaded(object sender, RoutedEventArgs e)
    {
        var handle = new WindowInteropHelper(this).Handle;

        // 1. Initialize SMTC and Hotkeys
        _smtcService.Initialize(handle);
        _hotkeyService.Initialize(handle);

        _hotkeyService.PlayPauseTriggered += () => _viewModel.TogglePlayPauseCommand.Execute(null);
        _hotkeyService.NextTriggered += () => _viewModel.NextCommand.Execute(null);
        _hotkeyService.PreviousTriggered += () => _viewModel.PreviousCommand.Execute(null);
        _hotkeyService.ToggleMiniPlayerTriggered += ToggleMiniPlayer;

        _viewModel.PropertyChanged += (s, args) =>
        {
            if (args.PropertyName == nameof(MainViewModel.IsMiniPlayerOpen))
            {
                if (_viewModel.IsMiniPlayerOpen) ShowMiniPlayer();
                else _miniPlayerWindow?.Hide();
            }
            else if (args.PropertyName == nameof(MainViewModel.IsFullPlayerOpen))
            {
                _viewModel.CurrentView = _viewModel.IsFullPlayerOpen ? _viewModel.PlayerVM : _viewModel.LibraryVM;
            }
        };

        // 2. Animate Welcome Screen (Section 4.6 specification)
        await AnimateWelcomeSequenceAsync();
    }

    private async Task AnimateWelcomeSequenceAsync()
    {
        try
        {
            // Emerge 'welcome' smoothly
            var fadeIn = new DoubleAnimation(0.0, 1.0, TimeSpan.FromMilliseconds(500));
            WelcomeText.BeginAnimation(OpacityProperty, fadeIn);
            await Task.Delay(800);

            // Transform into 'inline'
            WelcomeText.Text = "inline";
            await Task.Delay(600);

            // Fade out overlay
            var fadeOut = new DoubleAnimation(1.0, 0.0, TimeSpan.FromMilliseconds(400));
            fadeOut.Completed += (s, e) => WelcomeOverlay.Visibility = Visibility.Collapsed;
            WelcomeOverlay.BeginAnimation(OpacityProperty, fadeOut);
        }
        catch
        {
            WelcomeOverlay.Visibility = Visibility.Collapsed;
        }
    }

    private void EnterApp_Click(object sender, RoutedEventArgs e)
    {
        WelcomeOverlay.Visibility = Visibility.Collapsed;
    }

    private void ToggleMiniPlayer()
    {
        _viewModel.IsMiniPlayerOpen = !_viewModel.IsMiniPlayerOpen;
    }

    private void ShowMiniPlayer()
    {
        if (_miniPlayerWindow == null)
        {
            _miniPlayerWindow = new MiniPlayerWindow
            {
                DataContext = _viewModel
            };
        }
        _miniPlayerWindow.Show();
        _miniPlayerWindow.Activate();
    }
}
