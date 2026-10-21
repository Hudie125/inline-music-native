#include "GStreamerAudioEngine.hpp"
#include <QUrl>
#include <QRandomGenerator>
#include <algorithm>

namespace inline_music {

GStreamerAudioEngine::GStreamerAudioEngine(QObject* parent)
    : QObject(parent)
{
    connect(&m_positionTimer, &QTimer::timeout, this, [this]() {
        if (m_pipeline && m_state == PlaybackState::Playing) {
            gint64 pos = 0;
            if (gst_element_query_position(m_pipeline, GST_FORMAT_TIME, &pos)) {
                m_positionMs = pos / GST_MSECOND;
                emit positionChanged(m_positionMs);
            }

            gint64 dur = 0;
            if (gst_element_query_duration(m_pipeline, GST_FORMAT_TIME, &dur) && dur > 0) {
                qint64 dMs = dur / GST_MSECOND;
                if (dMs != m_durationMs) {
                    m_durationMs = dMs;
                    emit durationChanged(m_durationMs);
                }
            }
        }
    });
}

GStreamerAudioEngine::~GStreamerAudioEngine() {
    stop();
    if (m_pipeline) {
        gst_element_set_state(m_pipeline, GST_STATE_NULL);
        gst_object_unref(m_pipeline);
        m_pipeline = nullptr;
    }
}

bool GStreamerAudioEngine::initialize() {
    if (!gst_is_initialized()) {
        gst_init(nullptr, nullptr);
    }

    // Create playbin3 with custom equalizer bin
    m_pipeline = gst_element_factory_make("playbin3", "player");
    if (!m_pipeline) {
        m_pipeline = gst_element_factory_make("playbin", "player");
    }

    if (!m_pipeline) {
        return false;
    }

    // Create audio sink with equalizer
    GstElement* audioSinkBin = gst_bin_new("audiosinkbin");
    GstElement* eq = gst_element_factory_make("equalizer-10bands", "equalizer");
    GstElement* sink = gst_element_factory_make("autoaudiosink", "sink");

    if (audioSinkBin && eq && sink) {
        gst_bin_add_many(GST_BIN(audioSinkBin), eq, sink, NULL);
        gst_element_link(eq, sink);

        GstPad* pad = gst_element_get_static_pad(eq, "sink");
        GstPad* ghostPad = gst_ghost_pad_new("sink", pad);
        gst_pad_set_active(ghostPad, TRUE);
        gst_element_add_pad(audioSinkBin, ghostPad);
        gst_object_unref(pad);

        g_object_set(GST_OBJECT(m_pipeline), "audio-sink", audioSinkBin, NULL);
        m_equalizer.attachToPipeline(audioSinkBin);
    }

    GstBus* bus = gst_element_get_bus(m_pipeline);
    gst_bus_add_watch(bus, busCallback, this);
    gst_object_unref(bus);

    m_positionTimer.start(250);
    return true;
}

gboolean GStreamerAudioEngine::busCallback(GstBus*, GstMessage* msg, gpointer data) {
    auto* self = static_cast<GStreamerAudioEngine*>(data);
    switch (GST_MESSAGE_TYPE(msg)) {
        case GST_MESSAGE_EOS:
            self->next();
            break;
        case GST_MESSAGE_ERROR: {
            GError* err = nullptr;
            gchar* debug = nullptr;
            gst_message_parse_error(msg, &err, &debug);
            self->updateState(PlaybackState::Error);
            if (err) g_error_free(err);
            if (debug) g_free(debug);
            self->next();
            break;
        }
        case GST_MESSAGE_STATE_CHANGED: {
            if (GST_MESSAGE_SRC(msg) == GST_OBJECT(self->m_pipeline)) {
                GstState oldState, newState, pending;
                gst_message_parse_state_changed(msg, &oldState, &newState, &pending);
                if (newState == GST_STATE_PLAYING) {
                    self->updateState(PlaybackState::Playing);
                } else if (newState == GST_STATE_PAUSED) {
                    self->updateState(PlaybackState::Paused);
                }
            }
            break;
        }
        default:
            break;
    }
    return TRUE;
}

void GStreamerAudioEngine::updateState(PlaybackState newState) {
    if (m_state != newState) {
        m_state = newState;
        emit stateChanged(static_cast<int>(m_state));
    }
}

QVariantMap GStreamerAudioEngine::currentTrack() const {
    QVariantMap map;
    map["id"] = m_currentTrack.id;
    map["title"] = m_currentTrack.title;
    map["artist"] = m_currentTrack.artist;
    map["album"] = m_currentTrack.album;
    map["durationMs"] = m_currentTrack.durationMs;
    map["coverPath"] = m_currentTrack.coverPath;
    map["filePath"] = m_currentTrack.filePath;
    map["isLiked"] = m_currentTrack.isLiked;
    map["formattedDuration"] = m_currentTrack.formattedDuration();
    return map;
}

void GStreamerAudioEngine::setVolume(double vol) {
    vol = std::clamp(vol, 0.0, 1.0);
    if (m_volume != vol) {
        m_volume = vol;
        if (m_pipeline) {
            g_object_set(GST_OBJECT(m_pipeline), "volume", (gdouble)m_volume, NULL);
        }
        emit volumeChanged(m_volume);
    }
}

void GStreamerAudioEngine::setShuffle(bool s) {
    if (m_shuffle != s) {
        m_shuffle = s;
        if (m_shuffle) rebuildShuffle();
        emit shuffleChanged(m_shuffle);
    }
}

void GStreamerAudioEngine::setRepeatMode(int mode) {
    if (m_repeatMode != mode) {
        m_repeatMode = mode;
        emit repeatModeChanged(m_repeatMode);
    }
}

void GStreamerAudioEngine::playTrack(const QVariantMap& trackData) {
    Track t;
    t.id = trackData.value("id").toString();
    t.title = trackData.value("title").toString();
    t.artist = trackData.value("artist").toString();
    t.album = trackData.value("album").toString();
    t.durationMs = trackData.value("durationMs").toLongLong();
    t.filePath = trackData.value("filePath").toString();
    t.coverPath = trackData.value("coverPath").toString();
    t.isLiked = trackData.value("isLiked").toBool();

    m_currentTrack = t;
    emit trackChanged(currentTrack());

    if (!m_pipeline) initialize();

    gst_element_set_state(m_pipeline, GST_STATE_NULL);

    QString uri = t.filePath.startsWith("http") ? t.filePath : QUrl::fromLocalFile(t.filePath).toString();
    g_object_set(GST_OBJECT(m_pipeline), "uri", uri.toUtf8().constData(), NULL);
    g_object_set(GST_OBJECT(m_pipeline), "volume", (gdouble)m_volume, NULL);

    gst_element_set_state(m_pipeline, GST_STATE_PLAYING);
    updateState(PlaybackState::Playing);
}

void GStreamerAudioEngine::playQueue(const QVariantList& tracks, int startIndex) {
    m_queue.clear();
    for (const auto& item : tracks) {
        QVariantMap map = item.toMap();
        Track t;
        t.id = map.value("id").toString();
        t.title = map.value("title").toString();
        t.artist = map.value("artist").toString();
        t.album = map.value("album").toString();
        t.durationMs = map.value("durationMs").toLongLong();
        t.filePath = map.value("filePath").toString();
        t.coverPath = map.value("coverPath").toString();
        t.isLiked = map.value("isLiked").toBool();
        m_queue.append(t);
    }

    m_queueIndex = std::clamp(startIndex, 0, (int)m_queue.size() - 1);
    rebuildShuffle();
    emit queueChanged();

    if (m_queueIndex >= 0 && m_queueIndex < m_queue.size()) {
        playTrack(currentTrack());
    }
}

void GStreamerAudioEngine::togglePlayPause() {
    if (m_state == PlaybackState::Playing) {
        pause();
    } else {
        play();
    }
}

void GStreamerAudioEngine::play() {
    if (m_pipeline && m_state == PlaybackState::Paused) {
        gst_element_set_state(m_pipeline, GST_STATE_PLAYING);
        updateState(PlaybackState::Playing);
    }
}

void GStreamerAudioEngine::pause() {
    if (m_pipeline && m_state == PlaybackState::Playing) {
        gst_element_set_state(m_pipeline, GST_STATE_PAUSED);
        updateState(PlaybackState::Paused);
    }
}

void GStreamerAudioEngine::stop() {
    if (m_pipeline) {
        gst_element_set_state(m_pipeline, GST_STATE_NULL);
        updateState(PlaybackState::Idle);
    }
}

void GStreamerAudioEngine::next() {
    if (m_queue.isEmpty()) return;

    if (m_repeatMode == 1) { // repeat one
        seek(0);
        play();
        return;
    }

    if (m_shuffle) {
        m_shufflePointer++;
        if (m_shufflePointer >= m_shuffleIndices.size()) {
            if (m_repeatMode == 2) { // repeat all
                rebuildShuffle();
                m_shufflePointer = 0;
            } else {
                stop();
                return;
            }
        }
        m_queueIndex = m_shuffleIndices[m_shufflePointer];
    } else {
        m_queueIndex++;
        if (m_queueIndex >= m_queue.size()) {
            if (m_repeatMode == 2) {
                m_queueIndex = 0;
            } else {
                stop();
                return;
            }
        }
    }

    if (m_queueIndex >= 0 && m_queueIndex < m_queue.size()) {
        m_currentTrack = m_queue[m_queueIndex];
        playTrack(currentTrack());
    }
}

void GStreamerAudioEngine::previous() {
    if (m_queue.isEmpty()) return;

    if (m_positionMs > 3000) {
        seek(0);
        return;
    }

    m_queueIndex = std::max(0, m_queueIndex - 1);
    if (m_queueIndex >= 0 && m_queueIndex < m_queue.size()) {
        m_currentTrack = m_queue[m_queueIndex];
        playTrack(currentTrack());
    }
}

void GStreamerAudioEngine::seek(qint64 targetMs) {
    if (m_pipeline) {
        gint64 posNs = targetMs * GST_MSECOND;
        gst_element_seek_simple(m_pipeline, GST_FORMAT_TIME,
            static_cast<GstSeekFlags>(GST_SEEK_FLAG_FLUSH | GST_SEEK_FLAG_KEY_UNIT), posNs);
        m_positionMs = targetMs;
        emit positionChanged(m_positionMs);
    }
}

void GStreamerAudioEngine::rebuildShuffle() {
    m_shuffleIndices.clear();
    for (int i = 0; i < m_queue.size(); ++i) {
        m_shuffleIndices.append(i);
    }
    // Fisher-Yates
    auto* rng = QRandomGenerator::global();
    for (int i = m_shuffleIndices.size() - 1; i > 0; --i) {
        int j = rng->bounded(i + 1);
        std::swap(m_shuffleIndices[i], m_shuffleIndices[j]);
    }
    m_shufflePointer = 0;
}

} // namespace inline_music
