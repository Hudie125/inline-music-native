#include "WaveEngine.hpp"
#include <QRandomGenerator>
#include <algorithm>
#include <cmath>

namespace inline_music {

WaveEngine::WaveEngine(DatabaseManager* db, QObject* parent)
    : QObject(parent), m_db(db)
{
}

QVariantList WaveEngine::generateWave(int count) {
    QList<Track> allTracks = m_db->getAllTracks();
    if (allTracks.isEmpty()) return {};

    QList<HistoryItem> history = m_db->getRecentHistory(150);
    QStringList topArtists = m_db->getTopArtists(6);
    QStringList topGenres = m_db->getTopGenres(5);

    QHash<QString, QList<HistoryItem>> historyMap;
    for (const auto& item : history) {
        historyMap[item.trackId].append(item);
    }

    QDateTime now = QDateTime::currentDateTimeUtc();
    auto rng = QRandomGenerator::securelyInitialized();

    struct ScoredTrack {
        Track track;
        double score;
    };

    QList<ScoredTrack> scoredList;

    for (const auto& track : allTracks) {
        double score = 50.0; // Baseline

        if (track.isLiked) {
            score += 35.0;
        }

        if (topArtists.contains(track.artist, Qt::CaseInsensitive)) {
            score += 20.0;
        }

        if (topGenres.contains(track.genre, Qt::CaseInsensitive)) {
            score += 15.0;
        }

        if (historyMap.contains(track.id)) {
            const auto& logs = historyMap[track.id];
            score += std::min(logs.size() * 4.0, 24.0);

            double totalComp = 0.0;
            QDateTime mostRecent = logs.first().playedAt;

            for (const auto& l : logs) {
                totalComp += l.completionRate;
                if (l.playedAt > mostRecent) mostRecent = l.playedAt;
            }

            double avgComp = totalComp / logs.size();
            if (avgComp >= 0.8) {
                score += 20.0;
            } else if (avgComp < 0.25) {
                score -= 30.0;
            }

            qint64 secsSince = mostRecent.secsTo(now);
            double hoursSince = secsSince / 3600.0;
            if (hoursSince < 2.0) {
                score -= 45.0; // Recency penalty
            } else if (hoursSince < 6.0) {
                score -= 20.0;
            }
        }

        // Controlled variance for discovery
        score += (rng.generateDouble() * 14.0) - 7.0;

        scoredList.append({track, score});
    }

    std::sort(scoredList.begin(), scoredList.end(), [](const ScoredTrack& a, const ScoredTrack& b) {
        return a.score > b.score;
    });

    int pickCount = std::min(count, static_cast<int>(scoredList.size()));
    QVariantList result;
    for (int i = 0; i < pickCount; ++i) {
        const auto& t = scoredList[i].track;
        QVariantMap map;
        map["id"] = t.id;
        map["title"] = t.title;
        map["artist"] = t.artist;
        map["album"] = t.album;
        map["durationMs"] = t.durationMs;
        map["filePath"] = t.filePath;
        map["coverPath"] = t.coverPath;
        map["isLiked"] = t.isLiked;
        map["formattedDuration"] = t.formattedDuration();
        result.append(map);
    }

    return result;
}

} // namespace inline_music
