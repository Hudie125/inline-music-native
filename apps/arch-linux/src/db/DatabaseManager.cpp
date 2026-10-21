#include "DatabaseManager.hpp"
#include <QStandardPaths>
#include <QDir>
#include <QUuid>
#include <QDateTime>

namespace inline_music {

DatabaseManager::DatabaseManager(const QString& customPath, QObject* parent)
    : QObject(parent)
{
    m_connectionName = QString("inline_music_db_%1").arg(QUuid::createUuid().toString());
    m_db = QSqlDatabase::addDatabase("QSQLITE", m_connectionName);

    QString path = customPath;
    if (path.isEmpty()) {
        QString dataDir = QStandardPaths::writableLocation(QStandardPaths::AppDataLocation);
        QDir().mkpath(dataDir);
        path = QDir(dataDir).filePath("inline_music.db");
    }

    m_db.setDatabaseName(path);
}

DatabaseManager::~DatabaseManager() {
    if (m_db.isOpen()) {
        m_db.close();
    }
    QSqlDatabase::removeDatabase(m_connectionName);
}

bool DatabaseManager::initialize() {
    if (!m_db.open()) {
        return false;
    }

    QSqlQuery q(m_db);
    QString createSql = R"(
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
            created_at TEXT NOT NULL
        );

        CREATE TABLE IF NOT EXISTS playlist_tracks (
            playlist_id TEXT NOT NULL,
            track_id TEXT NOT NULL,
            position INTEGER NOT NULL,
            added_at TEXT NOT NULL,
            PRIMARY KEY (playlist_id, track_id)
        );

        CREATE TABLE IF NOT EXISTS history (
            id INTEGER PRIMARY KEY AUTOINCREMENT,
            track_id TEXT NOT NULL,
            played_at TEXT NOT NULL,
            duration_listened_ms INTEGER NOT NULL,
            completion_rate REAL NOT NULL,
            skipped_early INTEGER NOT NULL
        );

        CREATE TABLE IF NOT EXISTS lyrics_cache (
            track_id TEXT PRIMARY KEY,
            plain_lyrics TEXT,
            synced_lyrics TEXT,
            cached_at TEXT NOT NULL
        );
    )";

    return q.exec(createSql);
}

QList<Track> DatabaseManager::getAllTracks() {
    QList<Track> list;
    QSqlQuery q("SELECT * FROM tracks ORDER BY artist, album, track_number, title", m_db);
    while (q.next()) {
        list.append(mapTrack(q));
    }
    return list;
}

std::optional<Track> DatabaseManager::getTrackById(const QString& id) {
    QSqlQuery q(m_db);
    q.prepare("SELECT * FROM tracks WHERE id = :id");
    q.bindValue(":id", id);
    if (q.exec() && q.next()) {
        return mapTrack(q);
    }
    return std::nullopt;
}

bool DatabaseManager::saveTrack(const Track& track) {
    QSqlQuery q(m_db);
    q.prepare(R"(
        INSERT INTO tracks (id, title, artist, album, duration_ms, track_number, year, genre, file_path, cover_path, is_liked, is_offline, created_at)
        VALUES (:id, :title, :artist, :album, :duration_ms, :track_number, :year, :genre, :file_path, :cover_path, :is_liked, :is_offline, :created_at)
        ON CONFLICT(file_path) DO UPDATE SET
            title = excluded.title,
            artist = excluded.artist,
            album = excluded.album,
            duration_ms = excluded.duration_ms,
            track_number = excluded.track_number,
            year = excluded.year,
            genre = excluded.genre,
            cover_path = excluded.cover_path
    )");

    q.bindValue(":id", track.id);
    q.bindValue(":title", track.title);
    q.bindValue(":artist", track.artist);
    q.bindValue(":album", track.album);
    q.bindValue(":duration_ms", track.durationMs);
    q.bindValue(":track_number", track.trackNumber);
    q.bindValue(":year", track.year);
    q.bindValue(":genre", track.genre);
    q.bindValue(":file_path", track.filePath);
    q.bindValue(":cover_path", track.coverPath);
    q.bindValue(":is_liked", track.isLiked ? 1 : 0);
    q.bindValue(":is_offline", track.isOffline ? 1 : 0);
    q.bindValue(":created_at", track.createdAt.toString(Qt::ISODate));

    return q.exec();
}

