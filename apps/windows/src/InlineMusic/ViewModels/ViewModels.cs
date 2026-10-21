using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using InlineMusic.Audio;
using InlineMusic.Data;
using InlineMusic.Models;
using InlineMusic.Services;

namespace InlineMusic.ViewModels;

public class MainViewModel : ViewModelBase
{
    private readonly IAudioEngine _audioEngine;
    private readonly IDatabaseService _databaseService;
    private readonly ILibraryScannerService _scannerService;
    private readonly IWaveRecommendationEngine _waveEngine;
    private readonly ILyricsService _lyricsService;
    private readonly ISleepTimerService _sleepTimerService;
    private readonly IPlaylistImportExportService _importExportService;

    private ViewModelBase _currentView;
    private bool _isWelcomeActive = true;
    private bool _isMiniPlayerOpen = false;
    private bool _isFullPlayerOpen = false;

    // View models
    public PlayerViewModel PlayerVM { get; }
    public LibraryViewModel LibraryVM { get; }
    public EqualizerViewModel EqualizerVM { get; }
    public WaveViewModel WaveVM { get; }
    public SearchViewModel SearchVM { get; }
    public SettingsViewModel SettingsVM { get; }

    public ViewModelBase CurrentView
    {
        get => _currentView;
        set => SetProperty(ref _currentView, value);
    }

    public bool IsWelcomeActive
    {
        get => _isWelcomeActive;
        set => SetProperty(ref _isWelcomeActive, value);
    }

    public bool IsMiniPlayerOpen
    {
        get => _isMiniPlayerOpen;
        set => SetProperty(ref _isMiniPlayerOpen, value);
    }

    public bool IsFullPlayerOpen
    {
        get => _isFullPlayerOpen;
        set => SetProperty(ref _isFullPlayerOpen, value);
    }

    // Playback state mirror properties
    public Track? CurrentTrack => _audioEngine.CurrentTrack;
    public bool IsPlaying => _audioEngine.State == PlaybackState.Playing;
    public PlaybackState PlaybackState => _audioEngine.State;
    public TimeSpan CurrentPosition => _audioEngine.CurrentPosition;
    public TimeSpan Duration => _audioEngine.Duration;
    public float Volume
    {
        get => _audioEngine.Volume;
        set
        {
            _audioEngine.Volume = value;
            OnPropertyChanged();
        }
    }

    public double ProgressFraction
    {
        get
        {
            if (Duration.TotalSeconds <= 0) return 0.0;
            return Math.Clamp(CurrentPosition.TotalSeconds / Duration.TotalSeconds, 0.0, 1.0);
        }
    }

    // Commands
    public ICommand NavigateHomeCommand { get; }
    public ICommand NavigateLibraryCommand { get; }
    public ICommand NavigateSearchCommand { get; }
    public ICommand NavigateEqualizerCommand { get; }
    public ICommand NavigateWaveCommand { get; }
    public ICommand NavigateSettingsCommand { get; }
    public ICommand TogglePlayPauseCommand { get; }
    public ICommand NextCommand { get; }
    public ICommand PreviousCommand { get; }
    public ICommand ToggleLikeCommand { get; }
    public ICommand ToggleMiniPlayerCommand { get; }
    public ICommand ToggleFullPlayerCommand { get; }
    public ICommand DismissWelcomeCommand { get; }

