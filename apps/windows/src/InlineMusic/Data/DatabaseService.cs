using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using InlineMusic.Models;
using Microsoft.Data.Sqlite;

namespace InlineMusic.Data;

public interface IDatabaseService
{
    Task InitializeAsync();
    Task<List<Track>> GetAllTracksAsync();
    Task<Track?> GetTrackByIdAsync(string trackId);
    Task SaveTrackAsync(Track track);
    Task SaveTracksBatchAsync(IEnumerable<Track> tracks);
    Task DeleteTrackAsync(string trackId);
    Task<bool> ToggleLikeAsync(string trackId);
    Task<List<Track>> GetLikedTracksAsync();
    Task<List<Playlist>> GetPlaylistsAsync();
    Task<Playlist> CreatePlaylistAsync(string name, string? description = null);
    Task DeletePlaylistAsync(string playlistId);
    Task AddTrackToPlaylistAsync(string playlistId, string trackId);
    Task RemoveTrackFromPlaylistAsync(string playlistId, string trackId);
    Task<List<Track>> GetPlaylistTracksAsync(string playlistId);
    Task LogHistoryAsync(string trackId, TimeSpan listenedDuration, double completionRate, bool skippedEarly);
    Task<List<HistoryItem>> GetHistoryAsync(int limit = 50);
    Task ClearHistoryAsync();
    Task CacheLyricsAsync(string trackId, string plain, string synced, string source);
    Task<(string? plain, string? synced)?> GetCachedLyricsAsync(string trackId);
    Task<List<string>> GetTopGenresAsync(int limit = 5);
    Task<List<string>> GetTopArtistsAsync(int limit = 5);
}

public class DatabaseService : IDatabaseService
{
    private readonly string _connectionString;

    public DatabaseService(string? customDbPath = null)
    {
        string dbPath;
        if (!string.IsNullOrEmpty(customDbPath))
        {
            dbPath = customDbPath;
        }
        else
        {
            string appData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            string appFolder = Path.Combine(appData, "inline-music");
            Directory.CreateDirectory(appFolder);
            dbPath = Path.Combine(appFolder, "inline_music.db");
        }

        _connectionString = new SqliteConnectionStringBuilder
        {
            DataSource = dbPath,
            Mode = SqliteOpenMode.ReadWriteCreate
        }.ToString();
    }

    public async Task InitializeAsync()
    {
        using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();

        string sql = @"
            CREATE TABLE IF NOT EXISTS tracks (
                id TEXT PRIMARY KEY,
                title TEXT NOT NULL,
                artist TEXT NOT NULL,
                album TEXT NOT NULL,
                duration_ms INTEGER NOT NULL,
                track_number INTEGER DEFAULT 0,
                year INTEGER DEFAULT 0,
                genre TEXT DEFAULT 'Unknown',
                file_path TEXT NOT NULL UNIQUE,
                format TEXT DEFAULT 'MP3',
                bitrate INTEGER DEFAULT 0,
                file_size INTEGER DEFAULT 0,
                cover_path TEXT,
                is_liked INTEGER DEFAULT 0,
                is_offline INTEGER DEFAULT 1,
                created_at TEXT NOT NULL
            );

            CREATE TABLE IF NOT EXISTS playlists (
                id TEXT PRIMARY KEY,
                name TEXT NOT NULL,
                description TEXT,
                cover_path TEXT,
                created_at TEXT NOT NULL,
                updated_at TEXT NOT NULL
            );

            CREATE TABLE IF NOT EXISTS playlist_tracks (
                playlist_id TEXT NOT NULL,
                track_id TEXT NOT NULL,
                position INTEGER NOT NULL,
                added_at TEXT NOT NULL,
                PRIMARY KEY (playlist_id, track_id),
                FOREIGN KEY (playlist_id) REFERENCES playlists(id) ON DELETE CASCADE,
                FOREIGN KEY (track_id) REFERENCES tracks(id) ON DELETE CASCADE
            );

            CREATE TABLE IF NOT EXISTS history (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                track_id TEXT NOT NULL,
                played_at TEXT NOT NULL,
                duration_listened_ms INTEGER NOT NULL,
                completion_rate REAL NOT NULL,
                skipped_early INTEGER NOT NULL,
                FOREIGN KEY (track_id) REFERENCES tracks(id) ON DELETE CASCADE
            );

            CREATE TABLE IF NOT EXISTS lyrics_cache (
                track_id TEXT PRIMARY KEY,
                plain_lyrics TEXT,
                synced_lyrics TEXT,
                source TEXT,
                cached_at TEXT NOT NULL,
                FOREIGN KEY (track_id) REFERENCES tracks(id) ON DELETE CASCADE
            );

            CREATE INDEX IF NOT EXISTS idx_tracks_artist ON tracks(artist);
            CREATE INDEX IF NOT EXISTS idx_tracks_album ON tracks(album);
            CREATE INDEX IF NOT EXISTS idx_tracks_genre ON tracks(genre);
            CREATE INDEX IF NOT EXISTS idx_history_played_at ON history(played_at);
        ";

        using var command = new SqliteCommand(sql, connection);
        await command.ExecuteNonQueryAsync();
    }

