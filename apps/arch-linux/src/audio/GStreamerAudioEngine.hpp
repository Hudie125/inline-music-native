#pragma once

#include <QObject>
#include <QTimer>
#include <QVariantMap>
#include <QVariantList>
#include <gst/gst.h>
#include "../db/Models.hpp"
#include "EqualizerDsp.hpp"

namespace inline_music {

class GStreamerAudioEngine : public QObject {
    Q_OBJECT
    Q_PROPERTY(int state READ state NOTIFY stateChanged)
    Q_PROPERTY(QVariantMap currentTrack READ currentTrack NOTIFY trackChanged)
    Q_PROPERTY(qint64 positionMs READ positionMs NOTIFY positionChanged)
    Q_PROPERTY(qint64 durationMs READ durationMs NOTIFY durationChanged)
    Q_PROPERTY(double volume READ volume WRITE setVolume NOTIFY volumeChanged)
    Q_PROPERTY(bool shuffle READ shuffle WRITE setShuffle NOTIFY shuffleChanged)
    Q_PROPERTY(int repeatMode READ repeatMode WRITE setRepeatMode NOTIFY repeatModeChanged)

public:
    explicit GStreamerAudioEngine(QObject* parent = nullptr);
    ~GStreamerAudioEngine() override;

    bool initialize();

    int state() const { return static_cast<int>(m_state); }
    QVariantMap currentTrack() const;
    qint64 positionMs() const { return m_positionMs; }
    qint64 durationMs() const { return m_durationMs; }
    double volume() const { return m_volume; }
    void setVolume(double vol);
    bool shuffle() const { return m_shuffle; }
    void setShuffle(bool s);
    int repeatMode() const { return m_repeatMode; }
    void setRepeatMode(int mode);

    EqualizerDsp* equalizer() { return &m_equalizer; }

    Q_INVOKABLE void playTrack(const QVariantMap& trackData);
    Q_INVOKABLE void playQueue(const QVariantList& tracks, int startIndex = 0);
    Q_INVOKABLE void togglePlayPause();
    Q_INVOKABLE void play();
    Q_INVOKABLE void pause();
    Q_INVOKABLE void stop();
    Q_INVOKABLE void next();
    Q_INVOKABLE void previous();
    Q_INVOKABLE void seek(qint64 targetMs);

signals:
    void stateChanged(int state);
    void trackChanged(const QVariantMap& track);
    void positionChanged(qint64 positionMs);
    void durationChanged(qint64 durationMs);
    void volumeChanged(double volume);
    void shuffleChanged(bool shuffle);
    void repeatModeChanged(int repeatMode);
    void queueChanged();

private:
    void updateState(PlaybackState newState);
    void rebuildShuffle();
    static gboolean busCallback(GstBus* bus, GstMessage* msg, gpointer data);

    PlaybackState m_state = PlaybackState::Idle;
    Track m_currentTrack;
    QList<Track> m_queue;
    QList<int> m_shuffleIndices;
    int m_queueIndex = -1;
    int m_shufflePointer = 0;

    qint64 m_positionMs = 0;
    qint64 m_durationMs = 0;
    double m_volume = 1.0;
    bool m_shuffle = false;
    int m_repeatMode = 0; // 0=off, 1=one, 2=all

    GstElement* m_pipeline = nullptr;
    EqualizerDsp m_equalizer;
    QTimer m_positionTimer;
};

} // namespace inline_music
