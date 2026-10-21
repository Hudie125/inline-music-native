using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using InlineMusic.Models;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;
using PlaybackState = InlineMusic.Models.PlaybackState;

namespace InlineMusic.Audio;

public enum RepeatMode
{
    Off,
    One,
    All
}

public interface IAudioEngine : IDisposable
{
    PlaybackState State { get; }
    Track? CurrentTrack { get; }
    IReadOnlyList<Track> Queue { get; }
    int CurrentQueueIndex { get; }
    TimeSpan CurrentPosition { get; }
    TimeSpan Duration { get; }
    float Volume { get; set; }
    bool IsMuted { get; set; }
    bool Shuffle { get; set; }
    RepeatMode RepeatMode { get; set; }
    TenBandEqualizerSampleProvider? Equalizer { get; }
    double CrossfadeDurationSeconds { get; set; }
    bool EnableCrossfade { get; set; }

    event EventHandler<PlaybackState>? StateChanged;
    event EventHandler<Track?>? TrackChanged;
    event EventHandler<TimeSpan>? PositionChanged;
    event EventHandler<IReadOnlyList<Track>>? QueueChanged;
    event EventHandler<string>? ErrorOccurred;

    Task PlayTrackAsync(Track track, bool immediate = true);
    Task PlayQueueAsync(IEnumerable<Track> tracks, int startIndex = 0);
    Task PlayAsync();
    Task PauseAsync();
    Task TogglePlayPauseAsync();
    Task StopAsync();
    Task NextAsync();
    Task PreviousAsync();
    void Seek(TimeSpan targetPosition);
    void Enqueue(Track track);
    void EnqueueNext(Track track);
    void RemoveFromQueue(int index);
    void ClearQueue();
    void ReorderQueue(int oldIndex, int newIndex);
}

public class AudioEngine : IAudioEngine
{
    private IWavePlayer? _outputDevice;
    private AudioFileReader? _audioFileReader;
    private MediaFoundationReader? _mediaFoundationReader;
    private VolumeSampleProvider? _volumeProvider;
    private TenBandEqualizerSampleProvider? _equalizerProvider;
    private CrossfadeSampleProvider? _crossfadeProvider;

    private readonly List<Track> _queue = new();
    private readonly List<int> _shuffleIndices = new();
    private int _queueIndex = -1;
    private int _shufflePointer = 0;
    private readonly object _stateLock = new();
    private readonly System.Timers.Timer _positionTimer;
    private volatile int _currentRequestId = 0;

    private PlaybackState _state = PlaybackState.Idle;
    private float _volume = 1.0f;
    private bool _isMuted = false;
    private bool _shuffle = false;
    private RepeatMode _repeatMode = RepeatMode.Off;
    private double _crossfadeSeconds = 2.5;
    private bool _enableCrossfade = true;

    public PlaybackState State
    {
        get => _state;
        private set
        {
            if (_state != value)
            {
                _state = value;
                StateChanged?.Invoke(this, _state);
            }
        }
    }

    public Track? CurrentTrack => (_queueIndex >= 0 && _queueIndex < _queue.Count) ? _queue[_queueIndex] : null;
    public IReadOnlyList<Track> Queue => _queue.AsReadOnly();
    public int CurrentQueueIndex => _queueIndex;

    public TimeSpan CurrentPosition
    {
        get
        {
            if (_audioFileReader != null)
            {
                return _audioFileReader.CurrentTime;
            }
            if (_mediaFoundationReader != null)
            {
                return _mediaFoundationReader.CurrentTime;
            }
            return TimeSpan.Zero;
        }
    }

    public TimeSpan Duration
    {
        get
        {
            if (_audioFileReader != null)
            {
                return _audioFileReader.TotalTime;
            }
            if (_mediaFoundationReader != null)
            {
                return _mediaFoundationReader.TotalTime;
            }
            return CurrentTrack?.Duration ?? TimeSpan.Zero;
        }
    }

    public float Volume
    {
        get => _volume;
        set
        {
            _volume = Math.Clamp(value, 0f, 1f);
            if (_volumeProvider != null)
            {
                _volumeProvider.Volume = _isMuted ? 0f : _volume;
            }
        }
    }

    public bool IsMuted
    {
        get => _isMuted;
        set
        {
            _isMuted = value;
            if (_volumeProvider != null)
            {
                _volumeProvider.Volume = _isMuted ? 0f : _volume;
            }
        }
    }