bool DatabaseManager::saveTracksBatch(const QList<Track>& tracks) {
    m_db.transaction();
    for (const auto& track : tracks) {
        saveTrack(track);
    }
    return m_db.commit();
}

bool DatabaseManager::toggleLike(const QString& trackId) {
    QSqlQuery q(m_db);
    q.prepare("UPDATE tracks SET is_liked = CASE WHEN is_liked = 1 THEN 0 ELSE 1 END WHERE id = :id");
    q.bindValue(":id", trackId);
    return q.exec();
}

QList<Track> DatabaseManager::getLikedTracks() {
    QList<Track> list;
    QSqlQuery q("SELECT * FROM tracks WHERE is_liked = 1 ORDER BY artist, title", m_db);
    while (q.next()) {
        list.append(mapTrack(q));
    }
    return list;
}

QList<Track> DatabaseManager::searchTracks(const QString& query) {
    QList<Track> list;
    QSqlQuery q(m_db);
    q.prepare("SELECT * FROM tracks WHERE title LIKE :q OR artist LIKE :q OR album LIKE :q");
    q.bindValue(":q", QString("%%1%").arg(query));
    if (q.exec()) {
        while (q.next()) {
            list.append(mapTrack(q));
        }
    }
    return list;
}

QList<Playlist> DatabaseManager::getPlaylists() {
    QList<Playlist> list;
    QSqlQuery q("SELECT * FROM playlists ORDER BY name", m_db);
    while (q.next()) {
        Playlist p;
        p.id = q.value("id").toString();
        p.name = q.value("name").toString();
        p.description = q.value("description").toString();
        p.coverPath = q.value("cover_path").toString();
        p.createdAt = QDateTime::fromString(q.value("created_at").toString(), Qt::ISODate);
        list.append(p);
    }
    return list;
}

Playlist DatabaseManager::createPlaylist(const QString& name, const QString& description) {
    Playlist p;
    p.id = QUuid::createUuid().toString();
    p.name = name;
    p.description = description;
    p.createdAt = QDateTime::currentDateTimeUtc();

    QSqlQuery q(m_db);
    q.prepare("INSERT INTO playlists (id, name, description, cover_path, created_at) VALUES (:id, :name, :description, :cover_path, :created_at)");
    q.bindValue(":id", p.id);
    q.bindValue(":name", p.name);
    q.bindValue(":description", p.description);
    q.bindValue(":cover_path", p.coverPath);
    q.bindValue(":created_at", p.createdAt.toString(Qt::ISODate));
    q.exec();

    return p;
}

bool DatabaseManager::deletePlaylist(const QString& playlistId) {
    QSqlQuery q(m_db);
    q.prepare("DELETE FROM playlists WHERE id = :id");
    q.bindValue(":id", playlistId);
    return q.exec();
}

bool DatabaseManager::addTrackToPlaylist(const QString& playlistId, const QString& trackId) {
    QSqlQuery q(m_db);
    q.prepare("INSERT INTO playlist_tracks (playlist_id, track_id, position, added_at) VALUES (:pId, :tId, 0, :now)");
    q.bindValue(":pId", playlistId);
    q.bindValue(":tId", trackId);
    q.bindValue(":now", QDateTime::currentDateTimeUtc().toString(Qt::ISODate));
    return q.exec();
}

QList<Track> DatabaseManager::getPlaylistTracks(const QString& playlistId) {
    QList<Track> list;
    QSqlQuery q(m_db);
    q.prepare("SELECT t.* FROM tracks t INNER JOIN playlist_tracks pt ON t.id = pt.track_id WHERE pt.playlist_id = :pId ORDER BY pt.position");
    q.bindValue(":pId", playlistId);
    if (q.exec()) {
        while (q.next()) {
            list.append(mapTrack(q));
        }
    }
    return list;
}

