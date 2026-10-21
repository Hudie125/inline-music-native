#include "EqualizerDsp.hpp"
#include <algorithm>

namespace inline_music {

EqualizerDsp::EqualizerDsp(QObject* parent)
    : QObject(parent), m_bandGains(10, 0.0)
{
}

void EqualizerDsp::attachToPipeline(GstElement* pipeline) {
    if (!pipeline) return;
    // Find equalizer-10bands element in GStreamer pipeline
    m_eqElement = gst_bin_get_by_name(GST_BIN(pipeline), "equalizer");
    if (m_eqElement) {
        for (int i = 0; i < 10; ++i) {
            setBandGain(i, m_bandGains[i]);
        }
    }
}

void EqualizerDsp::setEnabled(bool enabled) {
    if (m_enabled != enabled) {
        m_enabled = enabled;
        if (m_eqElement) {
            for (int i = 0; i < 10; ++i) {
                g_object_set(G_OBJECT(m_eqElement), QString("band%1").arg(i).toUtf8().constData(),
                             (gdouble)(m_enabled ? m_bandGains[i] : 0.0), NULL);
            }
        }
        emit enabledChanged(m_enabled);
    }
}

void EqualizerDsp::setCrossfadeDuration(double seconds) {
    seconds = std::clamp(seconds, 0.0, 12.0);
    if (m_crossfadeDuration != seconds) {
        m_crossfadeDuration = seconds;
        emit crossfadeDurationChanged(m_crossfadeDuration);
    }
}

void EqualizerDsp::setBandGain(int bandIndex, double gainDb) {
    if (bandIndex < 0 || bandIndex >= 10) return;
    gainDb = std::clamp(gainDb, -15.0, 15.0);
    m_bandGains[bandIndex] = gainDb;

    if (m_eqElement && m_enabled) {
        QByteArray propName = QString("band%1").arg(bandIndex).toUtf8();
        g_object_set(G_OBJECT(m_eqElement), propName.constData(), (gdouble)gainDb, NULL);
    }

    emit bandGainChanged(bandIndex, gainDb);
}

double EqualizerDsp::getBandGain(int bandIndex) const {
    if (bandIndex >= 0 && bandIndex < 10) {
        return m_bandGains[bandIndex];
    }
    return 0.0;
}

void EqualizerDsp::applyPreset(const QString& presetName) {
    if (presetName == "Bass Boost") {
        double g[10] = {6.5, 5.5, 4.0, 2.0, 0.5, 0.0, 0.0, 0.0, 0.0, 0.0};
        for (int i = 0; i < 10; ++i) setBandGain(i, g[i]);
    } else if (presetName == "Treble Boost") {
        double g[10] = {0.0, 0.0, 0.0, 0.0, 0.5, 1.5, 3.0, 4.5, 6.0, 7.0};
        for (int i = 0; i < 10; ++i) setBandGain(i, g[i]);
    } else if (presetName == "Vocal") {
        double g[10] = {-2.0, -1.0, 0.0, 1.5, 3.5, 4.0, 3.0, 1.5, 0.0, -1.0};
        for (int i = 0; i < 10; ++i) setBandGain(i, g[i]);
    } else {
        // Flat
        for (int i = 0; i < 10; ++i) setBandGain(i, 0.0);
    }
}

} // namespace inline_music
