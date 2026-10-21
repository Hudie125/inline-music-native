import QtQuick
import QtQuick.Controls
import "../components"

Item {
    id: root

    Column {
        anchors.fill: parent
        anchors.margins: 28
        spacing: 20

        Row {
            width: parent.width
            height: 48

            Column {
                Text {
                    text: "Music Library"
                    font.pixelSize: 32
                    font.bold: true
                    color: "#FFFFFF"
                }
                Text {
                    text: "Local audio collection"
                    font.pixelSize: 14
                    color: "#8E8E93"
                }
            }
        }

        GlassCard {
            width: parent.width
            height: parent.height - 80

            ListView {
                id: trackList
                anchors.fill: parent
                clip: true
                model: libraryModel
                spacing: 8

                delegate: Rectangle {
                    width: trackList.width
                    height: 56
                    radius: 10
                    color: mouseArea.containsMouse ? "#2A2A32" : "#1A1A20"

                    Row {
                        anchors.fill: parent
                        anchors.margins: 8
                        spacing: 14

                        Rectangle {
                            width: 40
                            height: 40
                            radius: 8
                            color: "#28282E"
                            Image {
                                anchors.fill: parent
                                source: model.coverPath || ""
                                fillMode: Image.PreserveAspectCrop
                            }
                        }

                        Column {
                            width: parent.width - 180
                            anchors.verticalCenter: parent.verticalCenter
                            Text {
                                text: model.title
                                color: "#FFFFFF"
                                font.bold: true
                                font.pixelSize: 14
                                elide: Text.ElideRight
                            }
                            Text {
                                text: model.artist + " • " + model.album
                                color: "#8E8E93"
                                font.pixelSize: 12
                                elide: Text.ElideRight
                            }
                        }

                        Text {
                            anchors.verticalCenter: parent.verticalCenter
                            text: model.formattedDuration || "0:00"
                            color: "#8E8E93"
                            font.pixelSize: 13
                        }
                    }

                    MouseArea {
                        id: mouseArea
                        anchors.fill: parent
                        hoverEnabled: true
                        onClicked: {
                            audioEngine.playTrack(model)
                        }
                    }
                }
            }
        }
    }
}
