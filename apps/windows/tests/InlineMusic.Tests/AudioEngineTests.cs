using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using InlineMusic.Audio;
using InlineMusic.Models;
using Xunit;

namespace InlineMusic.Tests;

public class AudioEngineTests
{
    [Fact]
    public void InitialState_IsIdle()
    {
        using var engine = new AudioEngine();
        Assert.Equal(PlaybackState.Idle, engine.State);
        Assert.Null(engine.CurrentTrack);
        Assert.Empty(engine.Queue);
    }

    [Fact]
    public void Volume_IsClampedBetweenZeroAndOne()
    {
        using var engine = new AudioEngine();
        engine.Volume = 1.5f;
        Assert.Equal(1.0f, engine.Volume);

        engine.Volume = -0.5f;
        Assert.Equal(0.0f, engine.Volume);
    }

    [Fact]
    public void Enqueue_AddsTracksToQueue()
    {
        using var engine = new AudioEngine();
        var track1 = new Track { Id = "1", Title = "Song 1" };
        var track2 = new Track { Id = "2", Title = "Song 2" };

        engine.Enqueue(track1);
        engine.Enqueue(track2);

        Assert.Equal(2, engine.Queue.Count);
        Assert.Equal("1", engine.Queue[0].Id);
        Assert.Equal("2", engine.Queue[1].Id);
    }

    [Fact]
    public void ReorderQueue_SwapsElementsCorrectly()
    {
        using var engine = new AudioEngine();
        var track1 = new Track { Id = "1", Title = "Song 1" };
        var track2 = new Track { Id = "2", Title = "Song 2" };
        var track3 = new Track { Id = "3", Title = "Song 3" };

        engine.Enqueue(track1);
        engine.Enqueue(track2);
        engine.Enqueue(track3);

        engine.ReorderQueue(0, 2);

        Assert.Equal("2", engine.Queue[0].Id);
        Assert.Equal("3", engine.Queue[1].Id);
        Assert.Equal("1", engine.Queue[2].Id);
    }

    [Fact]
    public void EnqueueNext_InsertsDirectlyAfterCurrent()
    {
        using var engine = new AudioEngine();
        var track1 = new Track { Id = "1", Title = "Song 1" };
        var track2 = new Track { Id = "2", Title = "Song 2" };
        var trackNext = new Track { Id = "NEXT", Title = "Next Song" };

        engine.Enqueue(track1);
        engine.Enqueue(track2);
        engine.EnqueueNext(trackNext);

        Assert.Equal("NEXT", engine.Queue[0].Id);
    }

    [Fact]
    public void Shuffle_ContainsSameElements()
    {
        using var engine = new AudioEngine();
        for (int i = 0; i < 10; i++)
        {
            engine.Enqueue(new Track { Id = i.ToString(), Title = $"Track {i}" });
        }

        engine.Shuffle = true;
        Assert.True(engine.Shuffle);
        Assert.Equal(10, engine.Queue.Count);
    }
}