bool DatabaseManager::logHistory(const QString& trackId, qint64 durationMs, double completionRate, bool skipped) {
    QSqlQuery q(m_db);
    q.prepare("INSERT INTO history (track_id, played_at, duration_listened_ms, completion_rate, skipped_early) VALUES (:tId, :at, :dur, :comp, :skip)");
    q.bindValue(":tId", trackId);
    q.bindValue(":at", QDateTime::currentDateTimeUtc().toString(Qt::ISODate));
    q.bindValue(":dur", durationMs);
    q.bindValue(":comp", completionRate);
    q.bindValue(":skip", skipped ? 1 : 0);
    return q.exec();
}

QList<HistoryItem> DatabaseManager::getRecentHistory(int limit) {
    QList<HistoryItem> list;
    QSqlQuery q(m_db);
    q.prepare("SELECT * FROM history ORDER BY played_at DESC LIMIT :lim");
    q.bindValue(":lim", limit);
    if (q.exec()) {
        while (q.next()) {
            HistoryItem item;
            item.id = q.value("id").toLongLong();
            item.trackId = q.value("track_id").toString();
            item.playedAt = QDateTime::fromString(q.value("played_at").toString(), Qt::ISODate);
            item.durationListenedMs = q.value("duration_listened_ms").toLongLong();
            item.completionRate = q.value("completion_rate").toDouble();
            item.skippedEarly = q.value("skipped_early").toInt() == 1;
            list.append(item);
        }
    }
    return list;
}

QStringList DatabaseManager::getTopArtists(int limit) {
    QStringList artists;
    QSqlQuery q(m_db);
    q.prepare("SELECT t.artist, COUNT(h.id) as cnt FROM tracks t LEFT JOIN history h ON t.id = h.track_id GROUP BY t.artist ORDER BY cnt DESC LIMIT :lim");
    q.bindValue(":lim", limit);
    if (q.exec()) {
        while (q.next()) {
            artists.append(q.value("artist").toString());
        }
    }
    return artists;
}

QStringList DatabaseManager::getTopGenres(int limit) {
    QStringList genres;
    QSqlQuery q(m_db);
    q.prepare("SELECT t.genre, COUNT(h.id) as cnt FROM tracks t LEFT JOIN history h ON t.id = h.track_id WHERE t.genre != 'Unknown' GROUP BY t.genre ORDER BY cnt DESC LIMIT :lim");
    q.bindValue(":lim", limit);
    if (q.exec()) {
        while (q.next()) {
            genres.append(q.value("genre").toString());
        }
    }
    return genres;
}

bool DatabaseManager::cacheLyrics(const QString& trackId, const QString& plain, const QString& synced) {
    QSqlQuery q(m_db);
    q.prepare("INSERT INTO lyrics_cache (track_id, plain_lyrics, synced_lyrics, cached_at) VALUES (:id, :plain, :synced, :now) ON CONFLICT(track_id) DO UPDATE SET plain_lyrics = excluded.plain_lyrics, synced_lyrics = excluded.synced_lyrics");
    q.bindValue(":id", trackId);
    q.bindValue(":plain", plain);
    q.bindValue(":synced", synced);
    q.bindValue(":now", QDateTime::currentDateTimeUtc().toString(Qt::ISODate));
    return q.exec();
}

QString DatabaseManager::getCachedSyncedLyrics(const QString& trackId) {
    QSqlQuery q(m_db);
    q.prepare("SELECT synced_lyrics FROM lyrics_cache WHERE track_id = :id");
    q.bindValue(":id", trackId);
    if (q.exec() && q.next()) {
        return q.value(0).toString();
    }
    return QString();
}

Track DatabaseManager::mapTrack(const QSqlQuery& q) const {
    Track t;
    t.id = q.value("id").toString();
    t.title = q.value("title").toString();
    t.artist = q.value("artist").toString();
    t.album = q.value("album").toString();
    t.durationMs = q.value("duration_ms").toLongLong();
    t.trackNumber = q.value("track_number").toInt();
    t.year = q.value("year").toInt();
    t.genre = q.value("genre").toString();
    t.filePath = q.value("file_path").toString();
    t.coverPath = q.value("cover_path").toString();
    t.isLiked = q.value("is_liked").toInt() == 1;
    t.isOffline = q.value("is_offline").toInt() == 1;
    t.createdAt = QDateTime::fromString(q.value("created_at").toString(), Qt::ISODate);
    return t;
}

} // namespace inline_music
