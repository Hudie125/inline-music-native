#pragma once

#include <QObject>
#include <QDBusConnection>
#include <QDBusAbstractAdaptor>
#include <QDBusObjectPath>
#include <QVariantMap>
#include "../audio/GStreamerAudioEngine.hpp"

namespace inline_music {

class MprisPlayerAdaptor : public QDBusAbstractAdaptor {
    Q_OBJECT
    Q_CLASSINFO("D-Bus Interface", "org.mpris.MediaPlayer2.Player")

    Q_PROPERTY(QString PlaybackStatus READ playbackStatus)
    Q_PROPERTY(QString LoopStatus READ loopStatus WRITE setLoopStatus)
    Q_PROPERTY(double Rate READ rate WRITE setRate)
    Q_PROPERTY(bool Shuffle READ shuffle WRITE setShuffle)
    Q_PROPERTY(QVariantMap Metadata READ metadata)
    Q_PROPERTY(double Volume READ volume WRITE setVolume)
    Q_PROPERTY(qlonglong Position READ position)
    Q_PROPERTY(double MinimumRate READ minimumRate)
    Q_PROPERTY(double MaximumRate READ maximumRate)
    Q_PROPERTY(bool CanGoNext READ canGoNext)
    Q_PROPERTY(bool CanGoPrevious READ canGoPrevious)
    Q_PROPERTY(bool CanPlay READ canPlay)
    Q_PROPERTY(bool CanPause READ canPause)
    Q_PROPERTY(bool CanSeek READ canSeek)
    Q_PROPERTY(bool CanControl READ canControl)

public:
    explicit MprisPlayerAdaptor(GStreamerAudioEngine* engine, QObject* parent);

    QString playbackStatus() const;
    QString loopStatus() const;
    void setLoopStatus(const QString& loopStatus);
    double rate() const { return 1.0; }
    void setRate(double) {}
    bool shuffle() const;
    void setShuffle(bool s);
    QVariantMap metadata() const;
    double volume() const;
    void setVolume(double vol);
    qlonglong position() const;
    double minimumRate() const { return 1.0; }
    double maximumRate() const { return 1.0; }
    bool canGoNext() const { return true; }
    bool canGoPrevious() const { return true; }
    bool canPlay() const { return true; }
    bool canPause() const { return true; }
    bool canSeek() const { return true; }
    bool canControl() const { return true; }

public slots:
    void Next();
    void Previous();
    void Pause();
    void PlayPause();
    void Stop();
    void Play();
    void Seek(qlonglong Offset);
    void SetPosition(const QDBusObjectPath& TrackId, qlonglong Position);
    void OpenUri(const QString& Uri);

signals:
    void Seeked(qlonglong Position);

private:
    GStreamerAudioEngine* m_engine;
};

class MprisRootAdaptor : public QDBusAbstractAdaptor {
    Q_OBJECT
    Q_CLASSINFO("D-Bus Interface", "org.mpris.MediaPlayer2")

    Q_PROPERTY(bool CanQuit READ canQuit)
    Q_PROPERTY(bool CanRaise READ canRaise)
    Q_PROPERTY(bool HasTrackList READ hasTrackList)
    Q_PROPERTY(QString Identity READ identity)
    Q_PROPERTY(QString DesktopEntry READ desktopEntry)
    Q_PROPERTY(QStringList SupportedUriSchemes READ supportedUriSchemes)
    Q_PROPERTY(QStringList SupportedMimeTypes READ supportedMimeTypes)

public:
    explicit MprisRootAdaptor(QObject* parent);

    bool canQuit() const { return true; }
    bool canRaise() const { return true; }
    bool hasTrackList() const { return false; }
    QString identity() const { return QStringLiteral("inline"); }
    QString desktopEntry() const { return QStringLiteral("inline"); }
    QStringList supportedUriSchemes() const { return {"file", "http", "https"}; }
    QStringList supportedMimeTypes() const { return {"audio/mpeg", "audio/flac", "audio/ogg", "audio/x-wav", "audio/aac"}; }

public slots:
    void Raise();
    void Quit();
};

class MprisManager : public QObject {
    Q_OBJECT
public:
    explicit MprisManager(GStreamerAudioEngine* engine, QObject* parent = nullptr);
    bool registerService();

private:
    GStreamerAudioEngine* m_engine;
    MprisPlayerAdaptor* m_playerAdaptor;
    MprisRootAdaptor* m_rootAdaptor;
};

} // namespace inline_music
