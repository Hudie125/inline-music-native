using System;
using System.Collections.Generic;

namespace InlineMusic.Models;

public enum PlaybackState
{
    Idle,
    Loading,
    Ready,
    Playing,
    Paused,
    Buffering,
    Seeking,
    Error
}

public class Track
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Title { get; set; } = "Unknown Title";
    public string Artist { get; set; } = "Unknown Artist";
    public string Album { get; set; } = "Unknown Album";
    public TimeSpan Duration { get; set; } = TimeSpan.Zero;
    public int TrackNumber { get; set; }
    public int Year { get; set; }
    public string Genre { get; set; } = "Unknown Genre";
    public string FilePath { get; set; } = string.Empty;
    public string Format { get; set; } = "MP3";
    public int Bitrate { get; set; }
    public long FileSize { get; set; }
    public string? CoverPath { get; set; }
    public bool IsLiked { get; set; }
    public bool IsOffline { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public string FormattedDuration => Duration.TotalHours >= 1
        ? Duration.ToString(@"h\:mm\:ss")
        : Duration.ToString(@"m\:ss");
}

public class Playlist
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Name { get; set; } = "New Playlist";
    public string? Description { get; set; }
    public string? CoverPath { get; set; }
    public int TrackCount { get; set; }
    public TimeSpan TotalDuration { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public List<Track> Tracks { get; set; } = new();
}

public class HistoryItem
{
    public int Id { get; set; }
    public string TrackId { get; set; } = string.Empty;
    public Track? Track { get; set; }
    public DateTime PlayedAt { get; set; } = DateTime.UtcNow;
    public TimeSpan DurationListened { get; set; }
    public double CompletionRate { get; set; }
    public bool SkippedEarly { get; set; }
}

public class LyricsLine
{
    public TimeSpan Timestamp { get; set; }
    public TimeSpan Duration { get; set; }
    public string Text { get; set; } = string.Empty;
    public List<LyricsWord>? Words { get; set; }
}

public class LyricsWord
{
    public TimeSpan StartTime { get; set; }
    public TimeSpan EndTime { get; set; }
    public string Word { get; set; } = string.Empty;
}

public class EqualizerPreset
{
    public string Name { get; set; } = "Flat";
    public float[] BandGainsDb { get; set; } = new float[10];

    public static readonly float[] CenterFrequencies = new float[]
    {
        32f, 64f, 125f, 250f, 500f, 1000f, 2000f, 4000f, 8000f, 16000f
    };

    public static EqualizerPreset Flat => new()
    {
        Name = "Flat",
        BandGainsDb = new float[10] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 }
    };

    public static EqualizerPreset BassBoost => new()
    {
        Name = "Bass Boost",
        BandGainsDb = new float[10] { 6.5f, 5.5f, 4.0f, 2.0f, 0.5f, 0f, 0f, 0f, 0f, 0f }
    };

    public static EqualizerPreset TrebleBoost => new()
    {
        Name = "Treble Boost",
        BandGainsDb = new float[10] { 0f, 0f, 0f, 0f, 0.5f, 1.5f, 3.0f, 4.5f, 6.0f, 7.0f }
    };

    public static EqualizerPreset Vocal => new()
    {
        Name = "Vocal Clarity",
        BandGainsDb = new float[10] { -2.0f, -1.0f, 0f, 1.5f, 3.5f, 4.0f, 3.0f, 1.5f, 0f, -1.0f }
    };

    public static EqualizerPreset Electronic => new()
    {
        Name = "Electronic",
        BandGainsDb = new float[10] { 5.0f, 4.5f, 2.0f, 0f, -1.5f, 1.5f, 0f, 2.0f, 4.0f, 4.5f }
    };

    public static EqualizerPreset Rock => new()
    {
        Name = "Rock",
        BandGainsDb = new float[10] { 4.5f, 3.0f, 1.5f, 0.5f, -1.0f, -0.5f, 1.5f, 2.5f, 3.5f, 4.0f }
    };

    public static List<EqualizerPreset> DefaultPresets => new()
    {
        Flat,
        BassBoost,
        TrebleBoost,
        Vocal,
        Electronic,
        Rock
    };
}
