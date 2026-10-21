import QtQuick
import QtQuick.Controls
import "../components"

Item {
    id: root

    Column {
        anchors.fill: parent
        anchors.margins: 28
        spacing: 20

        Text {
            text: "Equalizer & DSP"
            font.pixelSize: 32
            font.bold: true
            color: "#FFFFFF"
        }

        GlassCard {
            width: parent.width
            height: 80

            Row {
                anchors.fill: parent
                spacing: 24

                Text {
                    anchors.verticalCenter: parent.verticalCenter
                    text: "Crossfade (seconds):"
                    color: "#FFFFFF"
                    font.bold: true
                }

                Slider {
                    anchors.verticalCenter: parent.verticalCenter
                    from: 0
                    to: 12
                    value: 2.5
                    onValueChanged: audioEngine.equalizer.crossfadeDuration = value
                }

                Button {
                    anchors.verticalCenter: parent.verticalCenter
                    text: "Bass Boost"
                    onClicked: audioEngine.equalizer.applyPreset("Bass Boost")
                }

                Button {
                    anchors.verticalCenter: parent.verticalCenter
                    text: "Flat"
                    onClicked: audioEngine.equalizer.applyPreset("Flat")
                }
            }
        }

        GlassCard {
            width: parent.width
            height: 320

            Row {
                anchors.fill: parent
                spacing: (parent.width - (10 * 48)) / 11

                Repeater {
                    model: ["32Hz", "64Hz", "125Hz", "250Hz", "500Hz", "1kHz", "2kHz", "4kHz", "8kHz", "16kHz"]
                    Column {
                        width: 48
                        spacing: 8

                        Slider {
                            orientation: Qt.Vertical
                            height: 220
                            from: -12
                            to: 12
                            value: 0
                            onValueChanged: audioEngine.equalizer.setBandGain(index, value)
                        }

                        Text {
                            anchors.horizontalCenter: parent.horizontalCenter
                            text: modelData
                            color: "#FFFFFF"
                            font.pixelSize: 11
                        }
                    }
                }
            }
        }
    }
}
