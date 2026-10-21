#pragma once

#include <QObject>
#include <QVector>
#include <gst/gst.h>

namespace inline_music {

class EqualizerDsp : public QObject {
    Q_OBJECT
    Q_PROPERTY(bool enabled READ isEnabled WRITE setEnabled NOTIFY enabledChanged)
    Q_PROPERTY(double crossfadeDuration READ crossfadeDuration WRITE setCrossfadeDuration NOTIFY crossfadeDurationChanged)

public:
    explicit EqualizerDsp(QObject* parent = nullptr);

    void attachToPipeline(GstElement* pipeline);

    bool isEnabled() const { return m_enabled; }
    void setEnabled(bool enabled);

    double crossfadeDuration() const { return m_crossfadeDuration; }
    void setCrossfadeDuration(double seconds);

    Q_INVOKABLE void setBandGain(int bandIndex, double gainDb);
    Q_INVOKABLE double getBandGain(int bandIndex) const;
    Q_INVOKABLE void applyPreset(const QString& presetName);

signals:
    void enabledChanged(bool enabled);
    void crossfadeDurationChanged(double seconds);
    void bandGainChanged(int index, double gainDb);

private:
    bool m_enabled = true;
    double m_crossfadeDuration = 2.5;
    QVector<double> m_bandGains;
    GstElement* m_eqElement = nullptr;
};

} // namespace inline_music