    public async Task<List<Track>> GetAllTracksAsync()
    {
        var tracks = new List<Track>();
        using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();

        string sql = "SELECT * FROM tracks ORDER BY artist, album, track_number, title";
        using var command = new SqliteCommand(sql, connection);
        using var reader = await command.ExecuteReaderAsync();

        while (await reader.ReadAsync())
        {
            tracks.Add(MapTrack(reader));
        }

        return tracks;
    }

    public async Task<Track?> GetTrackByIdAsync(string trackId)
    {
        using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();

        string sql = "SELECT * FROM tracks WHERE id = @id";
        using var command = new SqliteCommand(sql, connection);
        command.Parameters.AddWithValue("@id", trackId);
        using var reader = await command.ExecuteReaderAsync();

        if (await reader.ReadAsync())
        {
            return MapTrack(reader);
        }

        return null;
    }

    public async Task SaveTrackAsync(Track track)
    {
        using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();

        string sql = @"
            INSERT INTO tracks (id, title, artist, album, duration_ms, track_number, year, genre, file_path, format, bitrate, file_size, cover_path, is_liked, is_offline, created_at)
            VALUES (@id, @title, @artist, @album, @duration_ms, @track_number, @year, @genre, @file_path, @format, @bitrate, @file_size, @cover_path, @is_liked, @is_offline, @created_at)
            ON CONFLICT(file_path) DO UPDATE SET
                title = excluded.title,
                artist = excluded.artist,
                album = excluded.album,
                duration_ms = excluded.duration_ms,
                track_number = excluded.track_number,
                year = excluded.year,
                genre = excluded.genre,
                format = excluded.format,
                bitrate = excluded.bitrate,
                file_size = excluded.file_size,
                cover_path = excluded.cover_path;
        ";

        using var command = new SqliteCommand(sql, connection);
        BindTrackParameters(command, track);
        await command.ExecuteNonQueryAsync();
    }

    public async Task SaveTracksBatchAsync(IEnumerable<Track> tracks)
    {
        using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();
        using var transaction = connection.BeginTransaction();

        string sql = @"
            INSERT INTO tracks (id, title, artist, album, duration_ms, track_number, year, genre, file_path, format, bitrate, file_size, cover_path, is_liked, is_offline, created_at)
            VALUES (@id, @title, @artist, @album, @duration_ms, @track_number, @year, @genre, @file_path, @format, @bitrate, @file_size, @cover_path, @is_liked, @is_offline, @created_at)
            ON CONFLICT(file_path) DO UPDATE SET
                title = excluded.title,
                artist = excluded.artist,
                album = excluded.album,
                duration_ms = excluded.duration_ms,
                track_number = excluded.track_number,
                year = excluded.year,
                genre = excluded.genre,
                format = excluded.format,
                bitrate = excluded.bitrate,
                file_size = excluded.file_size,
                cover_path = excluded.cover_path;
        ";

        using var command = new SqliteCommand(sql, connection, transaction);

        foreach (var track in tracks)
        {
            command.Parameters.Clear();
            BindTrackParameters(command, track);
            await command.ExecuteNonQueryAsync();
        }

        await transaction.CommitAsync();
    }

