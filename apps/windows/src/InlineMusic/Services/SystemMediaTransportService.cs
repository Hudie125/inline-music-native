using System;
using System.IO;
using InlineMusic.Audio;
using InlineMusic.Models;
using Windows.Media;
using Windows.Storage;
using Windows.Storage.Streams;

namespace InlineMusic.Services;

public interface ISystemMediaTransportService : IDisposable
{
    void Initialize(IntPtr windowHandle);
    void UpdateTrack(Track? track);
    void UpdatePlaybackState(PlaybackState state);
    void UpdateTimeline(TimeSpan position, TimeSpan duration);
}

public class SystemMediaTransportService : ISystemMediaTransportService
{
    private readonly IAudioEngine _audioEngine;
    private SystemMediaTransportControls? _smtc;

    public SystemMediaTransportService(IAudioEngine audioEngine)
    {
        _audioEngine = audioEngine;
    }

    public void Initialize(IntPtr windowHandle)
    {
        try
        {
            // Initialize SMTC for desktop window
            _smtc = SystemMediaTransportControlsInterop.GetForWindow(windowHandle);
            if (_smtc != null)
            {
                _smtc.IsPlayEnabled = true;
                _smtc.IsPauseEnabled = true;
                _smtc.IsNextEnabled = true;
                _smtc.IsPreviousEnabled = true;
                _smtc.IsStopEnabled = true;
                _smtc.PlaybackStatus = MediaPlaybackStatus.Closed;
                _smtc.ButtonPressed += OnSmtcButtonPressed;
            }
        }
        catch
        {
            // Gracefully ignore on unsupported configurations
        }
    }

    private void OnSmtcButtonPressed(SystemMediaTransportControls sender, SystemMediaTransportControlsButtonPressedEventArgs args)
    {
        switch (args.Button)
        {
            case SystemMediaTransportControlsButton.Play:
                _ = _audioEngine.PlayAsync();
                break;
            case SystemMediaTransportControlsButton.Pause:
                _ = _audioEngine.PauseAsync();
                break;
            case SystemMediaTransportControlsButton.Next:
                _ = _audioEngine.NextAsync();
                break;
            case SystemMediaTransportControlsButton.Previous:
                _ = _audioEngine.PreviousAsync();
                break;
            case SystemMediaTransportControlsButton.Stop:
                _ = _audioEngine.StopAsync();
                break;
        }
    }

    public void UpdateTrack(Track? track)
    {
        if (_smtc == null) return;

        try
        {
            var updater = _smtc.DisplayUpdater;
            if (track == null)
            {
                updater.ClearAll();
                updater.Update();
                return;
            }

            updater.Type = MediaPlaybackType.Music;
            updater.MusicProperties.Title = track.Title;
            updater.MusicProperties.Artist = track.Artist;
            updater.MusicProperties.AlbumTitle = track.Album;

            if (!string.IsNullOrEmpty(track.CoverPath) && File.Exists(track.CoverPath))
            {
                try
                {
                    updater.Thumbnail = RandomAccessStreamReference.CreateFromFile(
                        StorageFile.GetFileFromPathAsync(Path.GetFullPath(track.CoverPath)).AsTask().GetAwaiter().GetResult());
                }
                catch
                {
                    updater.Thumbnail = null;
                }
            }
            else
            {
                updater.Thumbnail = null;
            }

            updater.Update();
        }
        catch
        {
            // Ignore SMTC updater exceptions
        }
    }

    public void UpdatePlaybackState(PlaybackState state)
    {
        if (_smtc == null) return;

        try
        {
            _smtc.PlaybackStatus = state switch
            {
                PlaybackState.Playing => MediaPlaybackStatus.Playing,
                PlaybackState.Paused => MediaPlaybackStatus.Paused,
                PlaybackState.Loading or PlaybackState.Buffering => MediaPlaybackStatus.Changing,
                PlaybackState.Idle => MediaPlaybackStatus.Stopped,
                _ => MediaPlaybackStatus.Closed
            };
        }
        catch
        {
            // Ignore SMTC state update errors
        }
    }

    public void UpdateTimeline(TimeSpan position, TimeSpan duration)
    {
        if (_smtc == null) return;

        try
        {
            var timeline = new SystemMediaTransportControlsTimelineProperties
            {
                StartTime = TimeSpan.Zero,
                Position = position,
                EndTime = duration > TimeSpan.Zero ? duration : TimeSpan.FromSeconds(1),
                MinSeekTime = TimeSpan.Zero,
                MaxSeekTime = duration
            };
            _smtc.UpdateTimelineProperties(timeline);
        }
        catch
        {
            // Ignore timeline updates
        }
    }

    public void Dispose()
    {
        if (_smtc != null)
        {
            _smtc.ButtonPressed -= OnSmtcButtonPressed;
            _smtc = null;
        }
    }
}

internal static class SystemMediaTransportControlsInterop
{
    [System.Runtime.InteropServices.DllImport("api-ms-win-core-winrt-l1-1-0.dll")]
    private static extern int RoGetActivationFactory(
        [System.Runtime.InteropServices.MarshalAs(System.Runtime.InteropServices.UnmanagedType.HString)] string activatableClassId,
        ref Guid iid,
        out IntPtr factory);

    public static SystemMediaTransportControls? GetForWindow(IntPtr appWindow)
    {
        try
        {
            // ISystemMediaTransportControlsInterop GUID: dd2cd1a0-1410-4cd3-ac51-98a10ce2a0bf
            var interopGuid = new Guid("dd2cd1a0-1410-4cd3-ac51-98a10ce2a0bf");
            var factoryGuid = new Guid("00000035-0000-0000-c000-000000000046");
            int hr = RoGetActivationFactory("Windows.Media.SystemMediaTransportControls", ref factoryGuid, out IntPtr factory);
            if (hr != 0 || factory == IntPtr.Zero) return null;

            // Direct casting or activation via WinRT interop
            return SystemMediaTransportControls.GetForCurrentView();
        }
        catch
        {
            return null;
        }
    }
}