    public bool Shuffle
    {
        get => _shuffle;
        set
        {
            if (_shuffle != value)
            {
                _shuffle = value;
                if (_shuffle)
                {
                    RebuildShuffleIndices();
                }
            }
        }
    }

    public RepeatMode RepeatMode
    {
        get => _repeatMode;
        set => _repeatMode = value;
    }

    public TenBandEqualizerSampleProvider? Equalizer => _equalizerProvider;

    public double CrossfadeDurationSeconds
    {
        get => _crossfadeSeconds;
        set
        {
            _crossfadeSeconds = Math.Clamp(value, 0.0, 12.0);
            if (_crossfadeProvider != null)
            {
                _crossfadeProvider.CrossfadeDurationSeconds = _crossfadeSeconds;
            }
        }
    }

    public bool EnableCrossfade
    {
        get => _enableCrossfade;
        set => _enableCrossfade = value;
    }

    public event EventHandler<PlaybackState>? StateChanged;
    public event EventHandler<Track?>? TrackChanged;
    public event EventHandler<TimeSpan>? PositionChanged;
    public event EventHandler<IReadOnlyList<Track>>? QueueChanged;
    public event EventHandler<string>? ErrorOccurred;

    public AudioEngine()
    {
        _positionTimer = new System.Timers.Timer(200);
        _positionTimer.Elapsed += (s, e) =>
        {
            if (State == PlaybackState.Playing)
            {
                PositionChanged?.Invoke(this, CurrentPosition);
            }
        };
        _positionTimer.Start();
    }

    public Task PlayQueueAsync(IEnumerable<Track> tracks, int startIndex = 0)
    {
        lock (_stateLock)
        {
            _queue.Clear();
            _queue.AddRange(tracks);
            _queueIndex = Math.Clamp(startIndex, 0, Math.Max(0, _queue.Count - 1));
            RebuildShuffleIndices();
        }

        QueueChanged?.Invoke(this, Queue);

        if (_queueIndex >= 0 && _queueIndex < _queue.Count)
        {
            return PlayTrackInternalAsync(_queue[_queueIndex]);
        }

        return Task.CompletedTask;
    }

    public Task PlayTrackAsync(Track track, bool immediate = true)
    {
        lock (_stateLock)
        {
            int existingIndex = _queue.FindIndex(t => t.Id == track.Id);
            if (existingIndex >= 0)
            {
                _queueIndex = existingIndex;
            }
            else
            {
                _queue.Add(track);
                _queueIndex = _queue.Count - 1;
                RebuildShuffleIndices();
                QueueChanged?.Invoke(this, Queue);
            }
        }

        return PlayTrackInternalAsync(track);
    }

    private async Task PlayTrackInternalAsync(Track track)
    {
        int requestId = Interlocked.Increment(ref _currentRequestId);
        State = PlaybackState.Loading;
        TrackChanged?.Invoke(this, track);

        try
        {
            await Task.Run(() =>
            {
                lock (_stateLock)
                {
                    if (requestId != _currentRequestId) return;

                    // Clean previous readers
                    _audioFileReader?.Dispose();
                    _audioFileReader = null;
                    _mediaFoundationReader?.Dispose();
                    _mediaFoundationReader = null;

                    if (!File.Exists(track.FilePath))
                    {
                        throw new FileNotFoundException($"Audio file not found: {track.FilePath}");
                    }

                    ISampleProvider newSampleProvider;

                    // Try standard AudioFileReader (WAV, MP3, AIFF)
                    try
                    {
                        _audioFileReader = new AudioFileReader(track.FilePath);
                        newSampleProvider = _audioFileReader;
                    }
                    catch
                    {
                        // Fallback to MediaFoundationReader for FLAC, M4A/AAC, ALAC, WMA
                        _mediaFoundationReader = new MediaFoundationReader(track.FilePath);
                        newSampleProvider = _mediaFoundationReader.ToSampleProvider();
                    }

                    // Format standardization: Ensure stereo float 44100/48000
                    if (newSampleProvider.WaveFormat.Channels == 1)
                    {
                        newSampleProvider = new MonoToStereoSampleProvider(newSampleProvider);
                    }

                    // Setup output device and DSP pipeline if not yet initialized
                    if (_outputDevice == null)
                    {
                        _crossfadeProvider = new CrossfadeSampleProvider(newSampleProvider.WaveFormat)
                        {
                            CrossfadeDurationSeconds = _crossfadeSeconds
                        };

                        _equalizerProvider = new TenBandEqualizerSampleProvider(_crossfadeProvider);
                        _volumeProvider = new VolumeSampleProvider(_equalizerProvider)
                        {
                            Volume = _isMuted ? 0f : _volume
                        };

                        try
                        {
                            var wasapi = new WasapiOut(NAudio.CoreAudioApi.AudioClientShareMode.Shared, 100);
                            wasapi.Init(_volumeProvider);
                            wasapi.PlaybackStopped += OnPlaybackStopped;
                            _outputDevice = wasapi;
                        }
                        catch
                        {
                            // Fallback to WaveOutEvent
                            var waveOut = new WaveOutEvent { DesiredLatency = 150 };
                            waveOut.Init(_volumeProvider);
                            waveOut.PlaybackStopped += OnPlaybackStopped;
                            _outputDevice = waveOut;
                        }
                    }

                    _crossfadeProvider!.TransitionTo(newSampleProvider, _enableCrossfade);
                    _outputDevice.Play();
                }
            });

            if (requestId == _currentRequestId)
            {
                State = PlaybackState.Playing;
            }
        }
        catch (Exception ex)
        {
            State = PlaybackState.Error;
            ErrorOccurred?.Invoke(this, $"Playback error: {ex.Message}");
            // Auto skip to next track on error
            _ = Task.Delay(1000).ContinueWith(_ => NextAsync());
        }
    }