    public async Task DeleteTrackAsync(string trackId)
    {
        using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();
        string sql = "DELETE FROM tracks WHERE id = @id";
        using var command = new SqliteCommand(sql, connection);
        command.Parameters.AddWithValue("@id", trackId);
        await command.ExecuteNonQueryAsync();
    }

    public async Task<bool> ToggleLikeAsync(string trackId)
    {
        using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();

        string checkSql = "SELECT is_liked FROM tracks WHERE id = @id";
        using var checkCmd = new SqliteCommand(checkSql, connection);
        checkCmd.Parameters.AddWithValue("@id", trackId);
        var result = await checkCmd.ExecuteScalarAsync();

        if (result == null) return false;

        bool currentLike = Convert.ToInt32(result) == 1;
        bool newLike = !currentLike;

        string updateSql = "UPDATE tracks SET is_liked = @is_liked WHERE id = @id";
        using var updateCmd = new SqliteCommand(updateSql, connection);
        updateCmd.Parameters.AddWithValue("@is_liked", newLike ? 1 : 0);
        updateCmd.Parameters.AddWithValue("@id", trackId);
        await updateCmd.ExecuteNonQueryAsync();

        return newLike;
    }

    public async Task<List<Track>> GetLikedTracksAsync()
    {
        var tracks = new List<Track>();
        using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();

        string sql = "SELECT * FROM tracks WHERE is_liked = 1 ORDER BY artist, title";
        using var command = new SqliteCommand(sql, connection);
        using var reader = await command.ExecuteReaderAsync();

        while (await reader.ReadAsync())
        {
            tracks.Add(MapTrack(reader));
        }

        return tracks;
    }

    public async Task<List<Playlist>> GetPlaylistsAsync()
    {
        var playlists = new List<Playlist>();
        using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();

        string sql = @"
            SELECT p.*, COUNT(pt.track_id) as track_count, COALESCE(SUM(t.duration_ms), 0) as total_duration_ms
            FROM playlists p
            LEFT JOIN playlist_tracks pt ON p.id = pt.playlist_id
            LEFT JOIN tracks t ON pt.track_id = t.id
            GROUP BY p.id
            ORDER BY p.name;
        ";

        using var command = new SqliteCommand(sql, connection);
        using var reader = await command.ExecuteReaderAsync();

        while (await reader.ReadAsync())
        {
            playlists.Add(new Playlist
            {
                Id = reader.GetString(reader.GetOrdinal("id")),
                Name = reader.GetString(reader.GetOrdinal("name")),
                Description = reader.IsDBNull(reader.GetOrdinal("description")) ? null : reader.GetString(reader.GetOrdinal("description")),
                CoverPath = reader.IsDBNull(reader.GetOrdinal("cover_path")) ? null : reader.GetString(reader.GetOrdinal("cover_path")),
                TrackCount = reader.GetInt32(reader.GetOrdinal("track_count")),
                TotalDuration = TimeSpan.FromMilliseconds(reader.GetInt64(reader.GetOrdinal("total_duration_ms"))),
                CreatedAt = DateTime.Parse(reader.GetString(reader.GetOrdinal("created_at"))),
                UpdatedAt = DateTime.Parse(reader.GetString(reader.GetOrdinal("updated_at")))
            });
        }

        return playlists;
    }

