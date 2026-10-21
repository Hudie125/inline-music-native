using System.Windows;
using InlineMusic.Audio;
using InlineMusic.Data;
using InlineMusic.Services;
using InlineMusic.ViewModels;
using InlineMusic.Views;

namespace InlineMusic;

public partial class App : Application
{
    private IAudioEngine? _audioEngine;
    private IDatabaseService? _databaseService;
    private ILibraryScannerService? _scannerService;
    private IWaveRecommendationEngine? _waveEngine;
    private ILyricsService? _lyricsService;
    private ISleepTimerService? _sleepTimerService;
    private IPlaylistImportExportService? _importExportService;
    private ISystemMediaTransportService? _smtcService;
    private IGlobalHotkeyService? _hotkeyService;

    private async void Application_Startup(object sender, StartupEventArgs e)
    {
        // 1. Core Services Initialization
        _databaseService = new DatabaseService();
        _audioEngine = new AudioEngine();
        _scannerService = new LibraryScannerService(_databaseService);
        _waveEngine = new WaveRecommendationEngine(_databaseService);
        _lyricsService = new LyricsService(_databaseService);
        _sleepTimerService = new SleepTimerService(_audioEngine);
        _importExportService = new PlaylistImportExportService(_databaseService);
        _smtcService = new SystemMediaTransportService(_audioEngine);
        _hotkeyService = new GlobalHotkeyService();

        // 2. ViewModel Orchestrator
        var mainVM = new MainViewModel(
            _audioEngine,
            _databaseService,
            _scannerService,
            _waveEngine,
            _lyricsService,
            _sleepTimerService,
            _importExportService);

        await mainVM.InitializeAsync();

        // 3. Launch Main Window
        var mainWindow = new MainWindow(mainVM, _smtcService, _hotkeyService);
        mainWindow.Show();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _audioEngine?.Dispose();
        _sleepTimerService?.Dispose();
        _smtcService?.Dispose();
        _hotkeyService?.Dispose();
        base.OnExit(e);
    }
}
