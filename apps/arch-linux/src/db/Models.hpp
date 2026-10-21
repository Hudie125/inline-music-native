#pragma once

#include <QString>
#include <QDateTime>
#include <QMetaType>
#include <vector>

namespace inline_music {

enum class PlaybackState {
    Idle,
    Loading,
    Ready,
    Playing,
    Paused,
    Buffering,
    Seeking,
    Error
};

struct Track {
    QString id;
    QString title = "Unknown Title";
    QString artist = "Unknown Artist";
    QString album = "Unknown Album";
    qint64 durationMs = 0;
    int trackNumber = 0;
    int year = 0;
    QString genre = "Unknown";
    QString filePath;
    QString coverPath;
    bool isLiked = false;
    bool isOffline = true;
    QDateTime createdAt = QDateTime::currentDateTimeUtc();

    QString formattedDuration() const {
        qint64 totalSeconds = durationMs / 1000;
        qint64 minutes = totalSeconds / 60;
        qint64 seconds = totalSeconds % 60;
        return QString("%1:%2")
            .arg(minutes)
            .arg(seconds, 2, 10, QChar('0'));
    }
};

struct Playlist {
    QString id;
    QString name;
    QString description;
    QString coverPath;
    int trackCount = 0;
    qint64 totalDurationMs = 0;
    QDateTime createdAt = QDateTime::currentDateTimeUtc();
};

struct HistoryItem {
    qint64 id = 0;
    QString trackId;
    QDateTime playedAt;
    qint64 durationListenedMs = 0;
    double completionRate = 0.0;
    bool skippedEarly = false;
};

struct LyricsLine {
    qint64 timestampMs = 0;
    qint64 durationMs = 3000;
    QString text;
};

} // namespace inline_music

Q_DECLARE_METATYPE(inline_music::Track)
Q_DECLARE_METATYPE(inline_music::PlaybackState)
