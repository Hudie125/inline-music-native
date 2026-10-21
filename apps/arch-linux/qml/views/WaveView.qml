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
            text: "My Wave"
            font.pixelSize: 32
            font.bold: true
            color: "#FFFFFF"
        }

        GlassCard {
            width: parent.width
            height: 140

            Column {
                anchors.left: parent.left
                anchors.verticalCenter: parent.verticalCenter
                spacing: 6

                Text {
                    text: "Endless Personal Radio"
                    font.pixelSize: 22
                    font.bold: true
                    color: "#FFFFFF"
                }
                Text {
                    text: "Adaptive local recommendations calculated from your play history and likes."
                    font.pixelSize: 13
                    color: "#8E8E93"
                }
            }

            Button {
                anchors.right: parent.right
                anchors.verticalCenter: parent.verticalCenter
                text: "Launch Wave"
                onClicked: {
                    var tracks = waveEngine.generateWave(25)
                    audioEngine.playQueue(tracks, 0)
                }
            }
        }
    }
}
