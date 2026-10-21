#pragma once

#include <QObject>
#include <QSqlDatabase>
#include <QSqlQuery>
#include <QSqlError>
#include <QVariant>
#include <QList>
#include <memory>
#include "Models.hpp"

namespace inline_music {

class DatabaseManager : public QObject {
    Q_OBJECT

public:
    explicit DatabaseManager(const QString& customPath = QString(), QObject* parent = nullptr);
    ~DatabaseManager() override;

    bool initialize();

    // Tracks
    QList<Track> getAllTracks();
    std::optional<Track> getTrackById(const QString& id);
    bool saveTrack(const Track& track);
    bool saveTracksBatch(const QList<Track>& tracks);
    bool toggleLike(const QString& trackId);
    QList<Track> getLikedTracks();
    QList<Track> searchTracks(const QString& query);

    // Playlists
    QList<Playlist> getPlaylists();
    Playlist createPlaylist(const QString& name, const QString& description = QString());
    bool deletePlaylist(const QString& playlistId);
    bool addTrackToPlaylist(const QString& playlistId, const QString& trackId);
    QList<Track> getPlaylistTracks(const QString& playlistId);

    // History & Metrics
    bool logHistory(const QString& trackId, qint64 durationMs, double completionRate, bool skipped);
    QList<HistoryItem> getRecentHistory(int limit = 50);
    QStringList getTopArtists(int limit = 5);
    QStringList getTopGenres(int limit = 5);

    // Lyrics
    bool cacheLyrics(const QString& trackId, const QString& plain, const QString& synced);
    QString getCachedSyncedLyrics(const QString& trackId);

private:
    QString m_connectionName;
    QSqlDatabase m_db;
    Track mapTrack(const QSqlQuery& query) const;
};

} // namespace inline_music