    public MainViewModel(
        IAudioEngine audioEngine,
        IDatabaseService databaseService,
        ILibraryScannerService scannerService,
        IWaveRecommendationEngine waveEngine,
        ILyricsService lyricsService,
        ISleepTimerService sleepTimerService,
        IPlaylistImportExportService importExportService)
    {
        _audioEngine = audioEngine;
        _databaseService = databaseService;
        _scannerService = scannerService;
        _waveEngine = waveEngine;
        _lyricsService = lyricsService;
        _sleepTimerService = sleepTimerService;
        _importExportService = importExportService;

        PlayerVM = new PlayerViewModel(_audioEngine, _lyricsService, _databaseService);
        LibraryVM = new LibraryViewModel(_databaseService, _scannerService, _audioEngine);
        EqualizerVM = new EqualizerViewModel(_audioEngine);
        WaveVM = new WaveViewModel(_waveEngine, _audioEngine);
        SearchVM = new SearchViewModel(_databaseService, _audioEngine);
        SettingsVM = new SettingsViewModel(_databaseService, _scannerService, _importExportService, _audioEngine, _sleepTimerService);

        _currentView = LibraryVM;

        // Hook audio engine events
        _audioEngine.TrackChanged += (s, t) =>
        {
            OnPropertyChanged(nameof(CurrentTrack));
            OnPropertyChanged(nameof(Duration));
        };
        _audioEngine.StateChanged += (s, st) =>
        {
            OnPropertyChanged(nameof(PlaybackState));
            OnPropertyChanged(nameof(IsPlaying));
        };
        _audioEngine.PositionChanged += (s, pos) =>
        {
            OnPropertyChanged(nameof(CurrentPosition));
            OnPropertyChanged(nameof(ProgressFraction));
        };

        // Commands
        NavigateHomeCommand = new RelayCommand(() => CurrentView = LibraryVM);
        NavigateLibraryCommand = new RelayCommand(() => CurrentView = LibraryVM);
        NavigateSearchCommand = new RelayCommand(() => CurrentView = SearchVM);
        NavigateEqualizerCommand = new RelayCommand(() => CurrentView = EqualizerVM);
        NavigateWaveCommand = new RelayCommand(() => CurrentView = WaveVM);
        NavigateSettingsCommand = new RelayCommand(() => CurrentView = SettingsVM);

        TogglePlayPauseCommand = new AsyncRelayCommand(() => _audioEngine.TogglePlayPauseAsync());
        NextCommand = new AsyncRelayCommand(() => _audioEngine.NextAsync());
        PreviousCommand = new AsyncRelayCommand(() => _audioEngine.PreviousAsync());

        ToggleLikeCommand = new AsyncRelayCommand(async () =>
        {
            if (CurrentTrack != null)
            {
                bool newLiked = await _databaseService.ToggleLikeAsync(CurrentTrack.Id);
                CurrentTrack.IsLiked = newLiked;
                OnPropertyChanged(nameof(CurrentTrack));
                await LibraryVM.RefreshLikedTracksAsync();
            }
        });

        ToggleMiniPlayerCommand = new RelayCommand(() => IsMiniPlayerOpen = !IsMiniPlayerOpen);
        ToggleFullPlayerCommand = new RelayCommand(() => IsFullPlayerOpen = !IsFullPlayerOpen);
        DismissWelcomeCommand = new RelayCommand(() => IsWelcomeActive = false);
    }

    public async Task InitializeAsync()
    {
        await _databaseService.InitializeAsync();
        await LibraryVM.LoadInitialDataAsync();
    }
}

public class PlayerViewModel : ViewModelBase
{
    private readonly IAudioEngine _audioEngine;
    private readonly ILyricsService _lyricsService;
    private readonly IDatabaseService _databaseService;

    private ObservableCollection<LyricsLine> _lyrics = new();
    private LyricsLine? _activeLyricsLine;

    public Track? CurrentTrack => _audioEngine.CurrentTrack;
    public bool IsPlaying => _audioEngine.State == PlaybackState.Playing;
    public TimeSpan CurrentPosition => _audioEngine.CurrentPosition;
    public TimeSpan Duration => _audioEngine.Duration;
    public ObservableCollection<Track> Queue { get; } = new();

    public ObservableCollection<LyricsLine> Lyrics
    {
        get => _lyrics;
        set => SetProperty(ref _lyrics, value);
    }

    public LyricsLine? ActiveLyricsLine
    {
        get => _activeLyricsLine;
        set => SetProperty(ref _activeLyricsLine, value);
    }

