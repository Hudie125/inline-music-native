using System;
using System.Threading;
using System.Threading.Tasks;
using InlineMusic.Audio;

namespace InlineMusic.Services;

public enum SleepTimerMode
{
    Off,
    Minutes,
    EndOfTrack,
    EndOfQueue
}

public interface ISleepTimerService : IDisposable
{
    bool IsActive { get; }
    SleepTimerMode Mode { get; }
    TimeSpan RemainingTime { get; }

    event EventHandler<TimeSpan>? Tick;
    event EventHandler? TimerCompleted;
    event EventHandler? TimerCancelled;

    void StartMinutes(int minutes);
    void StartEndOfTrack();
    void StartEndOfQueue();
    void Cancel();
}

public class SleepTimerService : ISleepTimerService
{
    private readonly IAudioEngine _audioEngine;
    private System.Timers.Timer? _timer;
    private DateTime _targetTime;

    public bool IsActive { get; private set; }
    public SleepTimerMode Mode { get; private set; } = SleepTimerMode.Off;
    public TimeSpan RemainingTime => IsActive ? (_targetTime - DateTime.UtcNow) : TimeSpan.Zero;

    public event EventHandler<TimeSpan>? Tick;
    public event EventHandler? TimerCompleted;
    public event EventHandler? TimerCancelled;

    public SleepTimerService(IAudioEngine audioEngine)
    {
        _audioEngine = audioEngine;
        _audioEngine.TrackChanged += OnTrackChanged;
    }

    public void StartMinutes(int minutes)
    {
        Cancel();
        Mode = SleepTimerMode.Minutes;
        IsActive = true;
        _targetTime = DateTime.UtcNow.AddMinutes(minutes);

        _timer = new System.Timers.Timer(1000);
        _timer.Elapsed += (s, e) =>
        {
            var remaining = RemainingTime;
            if (remaining <= TimeSpan.Zero)
            {
                TriggerSleepStop();
            }
            else
            {
                Tick?.Invoke(this, remaining);
            }
        };
        _timer.Start();
    }

    public void StartEndOfTrack()
    {
        Cancel();
        Mode = SleepTimerMode.EndOfTrack;
        IsActive = true;
    }

    public void StartEndOfQueue()
    {
        Cancel();
        Mode = SleepTimerMode.EndOfQueue;
        IsActive = true;
    }

    private void OnTrackChanged(object? sender, Models.Track? track)
    {
        if (!IsActive) return;

        if (Mode == SleepTimerMode.EndOfTrack)
        {
            TriggerSleepStop();
        }
        else if (Mode == SleepTimerMode.EndOfQueue && _audioEngine.CurrentQueueIndex >= _audioEngine.Queue.Count - 1)
        {
            TriggerSleepStop();
        }
    }

    private void TriggerSleepStop()
    {
        Cancel();
        _ = _audioEngine.StopAsync();
        TimerCompleted?.Invoke(this, EventArgs.Empty);
    }

    public void Cancel()
    {
        if (_timer != null)
        {
            _timer.Stop();
            _timer.Dispose();
            _timer = null;
        }

        bool wasActive = IsActive;
        IsActive = false;
        Mode = SleepTimerMode.Off;

        if (wasActive)
        {
            TimerCancelled?.Invoke(this, EventArgs.Empty);
        }
    }

    public void Dispose()
    {
        _audioEngine.TrackChanged -= OnTrackChanged;
        Cancel();
    }
}
