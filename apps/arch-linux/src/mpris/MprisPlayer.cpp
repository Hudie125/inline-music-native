#include "MprisPlayer.hpp"
#include <QCoreApplication>
#include <QDBusConnectionInterface>
#include <QDBusObjectPath>
#include <QUrl>

namespace inline_music {

MprisPlayerAdaptor::MprisPlayerAdaptor(GStreamerAudioEngine* engine, QObject* parent)
    : QDBusAbstractAdaptor(parent), m_engine(engine)
{
}

QString MprisPlayerAdaptor::playbackStatus() const {
    switch (static_cast<PlaybackState>(m_engine->state())) {
        case PlaybackState::Playing: return "Playing";
        case PlaybackState::Paused: return "Paused";
        default: return "Stopped";
    }
}

QString MprisPlayerAdaptor::loopStatus() const {
    switch (m_engine->repeatMode()) {
        case 1: return "Track";
        case 2: return "Playlist";
        default: return "None";
    }
}

void MprisPlayerAdaptor::setLoopStatus(const QString& loopStatus) {
    if (loopStatus == "Track") m_engine->setRepeatMode(1);
    else if (loopStatus == "Playlist") m_engine->setRepeatMode(2);
    else m_engine->setRepeatMode(0);
}

bool MprisPlayerAdaptor::shuffle() const {
    return m_engine->shuffle();
}

void MprisPlayerAdaptor::setShuffle(bool s) {
    m_engine->setShuffle(s);
}

QVariantMap MprisPlayerAdaptor::metadata() const {
    QVariantMap meta;
    QVariantMap track = m_engine->currentTrack();
    if (!track.isEmpty()) {
        meta["mpris:trackid"] = QVariant::fromValue(QDBusObjectPath(QString("/org/inline/Track/%1").arg(track.value("id").toString())));
        meta["mpris:length"] = track.value("durationMs").toLongLong() * 1000; // microseconds
        meta["xesam:title"] = track.value("title").toString();
        meta["xesam:artist"] = QStringList{track.value("artist").toString()};
        meta["xesam:album"] = track.value("album").toString();
        QString cover = track.value("coverPath").toString();
        if (!cover.isEmpty()) {
            meta["mpris:artUrl"] = QUrl::fromLocalFile(cover).toString();
        }
    }
    return meta;
}

double MprisPlayerAdaptor::volume() const {
    return m_engine->volume();
}

void MprisPlayerAdaptor::setVolume(double vol) {
    m_engine->setVolume(vol);
}

qlonglong MprisPlayerAdaptor::position() const {
    return m_engine->positionMs() * 1000; // microseconds
}

void MprisPlayerAdaptor::Next() { m_engine->next(); }
void MprisPlayerAdaptor::Previous() { m_engine->previous(); }
void MprisPlayerAdaptor::Pause() { m_engine->pause(); }
void MprisPlayerAdaptor::PlayPause() { m_engine->togglePlayPause(); }
void MprisPlayerAdaptor::Stop() { m_engine->stop(); }
void MprisPlayerAdaptor::Play() { m_engine->play(); }
void MprisPlayerAdaptor::Seek(qlonglong Offset) {
    m_engine->seek(m_engine->positionMs() + (Offset / 1000));
}
void MprisPlayerAdaptor::SetPosition(const QDBusObjectPath&, qlonglong Position) {
    m_engine->seek(Position / 1000);
}
void MprisPlayerAdaptor::OpenUri(const QString& Uri) {
    QVariantMap t;
    t["id"] = "uri_track";
    t["title"] = "Stream";
    t["filePath"] = Uri;
    m_engine->playTrack(t);
}

MprisRootAdaptor::MprisRootAdaptor(QObject* parent)
    : QDBusAbstractAdaptor(parent)
{
}

void MprisRootAdaptor::Raise() {
    // Notify window raise
}

void MprisRootAdaptor::Quit() {
    QCoreApplication::quit();
}

MprisManager::MprisManager(GStreamerAudioEngine* engine, QObject* parent)
    : QObject(parent), m_engine(engine)
{
    m_rootAdaptor = new MprisRootAdaptor(this);
    m_playerAdaptor = new MprisPlayerAdaptor(m_engine, this);
}

bool MprisManager::registerService() {
    QDBusConnection bus = QDBusConnection::sessionBus();
    if (!bus.isConnected()) return false;

    if (!bus.registerService("org.mpris.MediaPlayer2.inline")) {
        return false;
    }

    return bus.registerObject("/org/mpris/MediaPlayer2", this);
}

} // namespace inline_music