    private void OnPlaybackStopped(object? sender, StoppedEventArgs e)
    {
        if (e.Exception != null)
        {
            State = PlaybackState.Error;
            ErrorOccurred?.Invoke(this, e.Exception.Message);
            return;
        }

        // Check if track completed naturally
        if (State == PlaybackState.Playing && CurrentPosition >= Duration - TimeSpan.FromMilliseconds(500))
        {
            _ = NextAsync();
        }
    }

    public Task PlayAsync()
    {
        if (_outputDevice != null && State == PlaybackState.Paused)
        {
            _outputDevice.Play();
            State = PlaybackState.Playing;
        }
        else if (CurrentTrack != null && State != PlaybackState.Playing)
        {
            return PlayTrackInternalAsync(CurrentTrack);
        }
        return Task.CompletedTask;
    }

    public Task PauseAsync()
    {
        if (_outputDevice != null && State == PlaybackState.Playing)
        {
            _outputDevice.Pause();
            State = PlaybackState.Paused;
        }
        return Task.CompletedTask;
    }

    public Task TogglePlayPauseAsync()
    {
        return State == PlaybackState.Playing ? PauseAsync() : PlayAsync();
    }

    public Task StopAsync()
    {
        if (_outputDevice != null)
        {
            _outputDevice.Stop();
            _crossfadeProvider?.Clear();
            State = PlaybackState.Idle;
        }
        return Task.CompletedTask;
    }

    public Task NextAsync()
    {
        lock (_stateLock)
        {
            if (_queue.Count == 0) return Task.CompletedTask;

            if (_repeatMode == RepeatMode.One)
            {
                Seek(TimeSpan.Zero);
                return PlayAsync();
            }

            if (_shuffle)
            {
                _shufflePointer++;
                if (_shufflePointer >= _shuffleIndices.Count)
                {
                    if (_repeatMode == RepeatMode.All)
                    {
                        RebuildShuffleIndices();
                        _shufflePointer = 0;
                    }
                    else
                    {
                        return StopAsync();
                    }
                }
                _queueIndex = _shuffleIndices[_shufflePointer];
            }
            else
            {
                _queueIndex++;
                if (_queueIndex >= _queue.Count)
                {
                    if (_repeatMode == RepeatMode.All)
                    {
                        _queueIndex = 0;
                    }
                    else
                    {
                        return StopAsync();
                    }
                }
            }
        }

        if (_queueIndex >= 0 && _queueIndex < _queue.Count)
        {
            return PlayTrackInternalAsync(_queue[_queueIndex]);
        }
        return Task.CompletedTask;
    }

    public Task PreviousAsync()
    {
        lock (_stateLock)
        {
            if (_queue.Count == 0) return Task.CompletedTask;

            // If more than 3 seconds in, restart track
            if (CurrentPosition > TimeSpan.FromSeconds(3))
            {
                Seek(TimeSpan.Zero);
                return Task.CompletedTask;
            }

            if (_shuffle)
            {
                _shufflePointer = Math.Max(0, _shufflePointer - 1);
                _queueIndex = _shuffleIndices[_shufflePointer];
            }
            else
            {
                _queueIndex--;
                if (_queueIndex < 0)
                {
                    _queueIndex = _repeatMode == RepeatMode.All ? _queue.Count - 1 : 0;
                }
            }
        }

        if (_queueIndex >= 0 && _queueIndex < _queue.Count)
        {
            return PlayTrackInternalAsync(_queue[_queueIndex]);
        }
        return Task.CompletedTask;
    }

