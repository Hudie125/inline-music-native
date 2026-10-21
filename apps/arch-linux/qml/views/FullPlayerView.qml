import QtQuick
import QtQuick.Controls
import "../components"

Item {
    id: root

    Row {
        anchors.fill: parent
        anchors.margins: 28
        spacing: 28

        // Left: Album Art & Controls
        GlassCard {
            width: 400
            height: parent.height

            Column {
                anchors.centerIn: parent
                spacing: 20

                Rectangle {
                    width: 280
                    height: 280
                    radius: 20
                    color: "#222228"
                    Image {
                        anchors.fill: parent
                        source: audioEngine.currentTrack.coverPath || ""
                        fillMode: Image.PreserveAspectCrop
                    }
                }

                Text {
                    text: audioEngine.currentTrack.title || "No Track Playing"
                    color: "#FFFFFF"
                    font.pixelSize: 22
                    font.bold: true
                    horizontalAlignment: Text.AlignHCenter
                    width: 320
                    elide: Text.ElideRight
                }

                Text {
                    text: audioEngine.currentTrack.artist || "Select track to begin"
                    color: "#8E8E93"
                    font.pixelSize: 15
                    horizontalAlignment: Text.AlignHCenter
                    width: 320
                    elide: Text.ElideRight
                }

                Row {
                    spacing: 16
                    anchors.horizontalCenter: parent.horizontalCenter

                    Button {
                        text: "⏮"
                        onClicked: audioEngine.previous()
                    }
                    Button {
                        text: "⏯"
                        font.bold: true
                        onClicked: audioEngine.togglePlayPause()
                    }
                    Button {
                        text: "⏭"
                        onClicked: audioEngine.next()
                    }
                }
            }
        }

        // Right: Synced Lyrics
        GlassCard {
            width: parent.width - 428
            height: parent.height

            Column {
                anchors.fill: parent
                spacing: 12

                Text {
                    text: "Synchronized Lyrics"
                    font.pixelSize: 20
                    font.bold: true
                    color: "#FFFFFF"
                }

                ListView {
                    id: lyricsList
                    width: parent.width
                    height: parent.height - 40
                    clip: true
                    model: lyricsModel

                    delegate: Text {
                        width: lyricsList.width
                        text: model.text
                        font.pixelSize: (audioEngine.positionMs >= model.timestampMs && audioEngine.positionMs < model.timestampMs + model.durationMs) ? 22 : 16
                        font.bold: (audioEngine.positionMs >= model.timestampMs && audioEngine.positionMs < model.timestampMs + model.durationMs)
                        color: (audioEngine.positionMs >= model.timestampMs && audioEngine.positionMs < model.timestampMs + model.durationMs) ? "#FFFFFF" : "#8E8E93"
                        wrapMode: Text.WordWrap
                        padding: 6
                    }
                }
            }
        }
    }
}
