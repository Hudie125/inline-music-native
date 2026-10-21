using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using InlineMusic.Data;
using InlineMusic.Models;

namespace InlineMusic.Services;

public interface IWaveRecommendationEngine
{
    Task<List<Track>> GenerateWaveQueueAsync(int count = 25);
    Task<List<Track>> GetSimilarTracksAsync(Track referenceTrack, int count = 10);
}

public class WaveRecommendationEngine : IWaveRecommendationEngine
{
    private readonly IDatabaseService _databaseService;

    public WaveRecommendationEngine(IDatabaseService databaseService)
    {
        _databaseService = databaseService;
    }

    public async Task<List<Track>> GenerateWaveQueueAsync(int count = 25)
    {
        var allTracks = await _databaseService.GetAllTracksAsync();
        if (allTracks.Count == 0) return new List<Track>();
        if (allTracks.Count <= count) return allTracks.OrderBy(_ => Guid.NewGuid()).ToList();

        var history = await _databaseService.GetHistoryAsync(150);
        var topGenres = new HashSet<string>(await _databaseService.GetTopGenresAsync(5), StringComparer.OrdinalIgnoreCase);
        var topArtists = new HashSet<string>(await _databaseService.GetTopArtistsAsync(8), StringComparer.OrdinalIgnoreCase);

        var trackHistoryMap = history.GroupBy(h => h.TrackId)
            .ToDictionary(g => g.Key, g => g.ToList());

        var now = DateTime.UtcNow;
        var scoredTracks = new List<(Track Track, double Score)>();
        var random = new Random();

        foreach (var track in allTracks)
        {
            double score = 50.0; // Baseline neutral affinity

            // Liked bonus
            if (track.IsLiked)
            {
                score += 35.0;
            }

            // Genre and Artist affinity
            if (!string.IsNullOrEmpty(track.Genre) && topGenres.Contains(track.Genre))
            {
                score += 15.0;
            }
            if (!string.IsNullOrEmpty(track.Artist) && topArtists.Contains(track.Artist))
            {
                score += 20.0;
            }

            // History analysis
            if (trackHistoryMap.TryGetValue(track.Id, out var logs))
            {
                score += Math.Min(logs.Count * 4.0, 24.0); // Play count boost capped

                double avgCompletion = logs.Average(l => l.CompletionRate);
                if (avgCompletion >= 0.8)
                {
                    score += 20.0; // Frequently completed
                }
                else if (avgCompletion < 0.25)
                {
                    score -= 30.0; // Frequently skipped early
                }

                var mostRecent = logs.Max(l => l.PlayedAt);
                var timeSince = now - mostRecent;
                if (timeSince.TotalHours < 2.0)
                {
                    score -= 45.0; // Freshness decay: prevent loop repetition
                }
                else if (timeSince.TotalHours < 6.0)
                {
                    score -= 20.0;
                }
            }

            // Add small controlled variance to promote music discovery
            score += (random.NextDouble() * 14.0) - 7.0;

            scoredTracks.Add((track, score));
        }

        // Return top scored tracks shuffled slightly for natural flow
        var selected = scoredTracks
            .OrderByDescending(x => x.Score)
            .Take(count * 2)
            .OrderBy(_ => Guid.NewGuid())
            .Take(count)
            .Select(x => x.Track)
            .ToList();

        return selected;
    }

    public async Task<List<Track>> GetSimilarTracksAsync(Track referenceTrack, int count = 10)
    {
        var allTracks = await _databaseService.GetAllTracksAsync();
        return allTracks
            .Where(t => t.Id != referenceTrack.Id)
            .OrderByDescending(t =>
            {
                int matchScore = 0;
                if (string.Equals(t.Artist, referenceTrack.Artist, StringComparison.OrdinalIgnoreCase))
                    matchScore += 40;
                if (!string.IsNullOrEmpty(t.Genre) && string.Equals(t.Genre, referenceTrack.Genre, StringComparison.OrdinalIgnoreCase))
                    matchScore += 25;
                if (t.IsLiked) matchScore += 10;
                return matchScore;
            })
            .Take(count)
            .ToList();
    }
}
