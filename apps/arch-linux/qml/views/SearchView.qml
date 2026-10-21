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
            text: "Search"
            font.pixelSize: 32
            font.bold: true
            color: "#FFFFFF"
        }

        GlassCard {
            width: parent.width
            height: 52

            TextField {
                id: searchInput
                anchors.fill: parent
                placeholderText: "Search tracks, artists, albums..."
                color: "#FFFFFF"
                placeholderTextColor: "#8E8E93"
                font.pixelSize: 16
                background: null
                onTextChanged: {
                    searchModel.query = text
                }
            }
        }

        GlassCard {
            width: parent.width
            height: parent.height - 120

            ListView {
                anchors.fill: parent
                clip: true
                model: searchModel
                spacing: 8

                delegate: Rectangle {
                    width: parent.width
                    height: 52
                    radius: 8
                    color: "#1A1A20"

                    Row {
                        anchors.fill: parent
                        anchors.margins: 8
                        spacing: 12

                        Text {
                            text: model.title
                            color: "#FFFFFF"
                            font.bold: true
                            font.pixelSize: 14
                        }
                        Text {
                            text: model.artist
                            color: "#8E8E93"
                            font.pixelSize: 13
                        }
                    }

                    MouseArea {
                        anchors.fill: parent
                        onClicked: audioEngine.playTrack(model)
                    }
                }
            }
        }
    }
}
