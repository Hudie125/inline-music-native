using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using InlineMusic.Data;
using InlineMusic.Models;

namespace InlineMusic.Services;

public class ScanProgressEventArgs : EventArgs
{
    public int ScannedFiles { get; set; }
    public int TotalFiles { get; set; }
    public string CurrentFilePath { get; set; } = string.Empty;
}

public interface ILibraryScannerService
{
    event EventHandler<ScanProgressEventArgs>? ProgressChanged;
    Task<List<Track>> ScanDirectoryAsync(string directoryPath, CancellationToken cancellationToken = default);
}

public class LibraryScannerService : ILibraryScannerService
{
    private static readonly HashSet<string> SupportedExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".mp3", ".flac", ".wav", ".m4a", ".aac", ".ogg", ".opus", ".wma", ".aiff"
    };

    private readonly IDatabaseService _databaseService;
    private readonly string _coversDirectory;

    public event EventHandler<ScanProgressEventArgs>? ProgressChanged;

    public LibraryScannerService(IDatabaseService databaseService)
    {
        _databaseService = databaseService;
        string appData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        _coversDirectory = Path.Combine(appData, "inline-music", "covers");
        Directory.CreateDirectory(_coversDirectory);
    }

    public async Task<List<Track>> ScanDirectoryAsync(string directoryPath, CancellationToken cancellationToken = default)
    {
        var discoveredTracks = new List<Track>();
        if (!Directory.Exists(directoryPath)) return discoveredTracks;

        var audioFiles = new List<string>();
        try
        {
            await Task.Run(() =>
            {
                var files = Directory.EnumerateFiles(directoryPath, "*.*", SearchOption.AllDirectories);
                foreach (var file in files)
                {
                    if (cancellationToken.IsCancellationRequested) break;
                    string ext = Path.GetExtension(file);
                    if (SupportedExtensions.Contains(ext))
                    {
                        audioFiles.Add(file);
                    }
                }
            }, cancellationToken);
        }
        catch
        {
            // Handle directory read permissions gracefully
        }

        int total = audioFiles.Count;
        int count = 0;

        foreach (var filePath in audioFiles)
        {
            if (cancellationToken.IsCancellationRequested) break;

            count++;
            ProgressChanged?.Invoke(this, new ScanProgressEventArgs
            {
                ScannedFiles = count,
                TotalFiles = total,
                CurrentFilePath = filePath
            });

            var track = await Task.Run(() => ExtractTrackMetadata(filePath), cancellationToken);
            if (track != null)
            {
                discoveredTracks.Add(track);
            }

            // Save in batches of 50
            if (discoveredTracks.Count % 50 == 0)
            {
                await _databaseService.SaveTracksBatchAsync(discoveredTracks.TakeLast(50));
            }
        }

        // Save remaining
        if (discoveredTracks.Count > 0)
        {
            await _databaseService.SaveTracksBatchAsync(discoveredTracks);
        }

        return discoveredTracks;
    }

    private Track? ExtractTrackMetadata(string filePath)
    {
        try
        {
            var fileInfo = new FileInfo(filePath);
            if (!fileInfo.Exists) return null;

            using var tagFile = TagLib.File.Create(filePath);

            string title = !string.IsNullOrWhiteSpace(tagFile.Tag.Title)
                ? tagFile.Tag.Title
                : Path.GetFileNameWithoutExtension(filePath);

            string artist = !string.IsNullOrWhiteSpace(tagFile.Tag.FirstPerformer)
                ? tagFile.Tag.FirstPerformer
                : (!string.IsNullOrWhiteSpace(tagFile.Tag.FirstAlbumArtist) ? tagFile.Tag.FirstAlbumArtist : "Unknown Artist");

            string album = !string.IsNullOrWhiteSpace(tagFile.Tag.Album)
                ? tagFile.Tag.Album
                : "Unknown Album";

            string genre = !string.IsNullOrWhiteSpace(tagFile.Tag.FirstGenre)
                ? tagFile.Tag.FirstGenre
                : "Unknown";

            int year = (int)tagFile.Tag.Year;
            int trackNumber = (int)tagFile.Tag.Track;
            TimeSpan duration = tagFile.Properties.Duration;
            int bitrate = tagFile.Properties.AudioBitrate;
            string format = Path.GetExtension(filePath).TrimStart('.').ToUpperInvariant();

            string? coverPath = null;
            if (tagFile.Tag.Pictures.Length > 0)
            {
                var pic = tagFile.Tag.Pictures[0];
                if (pic.Data.Data != null && pic.Data.Data.Length > 0)
                {
                    string hash = ComputeHash(pic.Data.Data);
                    string targetCoverPath = Path.Combine(_coversDirectory, $"{hash}.png");
                    if (!File.Exists(targetCoverPath))
                    {
                        File.WriteAllBytes(targetCoverPath, pic.Data.Data);
                    }
                    coverPath = targetCoverPath;
                }
            }

            return new Track
            {
                Id = Guid.NewGuid().ToString(),
                Title = title,
                Artist = artist,
                Album = album,
                Duration = duration,
                TrackNumber = trackNumber,
                Year = year,
                Genre = genre,
                FilePath = filePath,
                Format = format,
                Bitrate = bitrate,
                FileSize = fileInfo.Length,
                CoverPath = coverPath,
                IsOffline = true,
                CreatedAt = DateTime.UtcNow
            };
        }
        catch
        {
            // Fallback for files without TagLib parseable tags
            try
            {
                var fileInfo = new FileInfo(filePath);
                string fileName = Path.GetFileNameWithoutExtension(filePath);
                string artist = "Unknown Artist";
                string title = fileName;

                if (fileName.Contains(" - "))
                {
                    var parts = fileName.Split(new[] { " - " }, 2, StringSplitOptions.None);
                    artist = parts[0].Trim();
                    title = parts[1].Trim();
                }

                return new Track
                {
                    Id = Guid.NewGuid().ToString(),
                    Title = title,
                    Artist = artist,
                    Album = "Unknown Album",
                    Duration = TimeSpan.Zero,
                    FilePath = filePath,
                    Format = Path.GetExtension(filePath).TrimStart('.').ToUpperInvariant(),
                    FileSize = fileInfo.Length,
                    IsOffline = true,
                    CreatedAt = DateTime.UtcNow
                };
            }
            catch
            {
                return null;
            }
        }
    }

    private static string ComputeHash(byte[] data)
    {
        using var md5 = MD5.Create();
        byte[] hash = md5.ComputeHash(data);
        var sb = new StringBuilder();
        foreach (byte b in hash)
        {
            sb.Append(b.ToString("x2"));
        }
        return sb.ToString();
    }
}
