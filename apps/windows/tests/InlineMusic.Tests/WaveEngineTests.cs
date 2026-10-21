using System;
using System.IO;
using System.Threading.Tasks;
using InlineMusic.Data;
using InlineMusic.Models;
using InlineMusic.Services;
using Xunit;

namespace InlineMusic.Tests;

public class WaveEngineTests
{
    [Fact]
    public async Task WaveEngine_PrioritizesLikedTracks()
    {
        string dbPath = Path.Combine(Path.GetTempPath(), $"wave_test_{Guid.NewGuid()}.db");
        var db = new DatabaseService(dbPath);
        await db.InitializeAsync();

        var likedTrack = new Track
        {
            Id = "liked-1",
            Title = "Loved Song",
            Artist = "Favorite Artist",
            FilePath = "test_loved.mp3",
            IsLiked = true
        };

        var normalTrack = new Track
        {
            Id = "normal-1",
            Title = "Normal Song",
            Artist = "Normal Artist",
            FilePath = "test_normal.mp3",
            IsLiked = false
        };

        await db.SaveTrackAsync(likedTrack);
        await db.SaveTrackAsync(normalTrack);

        var wave = new WaveRecommendationEngine(db);
        var queue = await wave.GenerateWaveQueueAsync(2);

        Assert.Equal(2, queue.Count);
        // Loved track should be first due to high score
        Assert.Equal("liked-1", queue[0].Id);

        try { File.Delete(dbPath); } catch { }
    }
}