    public bool Shuffle
    {
        get => _audioEngine.Shuffle;
        set
        {
            _audioEngine.Shuffle = value;
            OnPropertyChanged();
        }
    }

    public RepeatMode RepeatMode
    {
        get => _audioEngine.RepeatMode;
        set
        {
            _audioEngine.RepeatMode = value;
            OnPropertyChanged();
        }
    }

    public ICommand PlayPauseCommand { get; }
    public ICommand NextCommand { get; }
    public ICommand PreviousCommand { get; }
    public ICommand ToggleShuffleCommand { get; }
    public ICommand CycleRepeatCommand { get; }

    public PlayerViewModel(IAudioEngine audioEngine, ILyricsService lyricsService, IDatabaseService databaseService)
    {
        _audioEngine = audioEngine;
        _lyricsService = lyricsService;
        _databaseService = databaseService;

        _audioEngine.TrackChanged += async (s, track) =>
        {
            OnPropertyChanged(nameof(CurrentTrack));
            OnPropertyChanged(nameof(Duration));
            UpdateQueue();
            if (track != null)
            {
                await LoadLyricsAsync(track);
            }
        };

        _audioEngine.StateChanged += (s, state) => OnPropertyChanged(nameof(IsPlaying));
        _audioEngine.PositionChanged += (s, pos) =>
        {
            OnPropertyChanged(nameof(CurrentPosition));
            UpdateActiveLyricsLine(pos);
        };
        _audioEngine.QueueChanged += (s, q) => UpdateQueue();

        PlayPauseCommand = new AsyncRelayCommand(() => _audioEngine.TogglePlayPauseAsync());
        NextCommand = new AsyncRelayCommand(() => _audioEngine.NextAsync());
        PreviousCommand = new AsyncRelayCommand(() => _audioEngine.PreviousAsync());
        ToggleShuffleCommand = new RelayCommand(() => Shuffle = !Shuffle);
        CycleRepeatCommand = new RelayCommand(() =>
        {
            RepeatMode = RepeatMode switch
            {
                RepeatMode.Off => RepeatMode.All,
                RepeatMode.All => RepeatMode.One,
                _ => RepeatMode.Off
            };
        });
    }

    private void UpdateQueue()
    {
        Queue.Clear();
        foreach (var track in _audioEngine.Queue)
        {
            Queue.Add(track);
        }
    }

    private async Task LoadLyricsAsync(Track track)
    {
        Lyrics.Clear();
        var lines = await _lyricsService.GetLyricsAsync(track);
        foreach (var line in lines)
        {
            Lyrics.Add(line);
        }
    }

    private void UpdateActiveLyricsLine(TimeSpan currentPos)
    {
        if (Lyrics.Count == 0) return;
        var matchingLine = Lyrics.LastOrDefault(l => l.Timestamp <= currentPos);
        if (matchingLine != null && matchingLine != ActiveLyricsLine)
        {
            ActiveLyricsLine = matchingLine;
        }
    }
}

public class LibraryViewModel : ViewModelBase
{
    private readonly IDatabaseService _databaseService;
    private readonly ILibraryScannerService _scannerService;
    private readonly IAudioEngine _audioEngine;

    private ObservableCollection<Track> _allTracks = new();
    private ObservableCollection<Track> _likedTracks = new();
    private ObservableCollection<Playlist> _playlists = new();
    private ObservableCollection<HistoryItem> _history = new();
    private bool _isScanning = false;
    private int _scannedCount = 0;
    private int _totalFilesCount = 0;

    public ObservableCollection<Track> AllTracks
    {
        get => _allTracks;
        set => SetProperty(ref _allTracks, value);
    }

    public ObservableCollection<Track> LikedTracks
    {
        get => _likedTracks;
        set => SetProperty(ref _likedTracks, value);
    }

    public ObservableCollection<Playlist> Playlists
    {
        get => _playlists;
        set => SetProperty(ref _playlists, value);
    }

