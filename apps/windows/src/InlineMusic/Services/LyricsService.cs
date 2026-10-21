using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using InlineMusic.Data;
using InlineMusic.Models;

namespace InlineMusic.Services;

public class LrclibResponse
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("trackName")]
    public string? TrackName { get; set; }

    [JsonPropertyName("artistName")]
    public string? ArtistName { get; set; }

    [JsonPropertyName("albumName")]
    public string? AlbumName { get; set; }

    [JsonPropertyName("duration")]
    public double Duration { get; set; }

    [JsonPropertyName("instrumental")]
    public bool Instrumental { get; set; }

    [JsonPropertyName("plainLyrics")]
    public string? PlainLyrics { get; set; }

    [JsonPropertyName("syncedLyrics")]
    public string? SyncedLyrics { get; set; }
}

public interface ILyricsService
{
    Task<List<LyricsLine>> GetLyricsAsync(Track track);
    List<LyricsLine> ParseLrc(string lrcContent);
}

public class LyricsService : ILyricsService
{
    private static readonly HttpClient HttpClient = new()
    {
        Timeout = TimeSpan.FromSeconds(8)
    };

    private readonly IDatabaseService _databaseService;
    private static readonly Regex TimestampRegex = new(@"\[(\d{1,2}):(\d{2})(?:\.(\d{2,3}))?\]", RegexOptions.Compiled);

    public LyricsService(IDatabaseService databaseService)
    {
        _databaseService = databaseService;
    }

    public async Task<List<LyricsLine>> GetLyricsAsync(Track track)
    {
        // 1. Check local cache
        var cached = await _databaseService.GetCachedLyricsAsync(track.Id);
        if (cached.HasValue && !string.IsNullOrEmpty(cached.Value.synced))
        {
            return ParseLrc(cached.Value.synced!);
        }

        // 2. Query LRCLIB API
        try
        {
            string url = $"https://lrclib.net/api/get?track_name={Uri.EscapeDataString(track.Title)}&artist_name={Uri.EscapeDataString(track.Artist)}";
            if (track.Duration > TimeSpan.Zero)
            {
                url += $"&duration={Math.Round(track.Duration.TotalSeconds)}";
            }

            var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.Add("User-Agent", "inline-Music-Native/0.1.0");

            var response = await HttpClient.SendAsync(request);
            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<LrclibResponse>();
                if (result != null && !string.IsNullOrEmpty(result.SyncedLyrics))
                {
                    await _databaseService.CacheLyricsAsync(track.Id, result.PlainLyrics ?? "", result.SyncedLyrics, "lrclib");
                    return ParseLrc(result.SyncedLyrics);
                }
                else if (result != null && !string.IsNullOrEmpty(result.PlainLyrics))
                {
                    await _databaseService.CacheLyricsAsync(track.Id, result.PlainLyrics, "", "lrclib");
                    return result.PlainLyrics
                        .Split('\n', StringSplitOptions.RemoveEmptyEntries)
                        .Select(line => new LyricsLine { Text = line.Trim() })
                        .ToList();
                }
            }
        }
        catch
        {
            // Network unreachable or timeout; fallback smoothly
        }

        return new List<LyricsLine>();
    }

    public List<LyricsLine> ParseLrc(string lrcContent)
    {
        var rawLines = new List<(TimeSpan Timestamp, string Text)>();
        if (string.IsNullOrWhiteSpace(lrcContent)) return new List<LyricsLine>();

        var split = lrcContent.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);

        foreach (var line in split)
        {
            var matches = TimestampRegex.Matches(line);
            if (matches.Count == 0) continue;

            // Remove all timestamp tags to extract pure line text
            string text = TimestampRegex.Replace(line, "").Trim();

            foreach (Match match in matches)
            {
                int minutes = int.Parse(match.Groups[1].Value, CultureInfo.InvariantCulture);
                int seconds = int.Parse(match.Groups[2].Value, CultureInfo.InvariantCulture);
                int milliseconds = 0;

                if (match.Groups[3].Success)
                {
                    string msStr = match.Groups[3].Value;
                    if (msStr.Length == 2)
                    {
                        milliseconds = int.Parse(msStr, CultureInfo.InvariantCulture) * 10;
                    }
                    else if (msStr.Length == 3)
                    {
                        milliseconds = int.Parse(msStr, CultureInfo.InvariantCulture);
                    }
                }

                var timestamp = TimeSpan.FromMinutes(minutes) + TimeSpan.FromSeconds(seconds) + TimeSpan.FromMilliseconds(milliseconds);
                rawLines.Add((timestamp, text));
            }
        }

        // Sort by timestamp
        var sorted = rawLines.OrderBy(x => x.Timestamp).ToList();
        var result = new List<LyricsLine>();

        for (int i = 0; i < sorted.Count; i++)
        {
            TimeSpan duration = TimeSpan.FromSeconds(3); // Default duration if last line
            if (i < sorted.Count - 1)
            {
                duration = sorted[i + 1].Timestamp - sorted[i].Timestamp;
                if (duration > TimeSpan.FromSeconds(10) || duration <= TimeSpan.Zero)
                {
                    duration = TimeSpan.FromSeconds(3);
                }
            }

            result.Add(new LyricsLine
            {
                Timestamp = sorted[i].Timestamp,
                Duration = duration,
                Text = sorted[i].Text
            });
        }

        return result;
    }
}
