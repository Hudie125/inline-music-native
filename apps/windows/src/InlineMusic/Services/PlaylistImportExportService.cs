using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using InlineMusic.Data;
using InlineMusic.Models;

namespace InlineMusic.Services;

public interface IPlaylistImportExportService
{
    Task ExportToM3uAsync(Playlist playlist, string exportPath);
    Task<Playlist> ImportFromM3uAsync(string m3uPath);
    Task ExportToCsvAsync(IEnumerable<Track> tracks, string exportPath);
    Task<string> ExportBackupJsonAsync(string exportPath);
    Task RestoreBackupJsonAsync(string backupJsonPath);
}

public class PlaylistImportExportService : IPlaylistImportExportService
{
    private readonly IDatabaseService _databaseService;

    public PlaylistImportExportService(IDatabaseService databaseService)
    {
        _databaseService = databaseService;
    }

    public async Task ExportToM3uAsync(Playlist playlist, string exportPath)
    {
        var tracks = await _databaseService.GetPlaylistTracksAsync(playlist.Id);
        var sb = new StringBuilder();
        sb.AppendLine("#EXTM3U");
        sb.AppendLine($"#PLAYLIST:{playlist.Name}");

        foreach (var track in tracks)
        {
            int durationSeconds = (int)track.Duration.TotalSeconds;
            sb.AppendLine($"#EXTINF:{durationSeconds},{track.Artist} - {track.Title}");
            sb.AppendLine(track.FilePath);
        }

        await File.WriteAllTextAsync(exportPath, sb.ToString(), Encoding.UTF8);
    }

    public async Task<Playlist> ImportFromM3uAsync(string m3uPath)
    {
        if (!File.Exists(m3uPath))
        {
            throw new FileNotFoundException($"M3U file not found: {m3uPath}");
        }

        string playlistName = Path.GetFileNameWithoutExtension(m3uPath);
        var playlist = await _databaseService.CreatePlaylistAsync(playlistName);
        var lines = await File.ReadAllLinesAsync(m3uPath, Encoding.UTF8);

        string m3uDir = Path.GetDirectoryName(m3uPath) ?? "";
        var allLibraryTracks = await _databaseService.GetAllTracksAsync();
        var libraryPathMap = allLibraryTracks.ToDictionary(t => t.FilePath, t => t, StringComparer.OrdinalIgnoreCase);

        for (int i = 0; i < lines.Length; i++)
        {
            string line = lines[i].Trim();
            if (string.IsNullOrWhiteSpace(line) || line.StartsWith("#")) continue;

            string targetPath = line;
            if (!Path.IsPathRooted(targetPath))
            {
                targetPath = Path.GetFullPath(Path.Combine(m3uDir, targetPath));
            }

            if (libraryPathMap.TryGetValue(targetPath, out var track))
            {
                await _databaseService.AddTrackToPlaylistAsync(playlist.Id, track.Id);
            }
            else if (File.Exists(targetPath))
            {
                var newTrack = new Track
                {
                    Id = Guid.NewGuid().ToString(),
                    Title = Path.GetFileNameWithoutExtension(targetPath),
                    FilePath = targetPath,
                    Format = Path.GetExtension(targetPath).TrimStart('.').ToUpperInvariant(),
                    CreatedAt = DateTime.UtcNow
                };
                await _databaseService.SaveTrackAsync(newTrack);
                await _databaseService.AddTrackToPlaylistAsync(playlist.Id, newTrack.Id);
            }
        }

        return playlist;
    }

    public async Task ExportToCsvAsync(IEnumerable<Track> tracks, string exportPath)
    {
        var sb = new StringBuilder();
        sb.AppendLine("Title,Artist,Album,DurationSeconds,Genre,Year,FilePath");

        foreach (var track in tracks)
        {
            string escape(string s) => $"\"{s.Replace("\"", "\"\"")}\"";
            sb.AppendLine($"{escape(track.Title)},{escape(track.Artist)},{escape(track.Album)},{(int)track.Duration.TotalSeconds},{escape(track.Genre)},{track.Year},{escape(track.FilePath)}");
        }

        await File.WriteAllTextAsync(exportPath, sb.ToString(), Encoding.UTF8);
    }

    public async Task<string> ExportBackupJsonAsync(string exportPath)
    {
        var tracks = await _databaseService.GetAllTracksAsync();
        var playlists = await _databaseService.GetPlaylistsAsync();
        var history = await _databaseService.GetHistoryAsync(500);

        var backup = new
        {
            ExportedAt = DateTime.UtcNow,
            Version = "0.1.0",
            Tracks = tracks,
            Playlists = playlists,
            History = history
        };

        string json = JsonSerializer.Serialize(backup, new JsonSerializerOptions { WriteIndented = true });
        await File.WriteAllTextAsync(exportPath, json, Encoding.UTF8);
        return exportPath;
    }

    public async Task RestoreBackupJsonAsync(string backupJsonPath)
    {
        if (!File.Exists(backupJsonPath)) return;
        string json = await File.ReadAllTextAsync(backupJsonPath, Encoding.UTF8);
        using var doc = JsonDocument.Parse(json);

        if (doc.RootElement.TryGetProperty("Tracks", out var tracksElement))
        {
            var tracks = JsonSerializer.Deserialize<List<Track>>(tracksElement.GetRawText());
            if (tracks != null)
            {
                await _databaseService.SaveTracksBatchAsync(tracks);
            }
        }
    }
}