    public ObservableCollection<HistoryItem> History
    {
        get => _history;
        set => SetProperty(ref _history, value);
    }

    public bool IsScanning
    {
        get => _isScanning;
        set => SetProperty(ref _isScanning, value);
    }

    public int ScannedCount
    {
        get => _scannedCount;
        set => SetProperty(ref _scannedCount, value);
    }

    public int TotalFilesCount
    {
        get => _totalFilesCount;
        set => SetProperty(ref _totalFilesCount, value);
    }

    public ICommand PlayTrackCommand { get; }
    public ICommand PlayAllCommand { get; }
    public ICommand CreatePlaylistCommand { get; }

    public LibraryViewModel(IDatabaseService databaseService, ILibraryScannerService scannerService, IAudioEngine audioEngine)
    {
        _databaseService = databaseService;
        _scannerService = scannerService;
        _audioEngine = audioEngine;

        _scannerService.ProgressChanged += (s, e) =>
        {
            ScannedCount = e.ScannedFiles;
            TotalFilesCount = e.TotalFiles;
        };

        PlayTrackCommand = new AsyncRelayCommand(async param =>
        {
            if (param is Track track)
            {
                await _audioEngine.PlayTrackAsync(track);
                await _databaseService.LogHistoryAsync(track.Id, TimeSpan.FromSeconds(1), 0.05, false);
            }
        });

        PlayAllCommand = new AsyncRelayCommand(async () =>
        {
            if (AllTracks.Count > 0)
            {
                await _audioEngine.PlayQueueAsync(AllTracks, 0);
            }
        });

        CreatePlaylistCommand = new AsyncRelayCommand(async () =>
        {
            var p = await _databaseService.CreatePlaylistAsync($"Playlist {Playlists.Count + 1}");
            Playlists.Add(p);
        });
    }

    public async Task LoadInitialDataAsync()
    {
        var tracks = await _databaseService.GetAllTracksAsync();
        AllTracks = new ObservableCollection<Track>(tracks);

        await RefreshLikedTracksAsync();
        await RefreshPlaylistsAsync();
        await RefreshHistoryAsync();
    }

    public async Task RefreshLikedTracksAsync()
    {
        var liked = await _databaseService.GetLikedTracksAsync();
        LikedTracks = new ObservableCollection<Track>(liked);
    }

    public async Task RefreshPlaylistsAsync()
    {
        var playlists = await _databaseService.GetPlaylistsAsync();
        Playlists = new ObservableCollection<Playlist>(playlists);
    }

    public async Task RefreshHistoryAsync()
    {
        var hist = await _databaseService.GetHistoryAsync(50);
        History = new ObservableCollection<HistoryItem>(hist);
    }

    public async Task ScanFolderAsync(string folderPath)
    {
        IsScanning = true;
        try
        {
            await _scannerService.ScanDirectoryAsync(folderPath);
            await LoadInitialDataAsync();
        }
        finally
        {
            IsScanning = false;
        }
    }
}

public class EqualizerViewModel : ViewModelBase
{
    private readonly IAudioEngine _audioEngine;
    private float[] _bandGains = new float[10];
    private bool _isEnabled = true;
    private EqualizerPreset _selectedPreset = EqualizerPreset.Flat;
    private double _crossfadeDuration = 2.5;

    public float[] BandGains
    {
        get => _bandGains;
        set => SetProperty(ref _bandGains, value);
    }

    public bool IsEnabled
    {
        get => _isEnabled;
        set
        {
            if (SetProperty(ref _isEnabled, value) && _audioEngine.Equalizer != null)
            {
                _audioEngine.Equalizer.IsEnabled = value;
            }
        }
    }

    public EqualizerPreset SelectedPreset
    {
        get => _selectedPreset;
        set
        {
            if (SetProperty(ref _selectedPreset, value))
            {
                ApplyPreset(value);
            }
        }
    }

