#pragma once

#include <QObject>
#include <QNetworkAccessManager>
#include <QNetworkReply>
#include <QVariantList>
#include "../db/DatabaseManager.hpp"
#include "../db/Models.hpp"

namespace inline_music {

class LyricsService : public QObject {
    Q_OBJECT

public:
    explicit LyricsService(DatabaseManager* db, QObject* parent = nullptr);

    Q_INVOKABLE void fetchLyrics(const QString& trackId, const QString& title, const QString& artist, qint64 durationSec = 0);
    static QVariantList parseLrc(const QString& lrcContent);

signals:
    void lyricsReady(const QString& trackId, const QVariantList& lines);

private:
    DatabaseManager* m_db;
    QNetworkAccessManager m_network;
};

} // namespace inline_music