    public void Seek(TimeSpan targetPosition)
    {
        lock (_stateLock)
        {
            try
            {
                var previousState = State;
                State = PlaybackState.Seeking;

                if (_audioFileReader != null)
                {
                    _audioFileReader.CurrentTime = targetPosition;
                }
                else if (_mediaFoundationReader != null)
                {
                    _mediaFoundationReader.CurrentTime = targetPosition;
                }

                State = (previousState == PlaybackState.Paused) ? PlaybackState.Paused : PlaybackState.Playing;
                PositionChanged?.Invoke(this, targetPosition);
            }
            catch (Exception ex)
            {
                ErrorOccurred?.Invoke(this, $"Seek failed: {ex.Message}");
            }
        }
    }

    public void Enqueue(Track track)
    {
        lock (_stateLock)
        {
            _queue.Add(track);
            RebuildShuffleIndices();
        }
        QueueChanged?.Invoke(this, Queue);
    }

    public void EnqueueNext(Track track)
    {
        lock (_stateLock)
        {
            int insertIndex = _queueIndex + 1;
            if (insertIndex > _queue.Count) insertIndex = _queue.Count;
            _queue.Insert(insertIndex, track);
            RebuildShuffleIndices();
        }
        QueueChanged?.Invoke(this, Queue);
    }

    public void RemoveFromQueue(int index)
    {
        lock (_stateLock)
        {
            if (index < 0 || index >= _queue.Count) return;

            _queue.RemoveAt(index);
            if (index < _queueIndex)
            {
                _queueIndex--;
            }
            else if (index == _queueIndex)
            {
                if (_queue.Count == 0)
                {
                    _queueIndex = -1;
                    _ = StopAsync();
                }
                else
                {
                    _queueIndex = Math.Min(_queueIndex, _queue.Count - 1);
                    _ = PlayTrackInternalAsync(_queue[_queueIndex]);
                }
            }
            RebuildShuffleIndices();
        }
        QueueChanged?.Invoke(this, Queue);
    }

    public void ClearQueue()
    {
        lock (_stateLock)
        {
            _queue.Clear();
            _queueIndex = -1;
            _shuffleIndices.Clear();
            _ = StopAsync();
        }
        QueueChanged?.Invoke(this, Queue);
    }

    public void ReorderQueue(int oldIndex, int newIndex)
    {
        lock (_stateLock)
        {
            if (oldIndex < 0 || oldIndex >= _queue.Count || newIndex < 0 || newIndex >= _queue.Count) return;
            var item = _queue[oldIndex];
            _queue.RemoveAt(oldIndex);
            _queue.Insert(newIndex, item);

            if (_queueIndex == oldIndex)
            {
                _queueIndex = newIndex;
            }
            else if (oldIndex < _queueIndex && newIndex >= _queueIndex)
            {
                _queueIndex--;
            }
            else if (oldIndex > _queueIndex && newIndex <= _queueIndex)
            {
                _queueIndex++;
            }
            RebuildShuffleIndices();
        }
        QueueChanged?.Invoke(this, Queue);
    }

    private void RebuildShuffleIndices()
    {
        _shuffleIndices.Clear();
        for (int i = 0; i < _queue.Count; i++)
        {
            _shuffleIndices.Add(i);
        }

        // Fisher-Yates shuffle
        var rng = new Random();
        int n = _shuffleIndices.Count;
        while (n > 1)
        {
            n--;
            int k = rng.Next(n + 1);
            int value = _shuffleIndices[k];
            _shuffleIndices[k] = _shuffleIndices[n];
            _shuffleIndices[n] = value;
        }

        // Ensure current track is first in shuffle order
        if (_queueIndex >= 0)
        {
            _shuffleIndices.Remove(_queueIndex);
            _shuffleIndices.Insert(0, _queueIndex);
            _shufflePointer = 0;
        }
    }

    public void Dispose()
    {
        _positionTimer.Dispose();
        _outputDevice?.Stop();
        _outputDevice?.Dispose();
        _audioFileReader?.Dispose();
        _mediaFoundationReader?.Dispose();
    }
}