    public double CrossfadeDuration
    {
        get => _crossfadeDuration;
        set
        {
            if (SetProperty(ref _crossfadeDuration, value))
            {
                _audioEngine.CrossfadeDurationSeconds = value;
            }
        }
    }

    public List<EqualizerPreset> Presets => EqualizerPreset.DefaultPresets;

    public ICommand ResetCommand { get; }

    public EqualizerViewModel(IAudioEngine audioEngine)
    {
        _audioEngine = audioEngine;
        _crossfadeDuration = _audioEngine.CrossfadeDurationSeconds;

        ResetCommand = new RelayCommand(() => SelectedPreset = EqualizerPreset.Flat);
    }

    public void SetBandGain(int index, float gainDb)
    {
        if (index < 0 || index >= _bandGains.Length) return;
        _bandGains[index] = gainDb;
        _audioEngine.Equalizer?.SetBandGain(index, gainDb);
        OnPropertyChanged(nameof(BandGains));
    }

    private void ApplyPreset(EqualizerPreset preset)
    {
        for (int i = 0; i < 10; i++)
        {
            _bandGains[i] = preset.BandGainsDb[i];
            _audioEngine.Equalizer?.SetBandGain(i, preset.BandGainsDb[i]);
        }
        OnPropertyChanged(nameof(BandGains));
    }
}

public class WaveViewModel : ViewModelBase
{
    private readonly IWaveRecommendationEngine _waveEngine;
    private readonly IAudioEngine _audioEngine;
    private ObservableCollection<Track> _waveTracks = new();
    private bool _isGenerating = false;

    public ObservableCollection<Track> WaveTracks
    {
        get => _waveTracks;
        set => SetProperty(ref _waveTracks, value);
    }

    public bool IsGenerating
    {
        get => _isGenerating;
        set => SetProperty(ref _isGenerating, value);
    }

    public ICommand LaunchWaveCommand { get; }
    public ICommand RefreshWaveCommand { get; }

    public WaveViewModel(IWaveRecommendationEngine waveEngine, IAudioEngine audioEngine)
    {
        _waveEngine = waveEngine;
        _audioEngine = audioEngine;

        LaunchWaveCommand = new AsyncRelayCommand(async () =>
        {
            await GenerateWaveAsync();
            if (WaveTracks.Count > 0)
            {
                await _audioEngine.PlayQueueAsync(WaveTracks, 0);
            }
        });

        RefreshWaveCommand = new AsyncRelayCommand(GenerateWaveAsync);
    }

    public async Task GenerateWaveAsync()
    {
        IsGenerating = true;
        try
        {
            var tracks = await _waveEngine.GenerateWaveQueueAsync(30);
            WaveTracks = new ObservableCollection<Track>(tracks);
        }
        finally
        {
            IsGenerating = false;
        }
    }
}

public class SearchViewModel : ViewModelBase
{
    private readonly IDatabaseService _databaseService;
    private readonly IAudioEngine _audioEngine;
    private string _searchQuery = string.Empty;
    private ObservableCollection<Track> _searchResults = new();

    public string SearchQuery
    {
        get => _searchQuery;
        set
        {
            if (SetProperty(ref _searchQuery, value))
            {
                _ = FilterAsync(value);
            }
        }
    }

    public ObservableCollection<Track> SearchResults
    {
        get => _searchResults;
        set => SetProperty(ref _searchResults, value);
    }

    public ICommand PlayTrackCommand { get; }

    public SearchViewModel(IDatabaseService databaseService, IAudioEngine audioEngine)
    {
        _databaseService = databaseService;
        _audioEngine = audioEngine;

        PlayTrackCommand = new AsyncRelayCommand(async param =>
        {
            if (param is Track track)
            {
                await _audioEngine.PlayTrackAsync(track);
            }
        });
    }

