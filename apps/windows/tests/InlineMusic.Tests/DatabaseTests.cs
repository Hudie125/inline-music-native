using System;
using System.IO;
using System.Threading.Tasks;
using InlineMusic.Data;
using InlineMusic.Models;
using Xunit;

namespace InlineMusic.Tests;

public class DatabaseTests
{
    [Fact]
    public async Task Database_PerformsCrudOperationsSuccessfully()
    {
        string dbPath = Path.Combine(Path.GetTempPath(), $"db_test_{Guid.NewGuid()}.db");
        var db = new DatabaseService(dbPath);
        await db.InitializeAsync();

        // 1. Insert Track
        var track = new Track
        {
            Id = "trk-1",
            Title = "Electric Pulse",
            Artist = "SynthWave Band",
            Album = "Neon Horizons",
            Duration = TimeSpan.FromMinutes(3.5),
            FilePath = "C:\\Music\\pulse.flac",
            Format = "FLAC",
            Bitrate = 960,
            Genre = "Electronic"
        };
        await db.SaveTrackAsync(track);

        var retrieved = await db.GetTrackByIdAsync("trk-1");
        Assert.NotNull(retrieved);
        Assert.Equal("Electric Pulse", retrieved!.Title);
        Assert.Equal("SynthWave Band", retrieved.Artist);
        Assert.False(retrieved.IsLiked);

        // 2. Toggle Like
        bool liked = await db.ToggleLikeAsync("trk-1");
        Assert.True(liked);
        var likedTracks = await db.GetLikedTracksAsync();
        Assert.Single(likedTracks);
        Assert.Equal("trk-1", likedTracks[0].Id);

        // 3. Playlists
        var playlist = await db.CreatePlaylistAsync("Night Drives", "Best midnight tracks");
        Assert.NotNull(playlist);
        Assert.Equal("Night Drives", playlist.Name);

        await db.AddTrackToPlaylistAsync(playlist.Id, "trk-1");
        var playlistTracks = await db.GetPlaylistTracksAsync(playlist.Id);
        Assert.Single(playlistTracks);
        Assert.Equal("trk-1", playlistTracks[0].Id);

        // 4. History Logging
        await db.LogHistoryAsync("trk-1", TimeSpan.FromMinutes(3.5), 1.0, false);
        var history = await db.GetHistoryAsync(10);
        Assert.Single(history);
        Assert.Equal("trk-1", history[0].TrackId);
        Assert.Equal(1.0, history[0].CompletionRate);

        try { File.Delete(dbPath); } catch { }
    }
}
