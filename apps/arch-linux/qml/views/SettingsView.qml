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
            text: "Settings"
            font.pixelSize: 32
            font.bold: true
            color: "#FFFFFF"
        }

        GlassCard {
            width: parent.width
            height: 120

            Column {
                anchors.left: parent.left
                anchors.verticalCenter: parent.verticalCenter
                spacing: 6

                Text {
                    text: "Privacy & Storage"
                    font.pixelSize: 18
                    font.bold: true
                    color: "#FFFFFF"
                }
                Text {
                    text: "inline Music does not transmit data or track habits. All history and settings stay local."
                    font.pixelSize: 13
                    color: "#8E8E93"
                }
            }
        }

        GlassCard {
            width: parent.width
            height: 140

            Column {
                anchors.fill: parent
                spacing: 12

                Text {
                    text: "MPRIS & Desktop Environment Integration"
                    font.pixelSize: 18
                    font.bold: true
                    color: "#FFFFFF"
                }
                Text {
                    text: "• D-Bus Service: org.mpris.MediaPlayer2.inline"
                    color: "#8E8E93"
                }
                Text {
                    text: "• GNOME Media Controls & KDE Plasma Media Player Widget active"
                    color: "#8E8E93"
                }
            }
        }
    }
}