    private async Task FilterAsync(string query)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            SearchResults.Clear();
            return;
        }

        var allTracks = await _databaseService.GetAllTracksAsync();
        var q = query.Trim();
        var matched = allTracks.Where(t =>
            t.Title.Contains(q, StringComparison.OrdinalIgnoreCase) ||
            t.Artist.Contains(q, StringComparison.OrdinalIgnoreCase) ||
            t.Album.Contains(q, StringComparison.OrdinalIgnoreCase) ||
            t.Genre.Contains(q, StringComparison.OrdinalIgnoreCase));

        SearchResults = new ObservableCollection<Track>(matched);
    }
}

public class SettingsViewModel : ViewModelBase
{
    private readonly IDatabaseService _databaseService;
    private readonly ILibraryScannerService _scannerService;
    private readonly IPlaylistImportExportService _importExportService;
    private readonly IAudioEngine _audioEngine;
    private readonly ISleepTimerService _sleepTimerService;

    private string _cacheSizeText = "Calculated on demand";
    private int _sleepTimerMinutes = 30;

    public string CacheSizeText
    {
        get => _cacheSizeText;
        set => SetProperty(ref _cacheSizeText, value);
    }

    public int SleepTimerMinutes
    {
        get => _sleepTimerMinutes;
        set => SetProperty(ref _sleepTimerMinutes, value);
    }

    public bool IsSleepTimerActive => _sleepTimerService.IsActive;

    public ICommand ClearCacheCommand { get; }
    public ICommand ClearHistoryCommand { get; }
    public ICommand ExportLibraryCsvCommand { get; }
    public ICommand BackupLibraryCommand { get; }
    public ICommand StartSleepTimerCommand { get; }
    public ICommand CancelSleepTimerCommand { get; }

    public SettingsViewModel(
        IDatabaseService databaseService,
        ILibraryScannerService scannerService,
        IPlaylistImportExportService importExportService,
        IAudioEngine audioEngine,
        ISleepTimerService sleepTimerService)
    {
        _databaseService = databaseService;
        _scannerService = scannerService;
        _importExportService = importExportService;
        _audioEngine = audioEngine;
        _sleepTimerService = sleepTimerService;

        _sleepTimerService.Tick += (s, r) => OnPropertyChanged(nameof(IsSleepTimerActive));
        _sleepTimerService.TimerCompleted += (s, e) => OnPropertyChanged(nameof(IsSleepTimerActive));
        _sleepTimerService.TimerCancelled += (s, e) => OnPropertyChanged(nameof(IsSleepTimerActive));

        ClearCacheCommand = new RelayCommand(ClearCoversCache);
        ClearHistoryCommand = new AsyncRelayCommand(async () =>
        {
            await _databaseService.ClearHistoryAsync();
        });

        ExportLibraryCsvCommand = new AsyncRelayCommand(async () =>
        {
            string docs = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            string exportFile = Path.Combine(docs, "inline_music_library.csv");
            var tracks = await _databaseService.GetAllTracksAsync();
            await _importExportService.ExportToCsvAsync(tracks, exportFile);
        });

        BackupLibraryCommand = new AsyncRelayCommand(async () =>
        {
            string docs = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            string backupFile = Path.Combine(docs, "inline_music_backup.json");
            await _importExportService.ExportBackupJsonAsync(backupFile);
        });

        StartSleepTimerCommand = new RelayCommand(() =>
        {
            _sleepTimerService.StartMinutes(SleepTimerMinutes);
            OnPropertyChanged(nameof(IsSleepTimerActive));
        });

        CancelSleepTimerCommand = new RelayCommand(() =>
        {
            _sleepTimerService.Cancel();
            OnPropertyChanged(nameof(IsSleepTimerActive));
        });
    }

    private void ClearCoversCache()
    {
        try
        {
            string appData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            string coversDir = Path.Combine(appData, "inline-music", "covers");
            if (Directory.Exists(coversDir))
            {
                var files = Directory.GetFiles(coversDir);
                foreach (var f in files)
                {
                    try { File.Delete(f); } catch { }
                }
            }
            CacheSizeText = "0 MB";
        }
        catch
        {
            CacheSizeText = "Error clearing cache";
        }
    }
}