    public async Task<Playlist> CreatePlaylistAsync(string name, string? description = null)
    {
        var playlist = new Playlist
        {
            Id = Guid.NewGuid().ToString(),
            Name = name,
            Description = description,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();

        string sql = @"
            INSERT INTO playlists (id, name, description, cover_path, created_at, updated_at)
            VALUES (@id, @name, @description, @cover_path, @created_at, @updated_at);
        ";

        using var command = new SqliteCommand(sql, connection);
        command.Parameters.AddWithValue("@id", playlist.Id);
        command.Parameters.AddWithValue("@name", playlist.Name);
        command.Parameters.AddWithValue("@description", (object?)playlist.Description ?? DBNull.Value);
        command.Parameters.AddWithValue("@cover_path", (object?)playlist.CoverPath ?? DBNull.Value);
        command.Parameters.AddWithValue("@created_at", playlist.CreatedAt.ToString("O"));
        command.Parameters.AddWithValue("@updated_at", playlist.UpdatedAt.ToString("O"));

        await command.ExecuteNonQueryAsync();
        return playlist;
    }

    public async Task DeletePlaylistAsync(string playlistId)
    {
        using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();
        string sql = "DELETE FROM playlists WHERE id = @id";
        using var command = new SqliteCommand(sql, connection);
        command.Parameters.AddWithValue("@id", playlistId);
        await command.ExecuteNonQueryAsync();
    }

    public async Task AddTrackToPlaylistAsync(string playlistId, string trackId)
    {
        using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();

        string posSql = "SELECT COALESCE(MAX(position), 0) + 1 FROM playlist_tracks WHERE playlist_id = @playlist_id";
        using var posCmd = new SqliteCommand(posSql, connection);
        posCmd.Parameters.AddWithValue("@playlist_id", playlistId);
        int nextPos = Convert.ToInt32(await posCmd.ExecuteScalarAsync());

        string sql = @"
            INSERT INTO playlist_tracks (playlist_id, track_id, position, added_at)
            VALUES (@playlist_id, @track_id, @position, @added_at)
            ON CONFLICT(playlist_id, track_id) DO NOTHING;
        ";

        using var command = new SqliteCommand(sql, connection);
        command.Parameters.AddWithValue("@playlist_id", playlistId);
        command.Parameters.AddWithValue("@track_id", trackId);
        command.Parameters.AddWithValue("@position", nextPos);
        command.Parameters.AddWithValue("@added_at", DateTime.UtcNow.ToString("O"));
        await command.ExecuteNonQueryAsync();
    }

    public async Task RemoveTrackFromPlaylistAsync(string playlistId, string trackId)
    {
        using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();
        string sql = "DELETE FROM playlist_tracks WHERE playlist_id = @playlist_id AND track_id = @track_id";
        using var command = new SqliteCommand(sql, connection);
        command.Parameters.AddWithValue("@playlist_id", playlistId);
        command.Parameters.AddWithValue("@track_id", trackId);
        await command.ExecuteNonQueryAsync();
    }

    public async Task<List<Track>> GetPlaylistTracksAsync(string playlistId)
    {
        var tracks = new List<Track>();
        using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();

        string sql = @"
            SELECT t.*
            FROM tracks t
            INNER JOIN playlist_tracks pt ON t.id = pt.track_id
            WHERE pt.playlist_id = @playlist_id
            ORDER BY pt.position;
        ";

        using var command = new SqliteCommand(sql, connection);
        command.Parameters.AddWithValue("@playlist_id", playlistId);
        using var reader = await command.ExecuteReaderAsync();

        while (await reader.ReadAsync())
        {
            tracks.Add(MapTrack(reader));
        }

        return tracks;
    }

    public async Task LogHistoryAsync(string trackId, TimeSpan listenedDuration, double completionRate, bool skippedEarly)
    {
        using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();

        string sql = @"
            INSERT INTO history (track_id, played_at, duration_listened_ms, completion_rate, skipped_early)
            VALUES (@track_id, @played_at, @duration_listened_ms, @completion_rate, @skipped_early);
        ";

        using var command = new SqliteCommand(sql, connection);
        command.Parameters.AddWithValue("@track_id", trackId);
        command.Parameters.AddWithValue("@played_at", DateTime.UtcNow.ToString("O"));
        command.Parameters.AddWithValue("@duration_listened_ms", (long)listenedDuration.TotalMilliseconds);
        command.Parameters.AddWithValue("@completion_rate", completionRate);
        command.Parameters.AddWithValue("@skipped_early", skippedEarly ? 1 : 0);

        await command.ExecuteNonQueryAsync();
    }

    public async Task<List<HistoryItem>> GetHistoryAsync(int limit = 50)
    {
        var history = new List<HistoryItem>();
        using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();

        string sql = @"
            SELECT h.*, t.title, t.artist, t.album, t.duration_ms, t.cover_path, t.file_path, t.is_liked
            FROM history h
            LEFT JOIN tracks t ON h.track_id = t.id
            ORDER BY h.played_at DESC
            LIMIT @limit;
        ";

        using var command = new SqliteCommand(sql, connection);
        command.Parameters.AddWithValue("@limit", limit);
        using var reader = await command.ExecuteReaderAsync();

        while (await reader.ReadAsync())
        {
            var item = new HistoryItem
            {
                Id = reader.GetInt32(reader.GetOrdinal("id")),
                TrackId = reader.GetString(reader.GetOrdinal("track_id")),
                PlayedAt = DateTime.Parse(reader.GetString(reader.GetOrdinal("played_at"))),
                DurationListened = TimeSpan.FromMilliseconds(reader.GetInt64(reader.GetOrdinal("duration_listened_ms"))),
                CompletionRate = reader.GetDouble(reader.GetOrdinal("completion_rate")),
                SkippedEarly = reader.GetInt32(reader.GetOrdinal("skipped_early")) == 1
            };

            if (!reader.IsDBNull(reader.GetOrdinal("title")))
            {
                item.Track = new Track
                {
                    Id = item.TrackId,
                    Title = reader.GetString(reader.GetOrdinal("title")),
                    Artist = reader.GetString(reader.GetOrdinal("artist")),
                    Album = reader.GetString(reader.GetOrdinal("album")),
                    Duration = TimeSpan.FromMilliseconds(reader.GetInt64(reader.GetOrdinal("duration_ms"))),
                    CoverPath = reader.IsDBNull(reader.GetOrdinal("cover_path")) ? null : reader.GetString(reader.GetOrdinal("cover_path")),
                    FilePath = reader.GetString(reader.GetOrdinal("file_path")),
                    IsLiked = reader.GetInt32(reader.GetOrdinal("is_liked")) == 1
                };
            }

            history.Add(item);
        }

        return history;
    }

    public async Task ClearHistoryAsync()
    {
        using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();
        string sql = "DELETE FROM history";
        using var command = new SqliteCommand(sql, connection);
        await command.ExecuteNonQueryAsync();
    }

    public async Task CacheLyricsAsync(string trackId, string plain, string synced, string source)
    {
        using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();

        string sql = @"
            INSERT INTO lyrics_cache (track_id, plain_lyrics, synced_lyrics, source, cached_at)
            VALUES (@track_id, @plain_lyrics, @synced_lyrics, @source, @cached_at)
            ON CONFLICT(track_id) DO UPDATE SET
                plain_lyrics = excluded.plain_lyrics,
                synced_lyrics = excluded.synced_lyrics,
                source = excluded.source,
                cached_at = excluded.cached_at;
        ";

        using var command = new SqliteCommand(sql, connection);
        command.Parameters.AddWithValue("@track_id", trackId);
        command.Parameters.AddWithValue("@plain_lyrics", plain);
        command.Parameters.AddWithValue("@synced_lyrics", synced);
        command.Parameters.AddWithValue("@source", source);
        command.Parameters.AddWithValue("@cached_at", DateTime.UtcNow.ToString("O"));

        await command.ExecuteNonQueryAsync();
    }

    public async Task<(string? plain, string? synced)?> GetCachedLyricsAsync(string trackId)
    {
        using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();

        string sql = "SELECT plain_lyrics, synced_lyrics FROM lyrics_cache WHERE track_id = @track_id";
        using var command = new SqliteCommand(sql, connection);
        command.Parameters.AddWithValue("@track_id", trackId);
        using var reader = await command.ExecuteReaderAsync();

        if (await reader.ReadAsync())
        {
            string? plain = reader.IsDBNull(0) ? null : reader.GetString(0);
            string? synced = reader.IsDBNull(1) ? null : reader.GetString(1);
            return (plain, synced);
        }

        return null;
    }

    public async Task<List<string>> GetTopGenresAsync(int limit = 5)
    {
        var genres = new List<string>();
        using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();

        string sql = @"
            SELECT t.genre, COUNT(h.id) as play_count
            FROM tracks t
            LEFT JOIN history h ON t.id = h.track_id
            WHERE t.genre IS NOT NULL AND t.genre != 'Unknown' AND t.genre != ''
            GROUP BY t.genre
            ORDER BY (play_count * 2 + SUM(t.is_liked) * 5) DESC
            LIMIT @limit;
        ";

        using var command = new SqliteCommand(sql, connection);
        command.Parameters.AddWithValue("@limit", limit);
        using var reader = await command.ExecuteReaderAsync();

        while (await reader.ReadAsync())
        {
            genres.Add(reader.GetString(0));
        }

        return genres;
    }

    public async Task<List<string>> GetTopArtistsAsync(int limit = 5)
    {
        var artists = new List<string>();
        using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();

        string sql = @"
            SELECT t.artist, COUNT(h.id) as play_count
            FROM tracks t
            LEFT JOIN history h ON t.id = h.track_id
            WHERE t.artist IS NOT NULL AND t.artist != 'Unknown Artist' AND t.artist != ''
            GROUP BY t.artist
            ORDER BY (play_count * 2 + SUM(t.is_liked) * 5) DESC
            LIMIT @limit;
        ";

        using var command = new SqliteCommand(sql, connection);
        command.Parameters.AddWithValue("@limit", limit);
        using var reader = await command.ExecuteReaderAsync();

        while (await reader.ReadAsync())
        {
            artists.Add(reader.GetString(0));
        }

        return artists;
    }

    private static Track MapTrack(SqliteDataReader reader)
    {
        return new Track
        {
            Id = reader.GetString(reader.GetOrdinal("id")),
            Title = reader.GetString(reader.GetOrdinal("title")),
            Artist = reader.GetString(reader.GetOrdinal("artist")),
            Album = reader.GetString(reader.GetOrdinal("album")),
            Duration = TimeSpan.FromMilliseconds(reader.GetInt64(reader.GetOrdinal("duration_ms"))),
            TrackNumber = reader.GetInt32(reader.GetOrdinal("track_number")),
            Year = reader.GetInt32(reader.GetOrdinal("year")),
            Genre = reader.GetString(reader.GetOrdinal("genre")),
            FilePath = reader.GetString(reader.GetOrdinal("file_path")),
            Format = reader.GetString(reader.GetOrdinal("format")),
            Bitrate = reader.GetInt32(reader.GetOrdinal("bitrate")),
            FileSize = reader.GetInt64(reader.GetOrdinal("file_size")),
            CoverPath = reader.IsDBNull(reader.GetOrdinal("cover_path")) ? null : reader.GetString(reader.GetOrdinal("cover_path")),
            IsLiked = reader.GetInt32(reader.GetOrdinal("is_liked")) == 1,
            IsOffline = reader.GetInt32(reader.GetOrdinal("is_offline")) == 1,
            CreatedAt = DateTime.Parse(reader.GetString(reader.GetOrdinal("created_at")))
        };
    }

    private static void BindTrackParameters(SqliteCommand command, Track track)
    {
        command.Parameters.AddWithValue("@id", track.Id);
        command.Parameters.AddWithValue("@title", track.Title);
        command.Parameters.AddWithValue("@artist", track.Artist);
        command.Parameters.AddWithValue("@album", track.Album);
        command.Parameters.AddWithValue("@duration_ms", (long)track.Duration.TotalMilliseconds);
        command.Parameters.AddWithValue("@track_number", track.TrackNumber);
        command.Parameters.AddWithValue("@year", track.Year);
        command.Parameters.AddWithValue("@genre", track.Genre);
        command.Parameters.AddWithValue("@file_path", track.FilePath);
        command.Parameters.AddWithValue("@format", track.Format);
        command.Parameters.AddWithValue("@bitrate", track.Bitrate);
        command.Parameters.AddWithValue("@file_size", track.FileSize);
        command.Parameters.AddWithValue("@cover_path", (object?)track.CoverPath ?? DBNull.Value);
        command.Parameters.AddWithValue("@is_liked", track.IsLiked ? 1 : 0);
        command.Parameters.AddWithValue("@is_offline", track.IsOffline ? 1 : 0);
        command.Parameters.AddWithValue("@created_at", track.CreatedAt.ToString("O"));
    }
}
