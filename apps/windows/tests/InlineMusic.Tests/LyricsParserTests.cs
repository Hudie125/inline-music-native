using System;
using InlineMusic.Data;
using InlineMusic.Services;
using Xunit;

namespace InlineMusic.Tests;

public class LyricsParserTests
{
    [Fact]
    public void ParseLrc_StandardTwoDigitCentiseconds_ParsesCorrectly()
    {
        var parser = new LyricsService(new DatabaseService(":memory:"));
        string lrc = @"
[00:12.50]First line of the song
[00:15.80]Second line of the song
";
        var lines = parser.ParseLrc(lrc);

        Assert.Equal(2, lines.Count);
        Assert.Equal("First line of the song", lines[0].Text);
        Assert.Equal(TimeSpan.FromSeconds(12.5), lines[0].Timestamp);
        Assert.Equal("Second line of the song", lines[1].Text);
        Assert.Equal(TimeSpan.FromSeconds(15.8), lines[1].Timestamp);
    }

    [Fact]
    public void ParseLrc_ThreeDigitMilliseconds_ParsesCorrectly()
    {
        var parser = new LyricsService(new DatabaseService(":memory:"));
        string lrc = "[01:05.250]Line with three digit milliseconds";
        var lines = parser.ParseLrc(lrc);

        Assert.Single(lines);
        Assert.Equal("Line with three digit milliseconds", lines[0].Text);
        Assert.Equal(TimeSpan.FromMinutes(1) + TimeSpan.FromMilliseconds(5250), lines[0].Timestamp);
    }

    [Fact]
    public void ParseLrc_MultiTimestampLine_ExpandsToMultipleLines()
    {
        var parser = new LyricsService(new DatabaseService(":memory:"));
        string lrc = "[00:10.00][00:20.00]Repeated chorus";
        var lines = parser.ParseLrc(lrc);

        Assert.Equal(2, lines.Count);
        Assert.Equal(TimeSpan.FromSeconds(10), lines[0].Timestamp);
        Assert.Equal(TimeSpan.FromSeconds(20), lines[1].Timestamp);
        Assert.Equal("Repeated chorus", lines[0].Text);
        Assert.Equal("Repeated chorus", lines[1].Text);
    }

    [Fact]
    public void ParseLrc_EmptyOrMalformedInput_ReturnsEmptyList()
    {
        var parser = new LyricsService(new DatabaseService(":memory:"));
        var lines1 = parser.ParseLrc("");
        var lines2 = parser.ParseLrc("This is just plain text without timestamps");

        Assert.Empty(lines1);
        Assert.Empty(lines2);
    }
}
