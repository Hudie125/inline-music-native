#include "LyricsService.hpp"
#include <QUrl>
#include <QUrlQuery>
#include <QJsonDocument>
#include <QJsonObject>
#include <QRegularExpression>

namespace inline_music {

LyricsService::LyricsService(DatabaseManager* db, QObject* parent)
    : QObject(parent), m_db(db)
{
}

void LyricsService::fetchLyrics(const QString& trackId, const QString& title, const QString& artist, qint64 durationSec) {
    // 1. Check local cache
    QString cachedSynced = m_db->getCachedSyncedLyrics(trackId);
    if (!cachedSynced.isEmpty()) {
        emit lyricsReady(trackId, parseLrc(cachedSynced));
        return;
    }

    // 2. Query LRCLIB
    QUrl url("https://lrclib.net/api/get");
    QUrlQuery q;
    q.addQueryItem("track_name", title);
    q.addQueryItem("artist_name", artist);
    if (durationSec > 0) {
        q.addQueryItem("duration", QString::number(durationSec));
    }
    url.setQuery(q);

    QNetworkRequest req(url);
    req.setHeader(QNetworkRequest::UserAgentHeader, "inline-Music-ArchLinux/0.1.0");

    QNetworkReply* reply = m_network.get(req);
    connect(reply, &QNetworkReply::finished, this, [this, reply, trackId]() {
        reply->deleteLater();
        if (reply->error() == QNetworkReply::NoError) {
            QByteArray data = reply->readAll();
            QJsonDocument doc = QJsonDocument::fromJson(data);
            if (doc.isObject()) {
                QJsonObject obj = doc.object();
                QString synced = obj.value("syncedLyrics").toString();
                QString plain = obj.value("plainLyrics").toString();

                if (!synced.isEmpty()) {
                    m_db->cacheLyrics(trackId, plain, synced);
                    emit lyricsReady(trackId, parseLrc(synced));
                    return;
                } else if (!plain.isEmpty()) {
                    m_db->cacheLyrics(trackId, plain, "");
                    QVariantList lines;
                    const auto strLines = plain.split('\n');
                    for (const auto& l : strLines) {
                        QVariantMap m;
                        m["timestampMs"] = 0;
                        m["durationMs"] = 0;
                        m["text"] = l.trimmed();
                        lines.append(m);
                    }
                    emit lyricsReady(trackId, lines);
                    return;
                }
            }
        }
        emit lyricsReady(trackId, {});
    });
}

QVariantList LyricsService::parseLrc(const QString& lrcContent) {
    QVariantList result;
    if (lrcContent.trimmed().isEmpty()) return result;

    QRegularExpression re(R"(\[(\d{1,2}):(\d{2})(?:\.(\d{2,3}))?\])");
    struct RawLine {
        qint64 timestampMs;
        QString text;
    };
    QList<RawLine> rawLines;

    const auto lines = lrcContent.split(QRegularExpression("[\r\n]+"), Qt::SkipEmptyParts);
    for (const auto& line : lines) {
        auto it = re.globalMatch(line);
        QList<qint64> timestamps;

        while (it.hasNext()) {
            auto match = it.next();
            qint64 min = match.captured(1).toLongLong();
            qint64 sec = match.captured(2).toLongLong();
            qint64 ms = 0;
            if (match.hasCaptured(3)) {
                QString msStr = match.captured(3);
                ms = (msStr.length() == 2) ? msStr.toLongLong() * 10 : msStr.toLongLong();
            }
            timestamps.append((min * 60 + sec) * 1000 + ms);
        }

        if (!timestamps.isEmpty()) {
            QString cleanText = line;
            cleanText.remove(re);
            cleanText = cleanText.trimmed();
            for (qint64 ts : timestamps) {
                rawLines.append({ts, cleanText});
            }
        }
    }

    std::sort(rawLines.begin(), rawLines.end(), [](const RawLine& a, const RawLine& b) {
        return a.timestampMs < b.timestampMs;
    });

    for (int i = 0; i < rawLines.size(); ++i) {
        qint64 dur = 3000;
        if (i < rawLines.size() - 1) {
            dur = rawLines[i + 1].timestampMs - rawLines[i].timestampMs;
            if (dur <= 0 || dur > 10000) dur = 3000;
        }

        QVariantMap map;
        map["timestampMs"] = rawLines[i].timestampMs;
        map["durationMs"] = dur;
        map["text"] = rawLines[i].text;
        result.append(map);
    }

    return result;
}

} // namespace inline_music
